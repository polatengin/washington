using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using Washington.Commands;
using Washington.Lsp;
using Washington.Services;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (await TryHandleVersionRequestAsync(args, Console.Out))
        {
            return 0;
        }

        return await InvokeAsync(CreateRootCommand(), args);
    }

    internal static async Task<bool> TryHandleVersionRequestAsync(
        string[] args,
        TextWriter outputWriter,
        Func<Assembly, CancellationToken, Task<string?>>? updateNoteProvider = null,
        CancellationToken cancellationToken = default)
    {
        if (!Array.Exists(args, static arg => string.Equals(arg, "--version", StringComparison.Ordinal)))
        {
            return false;
        }

        var assembly = typeof(Program).Assembly;

        await outputWriter.WriteLineAsync(CliVersion.GetDisplayVersion(assembly));

        updateNoteProvider ??= CliVersionUpdateChecker.TryGetUpdateNoteAsync;
        var updateNote = await updateNoteProvider(assembly, cancellationToken);
        if (!string.IsNullOrWhiteSpace(updateNote))
        {
            await outputWriter.WriteLineAsync(updateNote);
        }

        return true;
    }

    internal static async Task<int> InvokeAsync(Command command, string[] args, TextWriter? errorWriter = null)
    {
        var effectiveErrorWriter = errorWriter ?? Console.Error;

        try
        {
            return await command.Parse(args).InvokeAsync();
        }
        catch (BicepCompilationException ex)
        {
            await effectiveErrorWriter.WriteLineAsync($"Error: {ex.Message}");
            return 1;
        }
        catch (JsonException ex)
        {
            await effectiveErrorWriter.WriteLineAsync($"Error: Failed to parse template JSON. {ex.Message}");
            return 1;
        }
    }

    private static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("BCE (Bicep Cost Estimator) - estimate monthly Azure costs from Bicep and ARM files");

        // estimate command
        rootCommand.Subcommands.Add(EstimateCommand.Create());

        // cache command group
        var cacheCommand = new Command("cache", "Manage the pricing cache");
        cacheCommand.Subcommands.Add(CacheClearCommand.Create());
        cacheCommand.Subcommands.Add(CacheInfoCommand.Create());
        rootCommand.Subcommands.Add(cacheCommand);

        // lsp command
        var lspCommand = new Command("lsp", "Start in Language Server Protocol mode (stdin/stdout)");
        lspCommand.SetAction(async _ =>
        {
            var server = new CostEstimatorLanguageServer(
                Console.OpenStandardInput(),
                Console.OpenStandardOutput());
            await server.RunAsync();
        });
        rootCommand.Subcommands.Add(lspCommand);

        return rootCommand;
    }
}
