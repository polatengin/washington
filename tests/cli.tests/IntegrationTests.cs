using System.Text.Json;
using Washington.Commands;
using Washington.Mappers;
using Washington.Models;
using Washington.Services;
using Xunit;

namespace Washington.Tests;

/// <summary>
/// Integration tests that run the full pipeline: ARM JSON → extract → mock pricing → assert report.
/// </summary>
public class IntegrationTests
{
    [Fact]
    public async Task RoundTrip_SingleVm_ProducesCorrectReport()
    {
        var armJson = File.ReadAllText(GetFixturePath("simple-vm.arm.json"));

        var extractor = new ResourceExtractor();
        var resources = extractor.Extract(armJson);

        var mockPricing = new MockPricingApiClient(new List<PriceRecord>
        {
            new PriceRecord
            {
                ArmSkuName = "Standard_D2s_v3",
                UnitPrice = 0.096,
                UnitOfMeasure = "1 Hour",
                MeterName = "D2s v3",
                ServiceName = "Virtual Machines"
            }
        });

        var registry = new MapperRegistry();
        var aggregator = new CostAggregator(registry, mockPricing);
        var report = await aggregator.GenerateReportAsync(resources);

        Assert.Single(report.Lines);
        Assert.Equal("test-vm", report.Lines[0].ResourceName);
        Assert.Equal("Microsoft.Compute/virtualMachines", report.Lines[0].ResourceType);
        Assert.Equal(70.08m, report.Lines[0].MonthlyCost);
        Assert.Equal(70.08m, report.GrandTotal);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public async Task RoundTrip_MultiResource_ProducesReportWithWarnings()
    {
        var armJson = File.ReadAllText(GetFixturePath("multi-resource.arm.json"));

        var extractor = new ResourceExtractor();
        var resources = extractor.Extract(armJson);

        Assert.Equal(4, resources.Count);

        var mockPricing = new MockPricingApiClient(new List<PriceRecord>
        {
            new PriceRecord
            {
                ArmSkuName = "Standard_D2s_v3",
                UnitPrice = 0.096,
                UnitOfMeasure = "1 Hour",
                MeterName = "D2s v3",
                ServiceName = "Virtual Machines"
            },
            new PriceRecord
            {
                ArmSkuName = "P1v3",
                UnitPrice = 0.19,
                UnitOfMeasure = "1 Hour",
                MeterName = "P1 v3",
                ServiceName = "Azure App Service"
            },
            new PriceRecord
            {
                SkuName = "Standard_LRS",
                UnitPrice = 0.018,
                UnitOfMeasure = "1 GB/Month",
                MeterName = "Hot LRS Data Stored",
                ServiceName = "Storage",
                ProductName = "Tables"
            }
        });

        var registry = new MapperRegistry();
        var aggregator = new CostAggregator(registry, mockPricing);
        var report = await aggregator.GenerateReportAsync(resources);

        // 4 mapped resources (VM, Storage, App Service Plan, NIC)
        Assert.Equal(4, report.Lines.Count);
        Assert.Empty(report.Warnings);

        // Grand total should be sum of all mapped costs
        Assert.Equal(report.Lines.Sum(l => l.MonthlyCost), report.GrandTotal);
        Assert.True(report.GrandTotal > 0);
    }

    [Fact]
    public async Task RoundTrip_OutputFormats_AllProduceOutput()
    {
        var armJson = File.ReadAllText(GetFixturePath("simple-vm.arm.json"));
        var extractor = new ResourceExtractor();
        var resources = extractor.Extract(armJson);

        var mockPricing = new MockPricingApiClient(new List<PriceRecord>
        {
            new PriceRecord
            {
                ArmSkuName = "Standard_D2s_v3",
                UnitPrice = 0.096,
                UnitOfMeasure = "1 Hour",
                MeterName = "D2s v3",
            }
        });

        var registry = new MapperRegistry();
        var aggregator = new CostAggregator(registry, mockPricing);
        var report = await aggregator.GenerateReportAsync(resources);

        // All four output formats should produce non-empty output
        var tableOutput = OutputFormatter.Format(report, "table", "test.bicep");
        var jsonOutput = OutputFormatter.Format(report, "json", "test.bicep");
        var csvOutput = OutputFormatter.Format(report, "csv", "test.bicep");
        var markdownOutput = OutputFormatter.Format(report, "markdown", "test.bicep");

        Assert.False(string.IsNullOrEmpty(tableOutput));
        Assert.False(string.IsNullOrEmpty(jsonOutput));
        Assert.False(string.IsNullOrEmpty(csvOutput));
        Assert.False(string.IsNullOrEmpty(markdownOutput));

        // JSON should be parseable
        var parsed = JsonDocument.Parse(jsonOutput);
        Assert.True(parsed.RootElement.TryGetProperty("grandTotal", out _));

        // CSV should have header
        Assert.StartsWith("ResourceName", csvOutput);

        // Table should have TOTAL
        Assert.Contains("ESTIMATED MONTHLY TOTAL", tableOutput);

        // Markdown should have table
        Assert.Contains("| Resource |", markdownOutput);
    }

    [Fact]
    public async Task RoundTrip_EmptyTemplate_ProducesEmptyReport()
    {
        var armJson = """
        {
            "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
            "contentVersion": "1.0.0.0",
            "resources": []
        }
        """;

        var extractor = new ResourceExtractor();
        var resources = extractor.Extract(armJson);
        var mockPricing = new MockPricingApiClient(new List<PriceRecord>());
        var registry = new MapperRegistry();
        var aggregator = new CostAggregator(registry, mockPricing);
        var report = await aggregator.GenerateReportAsync(resources);

        Assert.Empty(report.Lines);
        Assert.Empty(report.Warnings);
        Assert.Equal(0m, report.GrandTotal);
    }

    [Fact]
    public async Task RoundTrip_ConditionFalseResources_Excluded()
    {
        var armJson = """
        {
            "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
            "contentVersion": "1.0.0.0",
            "resources": [
                {
                    "condition": false,
                    "type": "Microsoft.Compute/virtualMachines",
                    "apiVersion": "2023-09-01",
                    "name": "skipped-vm",
                    "location": "eastus",
                    "properties": { "hardwareProfile": { "vmSize": "Standard_D4s_v3" } }
                },
                {
                    "type": "Microsoft.Compute/virtualMachines",
                    "apiVersion": "2023-09-01",
                    "name": "included-vm",
                    "location": "eastus",
                    "properties": { "hardwareProfile": { "vmSize": "Standard_D2s_v3" } }
                }
            ]
        }
        """;

        var extractor = new ResourceExtractor();
        var resources = extractor.Extract(armJson);

        Assert.Single(resources);
        Assert.Equal("included-vm", resources[0].Name);

        var mockPricing = new MockPricingApiClient(new List<PriceRecord>
        {
            new PriceRecord
            {
                ArmSkuName = "Standard_D2s_v3",
                UnitPrice = 0.096,
                UnitOfMeasure = "1 Hour",
                MeterName = "D2s v3",
            }
        });

        var registry = new MapperRegistry();
        var aggregator = new CostAggregator(registry, mockPricing);
        var report = await aggregator.GenerateReportAsync(resources);

        Assert.Single(report.Lines);
        Assert.Equal("included-vm", report.Lines[0].ResourceName);
        Assert.Equal(70.08m, report.GrandTotal);
    }

    [Fact]
    public async Task RoundTrip_NewMappers_HandleAllPriorityTypes()
    {
        var armJson = """
        {
            "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
            "contentVersion": "1.0.0.0",
            "resources": [
                {
                    "type": "Microsoft.ContainerService/managedClusters",
                    "apiVersion": "2023-10-01",
                    "name": "my-aks",
                    "location": "eastus",
                    "properties": {
                        "agentPoolProfiles": [{ "vmSize": "Standard_DS2_v2", "count": 3 }]
                    }
                },
                {
                    "type": "Microsoft.Network/publicIPAddresses",
                    "apiVersion": "2023-05-01",
                    "name": "my-pip",
                    "location": "eastus",
                    "sku": { "name": "Standard" },
                    "properties": { "publicIPAllocationMethod": "Static" }
                },
                {
                    "type": "Microsoft.DocumentDB/databaseAccounts",
                    "apiVersion": "2023-04-15",
                    "name": "my-cosmos",
                    "location": "eastus",
                    "properties": { "databaseAccountOfferType": "Standard" }
                },
                {
                    "type": "Microsoft.KeyVault/vaults",
                    "apiVersion": "2023-07-01",
                    "name": "my-kv",
                    "location": "eastus",
                    "properties": {}
                },
                {
                    "type": "Microsoft.ContainerRegistry/registries",
                    "apiVersion": "2023-07-01",
                    "name": "my-acr",
                    "location": "eastus",
                    "sku": { "name": "Basic" },
                    "properties": {}
                },
                {
                    "type": "Microsoft.Network/loadBalancers",
                    "apiVersion": "2023-05-01",
                    "name": "my-lb",
                    "location": "eastus",
                    "sku": { "name": "Standard" },
                    "properties": {}
                }
            ]
        }
        """;

        var extractor = new ResourceExtractor();
        var resources = extractor.Extract(armJson);

        Assert.Equal(6, resources.Count);

        // All resource types should have mappers
        var registry = new MapperRegistry();
        foreach (var resource in resources)
        {
            var mapper = registry.GetMapper(resource);
            Assert.NotNull(mapper);
        }
    }

    private static string GetFixturePath(string fileName) =>
        Path.Combine("fixtures", fileName);
}
