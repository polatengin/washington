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

        // P3 resource types
        var diskResource = CreateResource("Microsoft.Compute/disks");
        var funcResource = CreateResource("Microsoft.Web/sites", properties: new { _kind = "functionapp" });
        var sqlMiResource = CreateResource("Microsoft.Sql/managedInstances");
        var vpnGwResource = CreateResource("Microsoft.Network/virtualNetworkGateways");
        var firewallResource = CreateResource("Microsoft.Network/azureFirewalls");
        var peResource = CreateResource("Microsoft.Network/privateEndpoints");
        var logResource = CreateResource("Microsoft.OperationalInsights/workspaces");
        var redisResource = CreateResource("Microsoft.Cache/redis");
        var ehResource = CreateResource("Microsoft.EventHub/namespaces");
        var sbResource = CreateResource("Microsoft.ServiceBus/namespaces");
        var fdResource = CreateResource("Microsoft.Cdn/profiles");
        var caResource = CreateResource("Microsoft.App/containerApps");
        var pgResource = CreateResource("Microsoft.DBforPostgreSQL/flexibleServers");
        var mysqlResource = CreateResource("Microsoft.DBforMySQL/flexibleServers");
        var apimResource = CreateResource("Microsoft.ApiManagement/service");
        var swaResource = CreateResource("Microsoft.Web/staticSites");
        var signalrResource = CreateResource("Microsoft.SignalRService/signalR");

        Assert.NotNull(registry.GetMapper(diskResource));
        Assert.NotNull(registry.GetMapper(funcResource));
        Assert.NotNull(registry.GetMapper(sqlMiResource));
        Assert.NotNull(registry.GetMapper(vpnGwResource));
        Assert.NotNull(registry.GetMapper(firewallResource));
        Assert.NotNull(registry.GetMapper(peResource));
        Assert.NotNull(registry.GetMapper(logResource));
        Assert.NotNull(registry.GetMapper(redisResource));
        Assert.NotNull(registry.GetMapper(ehResource));
        Assert.NotNull(registry.GetMapper(sbResource));
        Assert.NotNull(registry.GetMapper(fdResource));
        Assert.NotNull(registry.GetMapper(caResource));
        Assert.NotNull(registry.GetMapper(pgResource));
        Assert.NotNull(registry.GetMapper(mysqlResource));
        Assert.NotNull(registry.GetMapper(apimResource));
        Assert.NotNull(registry.GetMapper(swaResource));
        Assert.NotNull(registry.GetMapper(signalrResource));
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

    [Fact]
    public void ManagedDiskMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new ManagedDiskMapper();
        var resource = CreateResource("Microsoft.Compute/disks",
            sku: new { name = "Premium_LRS" },
            properties: new { diskSizeGB = 256 });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Storage", queries[0].ServiceName);
        Assert.Equal("Premium_LRS", queries[0].SkuName);
    }

    [Fact]
    public void FunctionAppMapper_CanMap_FunctionApp()
    {
        var mapper = new FunctionAppMapper();
        var resource = CreateResource("Microsoft.Web/sites",
            properties: new { _kind = "functionapp" });

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void FunctionAppMapper_CannotMap_RegularWebApp()
    {
        var mapper = new FunctionAppMapper();
        var resource = CreateResource("Microsoft.Web/sites",
            properties: new { _kind = "app" });

        Assert.False(mapper.CanMap(resource));
    }

    [Fact]
    public void FunctionAppMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new FunctionAppMapper();
        var resource = CreateResource("Microsoft.Web/sites",
            properties: new { _kind = "functionapp" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Functions", queries[0].ServiceName);
    }

    [Fact]
    public void SqlManagedInstanceMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new SqlManagedInstanceMapper();
        var resource = CreateResource("Microsoft.Sql/managedInstances",
            sku: new { name = "GP_Gen5", capacity = 8 });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("SQL Managed Instance", queries[0].ServiceName);
        Assert.Equal("GP_Gen5", queries[0].SkuName);
    }

    [Fact]
    public void VirtualNetworkGatewayMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new VirtualNetworkGatewayMapper();
        var resource = CreateResource("Microsoft.Network/virtualNetworkGateways",
            properties: new { sku = new { name = "VpnGw2" } });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("VPN Gateway", queries[0].ServiceName);
        Assert.Equal("VpnGw2", queries[0].ArmSkuName);
    }

    [Fact]
    public void AzureFirewallMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new AzureFirewallMapper();
        var resource = CreateResource("Microsoft.Network/azureFirewalls",
            sku: new { tier = "Premium" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Firewall", queries[0].ServiceName);
        Assert.Equal("Premium", queries[0].SkuName);
    }

    [Fact]
    public void PrivateEndpointMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new PrivateEndpointMapper();
        var resource = CreateResource("Microsoft.Network/privateEndpoints");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Private Link", queries[0].ServiceName);
    }

    [Fact]
    public void LogAnalyticsWorkspaceMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new LogAnalyticsWorkspaceMapper();
        var resource = CreateResource("Microsoft.OperationalInsights/workspaces",
            sku: new { name = "PerGB2018" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Log Analytics", queries[0].ServiceName);
    }

    [Fact]
    public void RedisCacheMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new RedisCacheMapper();
        var resource = CreateResource("Microsoft.Cache/redis",
            sku: new { name = "Standard", family = "C", capacity = 1 });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Cache for Redis", queries[0].ServiceName);
        Assert.Equal("Standard", queries[0].SkuName);
    }

    [Fact]
    public void EventHubMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new EventHubMapper();
        var resource = CreateResource("Microsoft.EventHub/namespaces",
            sku: new { name = "Standard", capacity = 2 });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Event Hubs", queries[0].ServiceName);
        Assert.Equal("Standard", queries[0].SkuName);
    }

    [Fact]
    public void ServiceBusMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new ServiceBusMapper();
        var resource = CreateResource("Microsoft.ServiceBus/namespaces",
            sku: new { name = "Premium", capacity = 1 });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Service Bus", queries[0].ServiceName);
        Assert.Equal("Premium", queries[0].SkuName);
    }

    [Fact]
    public void FrontDoorMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new FrontDoorMapper();
        var resource = CreateResource("Microsoft.Cdn/profiles",
            sku: new { name = "Standard_AzureFrontDoor" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Front Door Service", queries[0].ServiceName);
    }

    [Fact]
    public void ContainerAppMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new ContainerAppMapper();
        var resource = CreateResource("Microsoft.App/containerApps",
            properties: new { template = new { containers = new[] { new { name = "app", resources = new { cpu = 0.5, memory = "1Gi" } } } } });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Container Apps", queries[0].ServiceName);
    }

    [Fact]
    public void PostgreSqlFlexibleServerMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new PostgreSqlFlexibleServerMapper();
        var resource = CreateResource("Microsoft.DBforPostgreSQL/flexibleServers",
            sku: new { name = "Standard_D2s_v3" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Database for PostgreSQL", queries[0].ServiceName);
        Assert.Equal("Standard_D2s_v3", queries[0].ArmSkuName);
    }

    [Fact]
    public void MySqlFlexibleServerMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new MySqlFlexibleServerMapper();
        var resource = CreateResource("Microsoft.DBforMySQL/flexibleServers",
            sku: new { name = "Standard_D2ds_v4" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Database for MySQL", queries[0].ServiceName);
        Assert.Equal("Standard_D2ds_v4", queries[0].ArmSkuName);
    }

    [Fact]
    public void ApiManagementMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new ApiManagementMapper();
        var resource = CreateResource("Microsoft.ApiManagement/service",
            sku: new { name = "Developer", capacity = 1 });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("API Management", queries[0].ServiceName);
        Assert.Equal("Developer", queries[0].SkuName);
    }

    [Fact]
    public void StaticWebAppMapper_FreeTier_ReturnsZero()
    {
        var mapper = new StaticWebAppMapper();
        var resource = CreateResource("Microsoft.Web/staticSites",
            sku: new { name = "Free" });

        var cost = mapper.CalculateCost(resource, new List<PriceRecord>());

        Assert.Equal(0m, cost.Amount);
        Assert.Contains("Free", cost.Details);
    }

    [Fact]
    public void StaticWebAppMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new StaticWebAppMapper();
        var resource = CreateResource("Microsoft.Web/staticSites",
            sku: new { name = "Standard" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Static Web Apps", queries[0].ServiceName);
    }

    [Fact]
    public void SignalRMapper_FreeTier_ReturnsZero()
    {
        var mapper = new SignalRMapper();
        var resource = CreateResource("Microsoft.SignalRService/signalR",
            sku: new { name = "Free_F1" });

        var cost = mapper.CalculateCost(resource, new List<PriceRecord>());

        Assert.Equal(0m, cost.Amount);
        Assert.Contains("Free", cost.Details);
    }

    [Fact]
    public void SignalRMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new SignalRMapper();
        var resource = CreateResource("Microsoft.SignalRService/signalR",
            sku: new { name = "Standard_S1", capacity = 1 });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("SignalR Service", queries[0].ServiceName);
        Assert.Equal("Standard_S1", queries[0].SkuName);
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
