using System.Text.Json;
using Washington.Mappers;
using Washington.Models;
using Washington.Services;
using Xunit;

namespace Washington.Tests;

public class CostAggregatorTests
{
    [Fact]
    public async Task GenerateReport_SingleResource_CorrectTotal()
    {
        var mockPricingClient = new MockPricingApiClient(new List<PriceRecord>
        {
            new PriceRecord
            {
                ArmSkuName = "Standard_D2s_v3",
                UnitPrice = 0.096,
                UnitOfMeasure = "1 Hour",
                MeterName = "D2s v3",
                CurrencyCode = "USD",
                ServiceName = "Virtual Machines"
            }
        });

        var registry = new MapperRegistry();
        var aggregator = new CostAggregator(registry, mockPricingClient);
        var resources = new List<ResourceDescriptor>
        {
            CreateResource("Microsoft.Compute/virtualMachines", "test-vm",
                properties: new { hardwareProfile = new { vmSize = "Standard_D2s_v3" } })
        };

        var report = await aggregator.GenerateReportAsync(resources, "USD");

        Assert.Single(report.Lines);
        Assert.Equal(70.08m, report.Lines[0].MonthlyCost);
        Assert.Equal(70.08m, report.GrandTotal);
        Assert.Equal("USD", report.Currency);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public async Task GenerateReport_MultipleResources_AggregatesTotal()
    {
        var mockPricingClient = new MockPricingApiClient(new List<PriceRecord>
        {
            new PriceRecord
            {
                ArmSkuName = "Standard_D2s_v3",
                UnitPrice = 0.096,
                UnitOfMeasure = "1 Hour",
                MeterName = "D2s v3",
                CurrencyCode = "USD",
                ServiceName = "Virtual Machines"
            },
            new PriceRecord
            {
                ArmSkuName = "P1v3",
                UnitPrice = 0.19,
                UnitOfMeasure = "1 Hour",
                MeterName = "P1 v3",
                CurrencyCode = "USD",
                ServiceName = "Azure App Service"
            }
        });

        var registry = new MapperRegistry();
        var aggregator = new CostAggregator(registry, mockPricingClient);
        var resources = new List<ResourceDescriptor>
        {
            CreateResource("Microsoft.Compute/virtualMachines", "web-vm",
                properties: new { hardwareProfile = new { vmSize = "Standard_D2s_v3" } }),
            CreateResource("Microsoft.Web/serverfarms", "app-plan",
                sku: new { name = "P1v3" })
        };

        var report = await aggregator.GenerateReportAsync(resources, "USD");

        Assert.Equal(2, report.Lines.Count);
        Assert.True(report.GrandTotal > 0);
        Assert.Equal(report.Lines.Sum(l => l.MonthlyCost), report.GrandTotal);
    }

    [Fact]
    public async Task GenerateReport_UnmappedResource_AddsWarning()
    {
        var mockPricingClient = new MockPricingApiClient(new List<PriceRecord>());
        var registry = new MapperRegistry();
        var aggregator = new CostAggregator(registry, mockPricingClient);
        var resources = new List<ResourceDescriptor>
        {
            CreateResource("Microsoft.Network/networkInterfaces", "nic-1")
        };

        var report = await aggregator.GenerateReportAsync(resources, "USD");

        Assert.Empty(report.Lines);
        Assert.Single(report.Warnings);
        Assert.Contains("Microsoft.Network/networkInterfaces", report.Warnings[0]);
        Assert.Equal(0m, report.GrandTotal);
    }

    [Fact]
    public async Task GenerateReport_MixedResources_ReportsAndWarns()
    {
        var mockPricingClient = new MockPricingApiClient(new List<PriceRecord>
        {
            new PriceRecord
            {
                ArmSkuName = "Standard_D2s_v3",
                UnitPrice = 0.096,
                UnitOfMeasure = "1 Hour",
                MeterName = "D2s v3",
                CurrencyCode = "USD"
            }
        });

        var registry = new MapperRegistry();
        var aggregator = new CostAggregator(registry, mockPricingClient);
        var resources = new List<ResourceDescriptor>
        {
            CreateResource("Microsoft.Compute/virtualMachines", "web-vm",
                properties: new { hardwareProfile = new { vmSize = "Standard_D2s_v3" } }),
            CreateResource("Microsoft.Network/networkInterfaces", "nic-1")
        };

        var report = await aggregator.GenerateReportAsync(resources, "USD");

        Assert.Single(report.Lines);
        Assert.Single(report.Warnings);
        Assert.True(report.GrandTotal > 0);
    }

    [Fact]
    public async Task GenerateReport_EmptyResources_ReturnsEmptyReport()
    {
        var mockPricingClient = new MockPricingApiClient(new List<PriceRecord>());
        var registry = new MapperRegistry();
        var aggregator = new CostAggregator(registry, mockPricingClient);

        var report = await aggregator.GenerateReportAsync(new List<ResourceDescriptor>(), "USD");

        Assert.Empty(report.Lines);
        Assert.Empty(report.Warnings);
        Assert.Equal(0m, report.GrandTotal);
        Assert.Equal("USD", report.Currency);
    }

    [Fact]
    public async Task GenerateReport_RespectsCustomCurrency()
    {
        var mockPricingClient = new MockPricingApiClient(new List<PriceRecord>
        {
            new PriceRecord
            {
                ArmSkuName = "Standard_D2s_v3",
                UnitPrice = 0.088,
                UnitOfMeasure = "1 Hour",
                MeterName = "D2s v3",
                CurrencyCode = "EUR"
            }
        });

        var registry = new MapperRegistry();
        var aggregator = new CostAggregator(registry, mockPricingClient);
        var resources = new List<ResourceDescriptor>
        {
            CreateResource("Microsoft.Compute/virtualMachines", "vm-1",
                properties: new { hardwareProfile = new { vmSize = "Standard_D2s_v3" } })
        };

        var report = await aggregator.GenerateReportAsync(resources, "EUR");

        Assert.Equal("EUR", report.Currency);
    }

    private static ResourceDescriptor CreateResource(
        string resourceType, string name,
        object? properties = null, object? sku = null,
        string location = "eastus")
    {
        var propsDict = new Dictionary<string, JsonElement>();
        if (properties != null)
        {
            var json = JsonSerializer.Serialize(properties);
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
                propsDict[prop.Name] = prop.Value.Clone();
        }

        var skuDict = new Dictionary<string, JsonElement>();
        if (sku != null)
        {
            var json = JsonSerializer.Serialize(sku);
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
                skuDict[prop.Name] = prop.Value.Clone();
        }

        return new ResourceDescriptor(resourceType, "2023-09-01", name, location, skuDict, propsDict);
    }
}

/// <summary>
/// Mock PricingApiClient that returns predetermined prices without calling the real API.
/// </summary>
internal class MockPricingApiClient : PricingApiClient
{
    private readonly List<PriceRecord> _prices;

    public MockPricingApiClient(List<PriceRecord> prices) : base(null!)
    {
        _prices = prices;
    }

    public override Task<List<PriceRecord>> QueryPricesAsync(PricingQuery query)
    {
        return Task.FromResult(_prices);
    }
}
