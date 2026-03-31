using System.CommandLine;
using Washington.Cache;
using Washington.Mappers;
using Washington.Services;

namespace Washington.Commands;

public class EstimateCommand
{
    public static Command Create()
    {
        var fileOption = new Option<FileInfo>(
            name: "--file",
            description: "Path to the .bicep or ARM JSON file")
        { IsRequired = true };

        var paramsFileOption = new Option<FileInfo?>(
            name: "--params-file",
            description: "Path to the .bicepparam file");

        var currencyOption = new Option<string>(
            name: "--currency",
            getDefaultValue: () => "USD",
            description: "Currency code (e.g., USD, EUR)");

        var outputFormatOption = new Option<string>(
            name: "--output",
            getDefaultValue: () => "table",
            description: "Output format: table, json, csv, markdown");

        var outputFileOption = new Option<string?>(
            name: "--output-file",
            description: "Write output to file instead of stdout");

        var command = new Command("estimate", "Estimate monthly Azure costs from a Bicep file")
        {
            fileOption,
            paramsFileOption,
            currencyOption,
            outputFormatOption,
            outputFileOption
        };

        command.SetHandler(async (file, paramsFile, currency, outputFormat, outputFile) =>
        {
            await RunAsync(file, paramsFile, currency, outputFormat, outputFile);
        }, fileOption, paramsFileOption, currencyOption, outputFormatOption, outputFileOption);

        return command;
    }

    private static async Task RunAsync(
        FileInfo file, FileInfo? paramsFile, string currency, string outputFormat, string? outputFile)
    {
        if (!file.Exists)
        {
            Console.Error.WriteLine($"Error: File not found: {file.FullName}");
            return;
        }

        var cache = new FilePricingCache();
        var compiler = new BicepCompiler();
        var extractor = new ResourceExtractor();
        var pricingClient = new PricingApiClient(cache);
        var mapperRegistry = new MapperRegistry();
        var aggregator = new CostAggregator(mapperRegistry, pricingClient);
        var service = new CostEstimationService(compiler, extractor, aggregator);

        var report = await service.EstimateFromBicepAsync(
            file.FullName,
            paramsFile?.FullName,
            currency);

        var output = OutputFormatter.Format(report, outputFormat, file.FullName);

        if (!string.IsNullOrEmpty(outputFile))
        {
            File.WriteAllText(outputFile, output);
            Console.WriteLine($"Output written to {outputFile}");
        }
        else
        {
            Console.Write(output);
        }
    }
}
