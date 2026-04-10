using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Washington.Services;

public sealed record DocsDocument(
    string Title,
    string Category,
    string Href,
    string Body,
    int? SidebarPosition = null,
    string? SidebarGroup = null);

public sealed class DocsClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly IReadOnlyDictionary<string, DocsNavigationMetadata> EmptySidebarMetadata = new Dictionary<string, DocsNavigationMetadata>(StringComparer.OrdinalIgnoreCase);

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public DocsClient(Uri baseUri)
        : this(CreateHttpClient(baseUri), ownsHttpClient: true)
    {
    }

    internal DocsClient(HttpClient httpClient)
        : this(httpClient, ownsHttpClient: false)
    {
        if (httpClient.BaseAddress is null)
        {
            throw new ArgumentException("The HTTP client must have a BaseAddress configured.", nameof(httpClient));
        }
    }

    private DocsClient(HttpClient httpClient, bool ownsHttpClient)
    {
        _httpClient = httpClient;
        _ownsHttpClient = ownsHttpClient;
    }

    public async Task<IReadOnlyList<DocsDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "/docfind/documents.json", "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new HttpRequestException("The online docs search index is not available right now.");
        }

        EnsureSuccess(response, "the online docs search index");

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<List<DocsDocumentPayload>>(
            contentStream,
            JsonOptions,
            cancellationToken);
        if (payload is null || payload.Count == 0)
        {
            return Array.Empty<DocsDocument>();
        }

        var sidebarMetadata = await TryGetSidebarMetadataAsync(cancellationToken);
        var documents = new List<DocsDocument>(payload.Count);
        var seenRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in payload)
        {
            if (string.IsNullOrWhiteSpace(item.Href) || string.IsNullOrWhiteSpace(item.Title))
            {
                continue;
            }

            var href = NormalizeRoute(item.Href);
            if (!seenRoutes.Add(href))
            {
                continue;
            }

            documents.Add(new DocsDocument(
                item.Title.Trim(),
                string.IsNullOrWhiteSpace(item.Category) ? "Documentation" : item.Category.Trim(),
                href,
                item.Body?.Trim() ?? string.Empty,
                sidebarMetadata.TryGetValue(href, out var navigationMetadata) ? navigationMetadata.Order : null,
                sidebarMetadata.TryGetValue(href, out navigationMetadata) ? navigationMetadata.Group : null));
        }

        return OrderDocuments(documents);
    }

    public async Task<string?> GetPageAsync(string route, CancellationToken cancellationToken = default)
    {
        var normalizedRoute = NormalizeRoute(route);
        using var request = CreateRequest(HttpMethod.Get, normalizedRoute, "text/plain");
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        EnsureSuccess(response, $"the docs page {normalizedRoute}");
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocsDocument>> SearchAsync(
        string query,
        int limit = 12,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<DocsDocument>();
        }

        var normalizedQuery = query.Trim();
        var documents = await GetDocumentsAsync(cancellationToken);

        return documents
            .Where(document => Matches(document, normalizedQuery))
            .Take(limit)
            .ToArray();
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    internal static string NormalizeRoute(string value)
    {
        var trimmed = value.Trim();

        if (string.IsNullOrEmpty(trimmed) || trimmed == "/")
        {
            return "/";
        }

        var withLeadingSlash = trimmed.StartsWith('/') ? trimmed : $"/{trimmed}";
        return withLeadingSlash.EndsWith('/') ? withLeadingSlash[..^1] : withLeadingSlash;
    }

    internal static IReadOnlyList<DocsDocument> OrderDocuments(IEnumerable<DocsDocument> documents)
    {
        return documents
            .OrderBy(static document => document.SidebarPosition ?? int.MaxValue)
            .ThenBy(static document => document.Href == "/" ? 0 : 1)
            .ThenBy(static document => document.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static document => document.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static document => document.Href, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static HttpClient CreateHttpClient(Uri baseUri)
    {
        return new HttpClient
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(15),
        };
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string relativePath, string acceptMediaType)
    {
        var request = new HttpRequestMessage(method, relativePath);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptMediaType));
        request.Headers.UserAgent.ParseAdd("bce-docs");
        return request;
    }

    private static void EnsureSuccess(HttpResponseMessage response, string resourceDescription)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new HttpRequestException($"Could not load {resourceDescription} (HTTP {(int)response.StatusCode}).");
    }

    private static bool Matches(DocsDocument document, string query)
    {
        return document.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || document.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
            || document.Body.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyDictionary<string, DocsNavigationMetadata>> TryGetSidebarMetadataAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, "/docfind/navigation.json", "application/json");
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return EmptySidebarMetadata;
            }

            EnsureSuccess(response, "the online docs navigation metadata");

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<List<DocsNavigationPayload>>(
                contentStream,
                JsonOptions,
                cancellationToken);
            if (payload is null || payload.Count == 0)
            {
                return EmptySidebarMetadata;
            }

            var sidebarPositions = new Dictionary<string, DocsNavigationMetadata>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in payload.OrderBy(static item => item.Order ?? int.MaxValue))
            {
                if (string.IsNullOrWhiteSpace(item.Href) || item.Order is null || item.Order < 0)
                {
                    continue;
                }

                sidebarPositions.TryAdd(
                    NormalizeRoute(item.Href),
                    new DocsNavigationMetadata(
                        item.Order.Value,
                        string.IsNullOrWhiteSpace(item.Group) ? null : item.Group.Trim()));
            }

            return sidebarPositions;
        }
        catch (HttpRequestException)
        {
            return EmptySidebarMetadata;
        }
        catch (JsonException)
        {
            return EmptySidebarMetadata;
        }
        catch (TaskCanceledException)
        {
            return EmptySidebarMetadata;
        }
    }

    private readonly record struct DocsNavigationMetadata(int Order, string? Group);

    private sealed class DocsDocumentPayload
    {
        public string? Body { get; init; }

        public string? Category { get; init; }

        public string? Href { get; init; }

        public string? Title { get; init; }
    }

    private sealed class DocsNavigationPayload
    {
        public string? Group { get; init; }

        public string? Href { get; init; }

        public int? Order { get; init; }
    }
}