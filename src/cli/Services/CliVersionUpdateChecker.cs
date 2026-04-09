using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Washington.Services;

internal static class CliVersionUpdateChecker
{
    private static readonly HttpClient HttpClient = CreateHttpClient();
    private static readonly Uri LatestReleaseUri = new("https://api.github.com/repos/polatengin/washington/releases/latest");
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromMilliseconds(500);

    public static async Task<string?> TryGetUpdateNoteAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var currentVersion = CliVersion.GetComparableVersion(assembly);
        if (!CliVersion.TryParseComparableVersion(currentVersion, out var parsedCurrentVersion))
        {
            return null;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(RequestTimeout);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUri);
            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var release = await response.Content.ReadFromJsonAsync<LatestReleaseResponse>(cancellationToken: timeoutCts.Token);
            var latestTag = release?.TagName;
            if (!CliVersion.TryParseComparableVersion(latestTag, out var parsedLatestVersion))
            {
                return null;
            }

            return parsedLatestVersion > parsedCurrentVersion
                ? $"Update available: {CliVersion.NormalizeTag(latestTag)}"
                : null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("bce-cli");
        return httpClient;
    }

    private sealed class LatestReleaseResponse
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }
    }
}