using System.CommandLine;
using Washington.Commands;
using Washington.Lsp;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Azure Cost Estimator — estimate monthly Azure costs from Bicep files");

        // estimate command
        rootCommand.AddCommand(EstimateCommand.Create());

        // cache command group
        var cacheCommand = new Command("cache", "Manage the pricing cache");
        cacheCommand.AddCommand(CacheClearCommand.Create());
        cacheCommand.AddCommand(CacheInfoCommand.Create());
        rootCommand.AddCommand(cacheCommand);

        // lsp command
        var lspCommand = new Command("lsp", "Start in Language Server Protocol mode (stdin/stdout)");
        lspCommand.SetHandler(async () =>
        {
            var server = new CostEstimatorLanguageServer(
                Console.OpenStandardInput(),
                Console.OpenStandardOutput());
            await server.RunAsync();
        });
        rootCommand.AddCommand(lspCommand);

        return await rootCommand.InvokeAsync(args);
    }
}
