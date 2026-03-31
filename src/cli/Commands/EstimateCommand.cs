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

        var paramOption = new Option<string[]>(
            name: "--param",
            description: "Parameter value override in key=value format (can be specified multiple times)")
        { AllowMultipleArgumentsPerToken = true };

        var command = new Command("estimate", "Estimate monthly Azure costs from a Bicep file")
        {
            fileOption,
            paramsFileOption,
            currencyOption,
            outputFormatOption,
            outputFileOption,
            paramOption
        };

        command.SetHandler(async (file, paramsFile, currency, outputFormat, outputFile, paramOverrides) =>
        {
            await RunAsync(file, paramsFile, currency, outputFormat, outputFile, paramOverrides);
        }, fileOption, paramsFileOption, currencyOption, outputFormatOption, outputFileOption, paramOption);

        return command;
    }

    private static async Task RunAsync(
        FileInfo file, FileInfo? paramsFile, string currency,
        string outputFormat, string? outputFile, string[] paramOverrides)
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

        // Parse param overrides into dictionary
        var paramDict = new Dictionary<string, string>();
        foreach (var p in paramOverrides ?? Array.Empty<string>())
        {
            var eqIndex = p.IndexOf('=');
            if (eqIndex > 0)
            {
                paramDict[p[..eqIndex]] = p[(eqIndex + 1)..];
            }
        }

        var report = await service.EstimateFromBicepAsync(
            file.FullName,
            paramsFile?.FullName,
            currency,
            paramDict);

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
