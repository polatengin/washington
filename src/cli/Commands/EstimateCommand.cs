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
        var fileOption = new Option<FileInfo>("--file")
        {
            Description = "Path to the .bicep or ARM JSON file",
            Required = true
        }.AcceptExistingOnly();

        var paramsFileOption = new Option<FileInfo>("--params-file")
        {
            Description = "Path to the .bicepparam file"
        }.AcceptExistingOnly();

        var outputFormatOption = new Option<string>("--output")
        {
            Description = "Output format: table, json, csv, markdown",
            DefaultValueFactory = _ => "table"
        };

        var paramOption = new Option<string[]>("--param")
        {
            Description = "Parameter value override in key=value format (can be specified multiple times)",
            AllowMultipleArgumentsPerToken = true
        };

        var command = new Command("estimate", "Estimate monthly Azure costs from a Bicep or ARM file");
        command.Options.Add(fileOption);
        command.Options.Add(paramsFileOption);
        command.Options.Add(outputFormatOption);
        command.Options.Add(paramOption);

        command.SetAction(async parseResult =>
        {
            var file = parseResult.GetRequiredValue(fileOption);
            FileInfo? paramsFile = parseResult.GetValue(paramsFileOption);
            var outputFormat = parseResult.GetValue(outputFormatOption) ?? "table";
            var paramOverrides = parseResult.GetValue(paramOption) ?? Array.Empty<string>();

            await RunAsync(file, paramsFile, outputFormat, paramOverrides);
        });

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
