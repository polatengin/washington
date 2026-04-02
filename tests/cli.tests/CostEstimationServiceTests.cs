using System.Text.Json;
using Washington.Mappers;
using Washington.Models;
using Washington.Services;
using Xunit;

namespace Washington.Tests;

public class CostEstimationServiceTests
{
    [Fact]
    public async Task EstimateFromBicepAsync_AppliesParamsFileValues()
    {
        var compiler = new FakeBicepCompiler(
            armTemplateJson: """
            {
              "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
              "contentVersion": "1.0.0.0",
              "parameters": {
                "vmSize": { "type": "string", "defaultValue": "Standard_D2s_v3" }
              },
              "resources": [
                {
                  "type": "Microsoft.Compute/virtualMachines",
                  "apiVersion": "2023-09-01",
                  "name": "test-vm",
                  "location": "eastus",
                  "properties": {
                    "hardwareProfile": {
                      "vmSize": "[parameters('vmSize')]"
                    }
                  }
                }
              ]
            }
            """,
            armParamsJson: """
            {
              "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
              "contentVersion": "1.0.0.0",
              "parameters": {
                "vmSize": { "value": "Standard_D4s_v3" }
              }
            }
            """);

          var paramsFilePath = Path.GetTempFileName();

          try
          {
            var report = await CreateService(compiler).EstimateFromBicepAsync("main.bicep", paramsFilePath);

            Assert.Single(report.Lines);
            Assert.Contains("Standard_D4s_v3", report.Lines[0].PricingDetails);
            Assert.Equal(146.00m, report.GrandTotal);
          }
          finally
          {
            File.Delete(paramsFilePath);
          }
    }

    [Fact]
    public async Task EstimateFromBicepAsync_CommandLineOverridesWinOverParamsFileValues()
    {
        var compiler = new FakeBicepCompiler(
            armTemplateJson: """
            {
              "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
              "contentVersion": "1.0.0.0",
              "parameters": {
                "vmSize": { "type": "string", "defaultValue": "Standard_D2s_v3" }
              },
              "resources": [
                {
                  "type": "Microsoft.Compute/virtualMachines",
                  "apiVersion": "2023-09-01",
                  "name": "test-vm",
                  "location": "eastus",
                  "properties": {
                    "hardwareProfile": {
                      "vmSize": "[parameters('vmSize')]"
                    }
                  }
                }
              ]
            }
            """,
            armParamsJson: """
            {
              "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
              "contentVersion": "1.0.0.0",
              "parameters": {
                "vmSize": { "value": "Standard_D2s_v3" }
              }
            }
            """);

        var paramsFilePath = Path.GetTempFileName();

        try
        {
          var report = await CreateService(compiler).EstimateFromBicepAsync(
            "main.bicep",
            paramsFilePath,
            new Dictionary<string, string> { ["vmSize"] = "Standard_D4s_v3" });

          Assert.Single(report.Lines);
          Assert.Contains("Standard_D4s_v3", report.Lines[0].PricingDetails);
          Assert.Equal(146.00m, report.GrandTotal);
        }
        finally
        {
          File.Delete(paramsFilePath);
        }
    }

    private static CostEstimationService CreateService(BicepCompiler compiler)
    {
        var extractor = new ResourceExtractor();
        var pricingClient = new MockPricingApiClient(new List<PriceRecord>
        {
            new()
            {
                ArmSkuName = "Standard_D2s_v3",
                UnitPrice = 0.096,
                UnitOfMeasure = "1 Hour",
                MeterName = "D2s v3",
                ServiceName = "Virtual Machines"
            },
            new()
            {
                ArmSkuName = "Standard_D4s_v3",
                UnitPrice = 0.2,
                UnitOfMeasure = "1 Hour",
                MeterName = "D4s v3",
                ServiceName = "Virtual Machines"
            }
        });

        var aggregator = new CostAggregator(new MapperRegistry(), pricingClient);
        return new CostEstimationService(compiler, extractor, aggregator);
    }

    private sealed class FakeBicepCompiler(string armTemplateJson, string armParamsJson) : BicepCompiler
    {
        public override Task<string> CompileBicepToArm(string bicepFilePath) =>
            Task.FromResult(armTemplateJson);

        public override Task<string> CompileBicepParamsToArm(string bicepParamFilePath) =>
            Task.FromResult(armParamsJson);
    }
}
