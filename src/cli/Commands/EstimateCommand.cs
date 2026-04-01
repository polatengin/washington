using System.CommandLine;
using Washington.Cache;
using Washington.Mappers;
using Washington.Models;
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

        var outputFormatOption = new Option<string>(
            name: "--output",
            getDefaultValue: () => "table",
            description: "Output format: table, json, csv, markdown");

        var paramOption = new Option<string[]>(
            name: "--param",
            description: "Parameter value override in key=value format (can be specified multiple times)")
        { AllowMultipleArgumentsPerToken = true };

        var command = new Command("estimate", "Estimate monthly Azure costs from a Bicep or ARM file")
        {
            fileOption,
            paramsFileOption,
            outputFormatOption,
            paramOption
        };

        command.SetHandler(async (file, paramsFile, outputFormat, paramOverrides) =>
        {
            await RunAsync(file, paramsFile, outputFormat, paramOverrides);
        }, fileOption, paramsFileOption, outputFormatOption, paramOption);

        return command;
    }

    private static async Task RunAsync(
        FileInfo file, FileInfo? paramsFile,
        string outputFormat, string[] paramOverrides)
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

        CostReport report;
        if (file.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            var armJson = await File.ReadAllTextAsync(file.FullName);
            report = await service.EstimateFromArmJsonAsync(armJson);
        }
        else
        {
            report = await service.EstimateFromBicepAsync(
                file.FullName,
                paramsFile?.FullName,
                paramDict);
        }

        var output = OutputFormatter.Format(report, outputFormat, file.FullName);
        Console.Write(output);
    }
}
