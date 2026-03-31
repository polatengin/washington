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
        var aksResource = CreateResource("Microsoft.ContainerService/managedClusters");
        var publicIpResource = CreateResource("Microsoft.Network/publicIPAddresses");
        var appGwResource = CreateResource("Microsoft.Network/applicationGateways");
        var cosmosResource = CreateResource("Microsoft.DocumentDB/databaseAccounts");
        var kvResource = CreateResource("Microsoft.KeyVault/vaults");
        var acrResource = CreateResource("Microsoft.ContainerRegistry/registries");
        var lbResource = CreateResource("Microsoft.Network/loadBalancers");

        Assert.NotNull(registry.GetMapper(vmResource));
        Assert.NotNull(registry.GetMapper(storageResource));
        Assert.Null(registry.GetMapper(unknownResource));
        Assert.NotNull(registry.GetMapper(aksResource));
        Assert.NotNull(registry.GetMapper(publicIpResource));
        Assert.NotNull(registry.GetMapper(appGwResource));
        Assert.NotNull(registry.GetMapper(cosmosResource));
        Assert.NotNull(registry.GetMapper(kvResource));
        Assert.NotNull(registry.GetMapper(acrResource));
        Assert.NotNull(registry.GetMapper(lbResource));
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

    [Fact]
    public void ManagedClusterMapper_CanMap_CorrectType()
    {
        var mapper = new ManagedClusterMapper();
        var resource = CreateResource("Microsoft.ContainerService/managedClusters",
            properties: new { agentPoolProfiles = new[] { new { vmSize = "Standard_DS2_v2", count = 3 } } });

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void ManagedClusterMapper_BuildQueries_UsesNodePoolVmSize()
    {
        var mapper = new ManagedClusterMapper();
        var resource = CreateResource("Microsoft.ContainerService/managedClusters",
            properties: new { agentPoolProfiles = new[] { new { vmSize = "Standard_DS2_v2", count = 3 } } });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Virtual Machines", queries[0].ServiceName);
        Assert.Equal("Standard_DS2_v2", queries[0].ArmSkuName);
    }

    [Fact]
    public void ManagedClusterMapper_CalculateCost_MultipliesNodeCount()
    {
        var mapper = new ManagedClusterMapper();
        var resource = CreateResource("Microsoft.ContainerService/managedClusters",
            properties: new { agentPoolProfiles = new[] { new { vmSize = "Standard_DS2_v2", count = 3 } } });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                ArmSkuName = "Standard_DS2_v2",
                UnitPrice = 0.10,
                UnitOfMeasure = "1 Hour",
                MeterName = "DS2 v2",
                CurrencyCode = "USD"
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        // 0.10 * 730 * 3 = 219.00
        Assert.Equal(219.0m, cost.Amount);
        Assert.Contains("3×", cost.Details);
    }

    [Fact]
    public void PublicIpAddressMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new PublicIpAddressMapper();
        var resource = CreateResource("Microsoft.Network/publicIPAddresses",
            sku: new { name = "Standard" },
            properties: new { publicIPAllocationMethod = "Static" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Virtual Network", queries[0].ServiceName);
    }

    [Fact]
    public void ApplicationGatewayMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new ApplicationGatewayMapper();
        var resource = CreateResource("Microsoft.Network/applicationGateways",
            sku: new { name = "Standard_v2", tier = "Standard_v2", capacity = 2 });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Application Gateway", queries[0].ServiceName);
    }

    [Fact]
    public void CosmosDbAccountMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new CosmosDbAccountMapper();
        var resource = CreateResource("Microsoft.DocumentDB/databaseAccounts",
            properties: new { databaseAccountOfferType = "Standard" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Cosmos DB", queries[0].ServiceName);
    }

    [Fact]
    public void KeyVaultMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new KeyVaultMapper();
        var resource = CreateResource("Microsoft.KeyVault/vaults");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Key Vault", queries[0].ServiceName);
    }

    [Fact]
    public void ContainerRegistryMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new ContainerRegistryMapper();
        var resource = CreateResource("Microsoft.ContainerRegistry/registries",
            sku: new { name = "Premium" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Container Registry", queries[0].ServiceName);
        Assert.Equal("Premium", queries[0].SkuName);
    }

    [Fact]
    public void LoadBalancerMapper_BasicSku_ReturnsFree()
    {
        var mapper = new LoadBalancerMapper();
        var resource = CreateResource("Microsoft.Network/loadBalancers",
            sku: new { name = "Basic" });

        var cost = mapper.CalculateCost(resource, new List<PriceRecord>());

        Assert.Equal(0m, cost.Amount);
        Assert.Contains("free", cost.Details);
    }

    [Fact]
    public void LoadBalancerMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new LoadBalancerMapper();
        var resource = CreateResource("Microsoft.Network/loadBalancers",
            sku: new { name = "Standard" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Load Balancer", queries[0].ServiceName);
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
