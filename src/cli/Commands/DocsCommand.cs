using System.CommandLine;
using System.Net.Http;
using Washington.Services;

namespace Washington.Commands;

public class DocsCommand
{
    private static readonly Uri DefaultDocsBaseUri = new("https://bicepcostestimator.net");

    internal static async Task<(bool Handled, int ExitCode)> TryHandleAsync(
        string[] args,
        TextWriter outputWriter,
        TextWriter errorWriter,
        Func<DocsClient>? clientFactory = null,
        Func<bool>? isInteractive = null)
    {
        if (args.Length == 0 || !string.Equals(args[0], "docs", StringComparison.OrdinalIgnoreCase))
        {
            return (false, 0);
        }

        if (Array.Exists(args, static arg => string.Equals(arg, "--help", StringComparison.Ordinal)
            || string.Equals(arg, "-h", StringComparison.Ordinal)))
        {
            return (false, 0);
        }

        clientFactory ??= static () => new DocsClient(DefaultDocsBaseUri);
        isInteractive ??= static () => !Console.IsInputRedirected && !Console.IsOutputRedirected;

        using var client = clientFactory();
        var exitCode = await ExecuteAsync(
            () => ExecuteRequestedActionAsync(client, args.Skip(1).ToArray(), outputWriter, errorWriter, isInteractive()),
            errorWriter);

        return (true, exitCode);
    }

    public static Command Create() => Create(
        clientFactory: static () => new DocsClient(DefaultDocsBaseUri),
        outputWriter: Console.Out,
        errorWriter: Console.Error,
        isInteractive: static () => !Console.IsInputRedirected && !Console.IsOutputRedirected);

    internal static Command Create(
        Func<DocsClient> clientFactory,
        TextWriter outputWriter,
        TextWriter errorWriter,
        Func<bool> isInteractive)
    {
        var command = new Command("docs", "Browse or print the online docs hosted at bicepcostestimator.net");
        command.TreatUnmatchedTokensAsErrors = false;
        command.SetAction(async parseResult =>
        {
            using var client = clientFactory();
            var routeTokens = parseResult.UnmatchedTokens;

            return await ExecuteAsync(
                () => ExecuteDefaultActionAsync(client, routeTokens, outputWriter, errorWriter, isInteractive()),
                errorWriter);
        });

        var listCommand = new Command("list", "List available docs pages");
        listCommand.SetAction(async _ =>
        {
            using var client = clientFactory();
            return await ExecuteAsync(() => ExecuteListAsync(client, outputWriter), errorWriter);
        });

        var searchTermsArgument = new Argument<string[]>("terms")
        {
            Arity = ArgumentArity.OneOrMore,
            Description = "Search term to match against docs titles and summaries",
        };

        var searchCommand = new Command("search", "Search docs titles and summaries");
        searchCommand.Arguments.Add(searchTermsArgument);
        searchCommand.SetAction(async parseResult =>
        {
            using var client = clientFactory();
            var searchTerms = parseResult.GetValue(searchTermsArgument) ?? Array.Empty<string>();
            return await ExecuteAsync(
                () => ExecuteSearchAsync(client, string.Join(' ', searchTerms), outputWriter, errorWriter),
                errorWriter);
        });

        command.Subcommands.Add(listCommand);
        command.Subcommands.Add(searchCommand);

        return command;
    }

    internal static Task<int> ExecuteDefaultActionAsync(
        DocsClient client,
        IReadOnlyList<string> routeTokens,
        TextWriter outputWriter,
        TextWriter errorWriter,
        bool isInteractive)
    {
        if (routeTokens.Count == 0)
        {
            return ExecuteDefaultAsync(client, route: null, outputWriter, errorWriter, isInteractive);
        }

        if (routeTokens.Count == 1)
        {
            return ExecutePageAsync(client, routeTokens[0], outputWriter, errorWriter);
        }

        return WriteUsageErrorAsync(errorWriter, "Usage: bce docs [list|search <term>|/route]");
    }

    internal static Task<int> ExecuteRequestedActionAsync(
        DocsClient client,
        IReadOnlyList<string> args,
        TextWriter outputWriter,
        TextWriter errorWriter,
        bool isInteractive)
    {
        if (args.Count == 0)
        {
            return ExecuteDefaultAsync(client, route: null, outputWriter, errorWriter, isInteractive);
        }

        if (args.Count == 1 && string.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase))
        {
            return ExecuteListAsync(client, outputWriter);
        }

        if (string.Equals(args[0], "search", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Count == 1)
            {
                return WriteUsageErrorAsync(errorWriter, "Usage: bce docs search <term>");
            }

            return ExecuteSearchAsync(client, string.Join(' ', args.Skip(1)), outputWriter, errorWriter);
        }

        if (args.Count == 1)
        {
            return ExecutePageAsync(client, args[0], outputWriter, errorWriter);
        }

        return WriteUsageErrorAsync(errorWriter, "Usage: bce docs [list|search <term>|/route]");
    }

    internal static async Task<int> ExecuteAsync(Func<Task<int>> commandAction, TextWriter errorWriter)
    {
        try
        {
            return await commandAction();
        }
        catch (TaskCanceledException)
        {
            await errorWriter.WriteLineAsync("Error: Timed out while loading the online docs.");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            await errorWriter.WriteLineAsync($"Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            await errorWriter.WriteLineAsync($"Error: {ex.Message}");
            return 1;
        }
    }

    internal static async Task<int> ExecuteDefaultAsync(
        DocsClient client,
        string? route,
        TextWriter outputWriter,
        TextWriter errorWriter,
        bool isInteractive)
    {
        if (!string.IsNullOrWhiteSpace(route))
        {
            return await ExecutePageAsync(client, route, outputWriter, errorWriter);
        }

        if (!isInteractive)
        {
            return await ExecutePageAsync(client, "/", outputWriter, errorWriter);
        }

        try
        {
            var documents = await client.GetDocumentsAsync();
            if (documents.Count == 0)
            {
                return await ExecutePageAsync(client, "/", outputWriter, errorWriter);
            }

            var browser = new DocsConsoleBrowser(client, documents, outputWriter);
            return await browser.RunAsync();
        }
        catch (HttpRequestException)
        {
            return await ExecutePageAsync(client, "/", outputWriter, errorWriter);
        }
        catch (TaskCanceledException)
        {
            return await ExecutePageAsync(client, "/", outputWriter, errorWriter);
        }
    }

    internal static async Task<int> ExecutePageAsync(
        DocsClient client,
        string route,
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        var normalizedRoute = DocsClient.NormalizeRoute(route);
        var page = await client.GetPageAsync(normalizedRoute);
        if (page is null)
        {
            await errorWriter.WriteLineAsync($"No docs page found for {normalizedRoute}.");
            return 1;
        }

        await outputWriter.WriteAsync(NormalizePageOutput(page));
        return 0;
    }

    internal static async Task<int> ExecuteListAsync(DocsClient client, TextWriter outputWriter)
    {
        var documents = await client.GetDocumentsAsync();
        foreach (var document in documents.OrderBy(static document => document.Href, StringComparer.OrdinalIgnoreCase))
        {
            await outputWriter.WriteLineAsync($"{document.Href.PadRight(32)} {document.Title}");
        }

        return 0;
    }

    internal static async Task<int> ExecuteSearchAsync(
        DocsClient client,
        string query,
        TextWriter outputWriter,
        TextWriter errorWriter)
    {
        var matches = await client.SearchAsync(query);
        if (matches.Count == 0)
        {
            await errorWriter.WriteLineAsync($"No pages matched \"{query.Trim()}\".");
            return 1;
        }

        foreach (var document in matches)
        {
            await outputWriter.WriteLineAsync($"{document.Href.PadRight(32)} {document.Title}");
        }

        return 0;
    }

    private static string NormalizePageOutput(string page)
    {
        var rendered = SupportsAnsiPageOutput() ? page : DocsConsoleText.StripAnsi(page);
        return rendered.EndsWith('\n') ? rendered : rendered + Environment.NewLine;
    }

    private static async Task<int> WriteUsageErrorAsync(TextWriter errorWriter, string message)
    {
        await errorWriter.WriteLineAsync(message);
        return 1;
    }

    private static bool SupportsAnsiPageOutput()
        => !Console.IsOutputRedirected && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
}