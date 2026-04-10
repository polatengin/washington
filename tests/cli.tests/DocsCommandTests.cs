using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Washington.Commands;
using Washington.Services;
using Xunit;

namespace Washington.Tests;

public class DocsCommandTests
{
    [Fact]
    public async Task ExecutePage_WhenRouteExists_WritesPageContent()
    {
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var docsCommand = CreateCommand(
            outputWriter,
            errorWriter,
            isInteractive: false,
            pages: new Dictionary<string, string>
            {
                ["/"] = "Introduction page",
                ["/getting-started"] = "Getting Started page",
            },
            documents: new[]
            {
                new DocsDocument("Introduction", "Documentation", "/", "Home"),
                new DocsDocument("Getting Started", "Documentation", "/getting-started", "Install the CLI"),
            });

        var result = await DocsCommand.TryHandleAsync(
            new[] { "docs", "/getting-started" },
            outputWriter,
            errorWriter,
            clientFactory: () => CreateClient(
                new Dictionary<string, string>
                {
                    ["/"] = "Introduction page",
                    ["/getting-started"] = "Getting Started page",
                },
                new[]
                {
                    new DocsDocument("Introduction", "Documentation", "/", "Home"),
                    new DocsDocument("Getting Started", "Documentation", "/getting-started", "Install the CLI"),
                },
                HttpStatusCode.OK),
            isInteractive: () => false);

        Assert.True(result.Handled);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal($"Getting Started page{Environment.NewLine}", outputWriter.ToString());
        Assert.Empty(errorWriter.ToString());
    }

    [Fact]
    public async Task ExecutePage_WhenRouteDoesNotExist_ReturnsNonZeroAndWritesError()
    {
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var result = await DocsCommand.TryHandleAsync(
            new[] { "docs", "/missing" },
            outputWriter,
            errorWriter,
            clientFactory: () => CreateClient(
                new Dictionary<string, string>
                {
                    ["/"] = "Introduction page",
                },
                new[]
                {
                    new DocsDocument("Introduction", "Documentation", "/", "Home"),
                },
                HttpStatusCode.OK),
            isInteractive: () => false);

        Assert.True(result.Handled);
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(outputWriter.ToString());
        Assert.Equal($"No docs page found for /missing.{Environment.NewLine}", errorWriter.ToString());
    }

    [Fact]
    public async Task ExecuteDefault_WhenNonInteractive_WritesIntroductionPage()
    {
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var result = await DocsCommand.TryHandleAsync(
            new[] { "docs" },
            outputWriter,
            errorWriter,
            clientFactory: () => CreateClient(
                new Dictionary<string, string>
                {
                    ["/"] = "Introduction page",
                },
                new[]
                {
                    new DocsDocument("Introduction", "Documentation", "/", "Home"),
                },
                HttpStatusCode.OK),
            isInteractive: () => false);

        Assert.True(result.Handled);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal($"Introduction page{Environment.NewLine}", outputWriter.ToString());
        Assert.Empty(errorWriter.ToString());
    }

    [Fact]
    public async Task ExecuteDefault_WhenInteractiveIndexFails_FallsBackToIntroductionPage()
    {
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var result = await DocsCommand.TryHandleAsync(
            new[] { "docs" },
            outputWriter,
            errorWriter,
            clientFactory: () => CreateClient(
                new Dictionary<string, string>
                {
                    ["/"] = "Introduction page",
                },
                Array.Empty<DocsDocument>(),
                HttpStatusCode.InternalServerError),
            isInteractive: () => true);

        Assert.True(result.Handled);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal($"Introduction page{Environment.NewLine}", outputWriter.ToString());
        Assert.Empty(errorWriter.ToString());
    }

    [Fact]
    public async Task ExecuteList_WritesRoutesAndTitles()
    {
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var result = await DocsCommand.TryHandleAsync(
            new[] { "docs", "list" },
            outputWriter,
            errorWriter,
            clientFactory: () => CreateClient(
                new Dictionary<string, string>
                {
                    ["/"] = "Introduction page",
                    ["/cli/commands"] = "CLI Commands page",
                },
                new[]
                {
                    new DocsDocument("CLI Commands", "CLI", "/cli/commands", "Command reference"),
                    new DocsDocument("Introduction", "Documentation", "/", "Home"),
                },
                HttpStatusCode.OK),
            isInteractive: () => false);

        Assert.True(result.Handled);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            string.Join(Environment.NewLine, new[]
            {
                $"{"/".PadRight(32)} Introduction",
                $"{"/cli/commands".PadRight(32)} CLI Commands",
                string.Empty,
            }),
            outputWriter.ToString());
        Assert.Empty(errorWriter.ToString());
    }

    [Fact]
    public async Task ExecuteList_WhenNavigationMetadataExists_UsesSidebarOrder()
    {
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var result = await DocsCommand.TryHandleAsync(
            new[] { "docs", "list" },
            outputWriter,
            errorWriter,
            clientFactory: () => CreateClient(
                new Dictionary<string, string>
                {
                    ["/"] = "Introduction page",
                    ["/getting-started"] = "Getting Started page",
                    ["/playground"] = "Playground page",
                },
                new[]
                {
                    new DocsDocument("Getting Started", "Documentation", "/getting-started", "Install the CLI"),
                    new DocsDocument("Playground", "Playground", "/playground", "Interactive playground"),
                    new DocsDocument("Introduction", "Documentation", "/", "Home"),
                },
                HttpStatusCode.OK,
                new[]
                {
                    new DocsNavigationItem("/", 0),
                    new DocsNavigationItem("/playground", 1),
                    new DocsNavigationItem("/getting-started", 2),
                }),
            isInteractive: () => false);

        Assert.True(result.Handled);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            string.Join(Environment.NewLine, new[]
            {
                $"{"/".PadRight(32)} Introduction",
                $"{"/playground".PadRight(32)} Playground",
                $"{"/getting-started".PadRight(32)} Getting Started",
                string.Empty,
            }),
            outputWriter.ToString());
        Assert.Empty(errorWriter.ToString());
    }

    [Fact]
    public async Task ExecuteSearch_WhenMatchesExist_WritesMatches()
    {
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var result = await DocsCommand.TryHandleAsync(
            new[] { "docs", "search", "price", "mismatches" },
            outputWriter,
            errorWriter,
            clientFactory: () => CreateClient(
                new Dictionary<string, string>
                {
                    ["/"] = "Introduction page",
                    ["/guides/troubleshooting"] = "Troubleshooting page",
                },
                new[]
                {
                    new DocsDocument("Introduction", "Documentation", "/", "Home"),
                    new DocsDocument("Troubleshooting", "Guides", "/guides/troubleshooting", "Troubleshooting Azure price mismatches"),
                },
                HttpStatusCode.OK),
            isInteractive: () => false);

        Assert.True(result.Handled);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            $"/guides/troubleshooting          Troubleshooting{Environment.NewLine}",
            outputWriter.ToString());
        Assert.Empty(errorWriter.ToString());
    }

    [Fact]
    public async Task ExecuteSearch_WhenNoMatchesExist_ReturnsNonZeroAndWritesError()
    {
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var result = await DocsCommand.TryHandleAsync(
            new[] { "docs", "search", "does-not-exist" },
            outputWriter,
            errorWriter,
            clientFactory: () => CreateClient(
                new Dictionary<string, string>
                {
                    ["/"] = "Introduction page",
                },
                new[]
                {
                    new DocsDocument("Introduction", "Documentation", "/", "Home"),
                },
                HttpStatusCode.OK),
            isInteractive: () => false);

        Assert.True(result.Handled);
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(outputWriter.ToString());
        Assert.Equal($"No pages matched \"does-not-exist\".{Environment.NewLine}", errorWriter.ToString());
    }

    [Fact]
    public async Task RenderBrowse_DoesNotOverflowViewport()
    {
        using var outputWriter = new StringWriter();

        var documents = new[]
        {
            new DocsDocument("Introduction", "Documentation", "/", "Home"),
            new DocsDocument("CLI Commands", "CLI", "/cli/commands", "Command reference"),
        };

        var browser = new DocsConsoleBrowser(
            CreateClient(
                new Dictionary<string, string>
                {
                    ["/"] = "Introduction page",
                },
                documents,
                HttpStatusCode.OK),
            documents,
            outputWriter);

        var method = typeof(DocsConsoleBrowser).GetMethod("RenderBrowseAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var output = await (Task<string>)method!.Invoke(browser, new object[] { 80, 6 })!;
        var normalized = output.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Equal(6, normalized.Split('\n').Length);
        Assert.False(normalized.EndsWith("\n", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetDocuments_WhenNavigationMetadataExists_LoadsSidebarGroups()
    {
        using var client = CreateClient(
            new Dictionary<string, string>
            {
                ["/"] = "Introduction page",
                ["/cli/commands"] = "CLI Commands page",
                ["/cli/configuration"] = "CLI Configuration page",
            },
            new[]
            {
                new DocsDocument("CLI Configuration", "CLI", "/cli/configuration", "Configure the CLI"),
                new DocsDocument("CLI Commands", "CLI", "/cli/commands", "Command reference"),
                new DocsDocument("Introduction", "Documentation", "/", "Home"),
            },
            HttpStatusCode.OK,
            new[]
            {
                new DocsNavigationItem("/", 0),
                new DocsNavigationItem("/cli/commands", 1, "CLI"),
                new DocsNavigationItem("/cli/configuration", 2, "CLI"),
            });

        var documents = await client.GetDocumentsAsync();

        Assert.Collection(
            documents,
            document =>
            {
                Assert.Equal("/", document.Href);
                Assert.Null(document.SidebarGroup);
            },
            document =>
            {
                Assert.Equal("/cli/commands", document.Href);
                Assert.Equal("CLI", document.SidebarGroup);
            },
            document =>
            {
                Assert.Equal("/cli/configuration", document.Href);
                Assert.Equal("CLI", document.SidebarGroup);
            });
    }

    [Fact]
    public async Task RenderBrowse_WhenSidebarGroupsExist_RendersHeadingsAndPlainTitles()
    {
        using var outputWriter = new StringWriter();

        var documents = new[]
        {
            new DocsDocument("Introduction", "Documentation", "/", "Home", 0, null),
            new DocsDocument("Playground", "Playground", "/playground", "Interactive playground", 1, null),
            new DocsDocument("Getting Started", "Documentation", "/getting-started", "Install the CLI", 2, null),
            new DocsDocument("CLI Commands", "CLI", "/cli/commands", "Command reference", 3, "CLI"),
            new DocsDocument("CLI Configuration", "CLI", "/cli/configuration", "Configuration reference", 4, "CLI"),
        };

        var browser = new DocsConsoleBrowser(
            CreateClient(
                new Dictionary<string, string>
                {
                    ["/"] = "Introduction page",
                },
                documents,
                HttpStatusCode.OK),
            documents,
            outputWriter);

        var method = typeof(DocsConsoleBrowser).GetMethod("RenderBrowseAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var output = await (Task<string>)method!.Invoke(browser, new object[] { 80, 8 })!;
        var normalized = DocsConsoleText.StripAnsi(output).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var leftColumn = normalized
            .Take(6)
            .Select(line => line.Split(" | ", 2, StringSplitOptions.None)[0].TrimEnd())
            .ToArray();

        Assert.Equal("Introduction", leftColumn[0].Trim());
        Assert.Equal("Playground", leftColumn[1].Trim());
        Assert.Equal("Getting Started", leftColumn[2].Trim());
        Assert.Equal("~ CLI ~", leftColumn[3].Trim());
        Assert.Equal("CLI Commands", leftColumn[4].Trim());
        Assert.Equal("CLI Configuration", leftColumn[5].Trim());
        Assert.DoesNotContain("[CLI]", leftColumn[4], StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDocuments_WhenNavigationMetadataMissing_FallsBackToLegacyBrowseOrder()
    {
        using var client = CreateClient(
            new Dictionary<string, string>
            {
                ["/"] = "Introduction page",
                ["/getting-started"] = "Getting Started page",
                ["/playground"] = "Playground page",
            },
            new[]
            {
                new DocsDocument("Playground", "Playground", "/playground", "Interactive playground"),
                new DocsDocument("Getting Started", "Documentation", "/getting-started", "Install the CLI"),
                new DocsDocument("Introduction", "Documentation", "/", "Home"),
            },
            HttpStatusCode.OK);

        var documents = await client.GetDocumentsAsync();

        Assert.Collection(
            documents,
            document => Assert.Equal("/", document.Href),
            document => Assert.Equal("/getting-started", document.Href),
            document => Assert.Equal("/playground", document.Href));
    }

    private static System.CommandLine.Command CreateCommand(
        TextWriter outputWriter,
        TextWriter errorWriter,
        bool isInteractive,
        IReadOnlyDictionary<string, string> pages,
        IReadOnlyList<DocsDocument> documents,
        HttpStatusCode searchIndexStatusCode = HttpStatusCode.OK)
    {
        return DocsCommand.Create(
            clientFactory: () => CreateClient(pages, documents, searchIndexStatusCode),
            outputWriter: outputWriter,
            errorWriter: errorWriter,
            isInteractive: () => isInteractive);
    }

    private static DocsClient CreateClient(
        IReadOnlyDictionary<string, string> pages,
        IReadOnlyList<DocsDocument> documents,
        HttpStatusCode searchIndexStatusCode,
        IReadOnlyList<DocsNavigationItem>? navigation = null)
    {
        var httpClient = new HttpClient(new FakeDocsHandler(pages, documents, searchIndexStatusCode, navigation ?? Array.Empty<DocsNavigationItem>()))
        {
            BaseAddress = new Uri("https://example.test"),
        };

        return new DocsClient(httpClient);
    }

    private sealed class FakeDocsHandler : HttpMessageHandler
    {
        private readonly IReadOnlyList<DocsDocument> _documents;
        private readonly IReadOnlyList<DocsNavigationItem> _navigation;
        private readonly IReadOnlyDictionary<string, string> _pages;
        private readonly HttpStatusCode _searchIndexStatusCode;

        public FakeDocsHandler(
            IReadOnlyDictionary<string, string> pages,
            IReadOnlyList<DocsDocument> documents,
            HttpStatusCode searchIndexStatusCode,
            IReadOnlyList<DocsNavigationItem> navigation)
        {
            _pages = pages;
            _documents = documents;
            _navigation = navigation;
            _searchIndexStatusCode = searchIndexStatusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "/";

            if (path == "/docfind/documents.json")
            {
                if (_searchIndexStatusCode != HttpStatusCode.OK)
                {
                    return Task.FromResult(new HttpResponseMessage(_searchIndexStatusCode));
                }

                var json = BuildDocumentsJson(_documents);
                return Task.FromResult(CreateResponse(HttpStatusCode.OK, json, "application/json"));
            }

            if (path == "/docfind/navigation.json")
            {
                if (_navigation.Count == 0)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                var json = BuildNavigationJson(_navigation);
                return Task.FromResult(CreateResponse(HttpStatusCode.OK, json, "application/json"));
            }

            if (_pages.TryGetValue(path, out var page))
            {
                return Task.FromResult(CreateResponse(HttpStatusCode.OK, page, "text/plain"));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static string BuildDocumentsJson(IReadOnlyList<DocsDocument> documents)
        {
            return JsonSerializer.Serialize(documents.Select(document => new
            {
                title = document.Title,
                category = document.Category,
                href = document.Href,
                body = document.Body,
            }));
        }

        private static string BuildNavigationJson(IReadOnlyList<DocsNavigationItem> navigation)
        {
            return JsonSerializer.Serialize(navigation.Select(item => new
            {
                group = item.Group,
                href = item.Href,
                order = item.Order,
            }));
        }

        private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content, string mediaType)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, mediaType),
            };
        }
    }

    private sealed record DocsNavigationItem(string Href, int Order, string? Group = null);
}