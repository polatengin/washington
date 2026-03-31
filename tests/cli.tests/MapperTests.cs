using System.Text.Json;
using Washington.Mappers;
using Washington.Models;
using Xunit;

namespace Washington.Tests;

public class MapperTests
{
    [Fact]
    public void VirtualMachineMapper_CanMap_CorrectType()
    {
        var mapper = new VirtualMachineMapper();
        var resource = CreateResource("Microsoft.Compute/virtualMachines",
            properties: new { hardwareProfile = new { vmSize = "Standard_D2s_v3" } });

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void VirtualMachineMapper_CannotMap_WrongType()
    {
        var mapper = new VirtualMachineMapper();
        var resource = CreateResource("Microsoft.Storage/storageAccounts");

        Assert.False(mapper.CanMap(resource));
    }

    [Fact]
    public void VirtualMachineMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new VirtualMachineMapper();
        var resource = CreateResource("Microsoft.Compute/virtualMachines",
            properties: new { hardwareProfile = new { vmSize = "Standard_D4s_v3" } });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Virtual Machines", queries[0].ServiceName);
        Assert.Equal("Standard_D4s_v3", queries[0].ArmSkuName);
        Assert.Equal("eastus", queries[0].ArmRegionName);
    }

    [Fact]
    public void StorageAccountMapper_CanMap_CorrectType()
    {
        var mapper = new StorageAccountMapper();
        var resource = CreateResource("Microsoft.Storage/storageAccounts");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void SqlDatabaseMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new SqlDatabaseMapper();
        var resource = CreateResource("Microsoft.Sql/servers/databases",
            sku: new { name = "S3" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("SQL Database", queries[0].ServiceName);
        Assert.Equal("S3", queries[0].SkuName);
    }

    [Fact]
    public void AppServicePlanMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new AppServicePlanMapper();
        var resource = CreateResource("Microsoft.Web/serverfarms",
            sku: new { name = "P1v3" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure App Service", queries[0].ServiceName);
        Assert.Equal("P1v3", queries[0].ArmSkuName);
    }

    [Fact]
    public void MapperRegistry_FindsCorrectMapper()
    {
        var registry = new MapperRegistry();

        var vmResource = CreateResource("Microsoft.Compute/virtualMachines");
        var storageResource = CreateResource("Microsoft.Storage/storageAccounts");
        var unknownResource = CreateResource("Microsoft.Network/networkInterfaces");

        Assert.NotNull(registry.GetMapper(vmResource));
        Assert.NotNull(registry.GetMapper(storageResource));
        Assert.Null(registry.GetMapper(unknownResource));
    }

    [Fact]
    public void VirtualMachineMapper_CalculateCost_WithPrices()
    {
        var mapper = new VirtualMachineMapper();
        var resource = CreateResource("Microsoft.Compute/virtualMachines",
            properties: new { hardwareProfile = new { vmSize = "Standard_D2s_v3" } });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                ArmSkuName = "Standard_D2s_v3",
                UnitPrice = 0.096,
                UnitOfMeasure = "1 Hour",
                MeterName = "D2s v3",
                CurrencyCode = "USD"
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(70.08m, cost.Amount);
        Assert.Contains("Standard_D2s_v3", cost.Details);
    }

    private static ResourceDescriptor CreateResource(
        string resourceType,
        object? properties = null,
        object? sku = null,
        string location = "eastus",
        string name = "test-resource")
    {
        var propsDict = new Dictionary<string, JsonElement>();
        if (properties != null)
        {
            var json = JsonSerializer.Serialize(properties);
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                propsDict[prop.Name] = prop.Value.Clone();
            }
        }

        var skuDict = new Dictionary<string, JsonElement>();
        if (sku != null)
        {
            var json = JsonSerializer.Serialize(sku);
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                skuDict[prop.Name] = prop.Value.Clone();
            }
        }

        return new ResourceDescriptor(resourceType, "2023-09-01", name, location, skuDict, propsDict);
    }
}
