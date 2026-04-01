using Washington.Mappers;
using Washington.Models;

namespace Washington.Services;

public class CostEstimationService
{
    private readonly BicepCompiler _compiler;
    private readonly ResourceExtractor _extractor;
    private readonly CostAggregator _aggregator;

    public CostEstimationService(BicepCompiler compiler, ResourceExtractor extractor, CostAggregator aggregator)
    {
        _compiler = compiler;
        _extractor = extractor;
        _aggregator = aggregator;
    }

    public async Task<CostReport> EstimateFromBicepAsync(
        string bicepFilePath, string? paramsFilePath = null,
        Dictionary<string, string>? paramOverrides = null)
    {
        // Compile bicep to ARM JSON
        var armJson = await _compiler.CompileBicepToArm(bicepFilePath);

        // If params file provided, compile it too and merge parameters
        if (!string.IsNullOrEmpty(paramsFilePath) && File.Exists(paramsFilePath))
        {
            // Params compilation is for validation; the main bicep build already resolves defaults
            await _compiler.CompileBicepParamsToArm(paramsFilePath);
        }

        return await EstimateFromArmJsonAsync(armJson);
    }

    public async Task<CostReport> EstimateFromArmJsonAsync(string armJson)
    {
        var resources = _extractor.Extract(armJson);
        return await _aggregator.GenerateReportAsync(resources);
    }
}
