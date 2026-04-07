using System.CommandLine;
using Washington.Commands;
using Washington.Lsp;

public class Program
{
    public static async Task<int> Main(string[] args)
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

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
