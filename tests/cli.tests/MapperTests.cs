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
    public void StorageBlobServiceMapper_CanMap_CorrectType()
    {
        var mapper = new StorageBlobServiceMapper();
        var resource = CreateResource("Microsoft.Storage/storageAccounts/blobServices");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void StorageBlobContainerMapper_CanMap_CorrectType()
    {
        var mapper = new StorageBlobContainerMapper();
        var resource = CreateResource("Microsoft.Storage/storageAccounts/blobServices/containers");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void StorageAccountMapper_BuildQueries_UsesModeledStorageMeter()
    {
        var mapper = new StorageAccountMapper();
        var resource = CreateResource(
            "Microsoft.Storage/storageAccounts",
            properties: new { _kind = "StorageV2", accessTier = "Hot" },
            sku: new { name = "Standard_LRS" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Storage", queries[0].ServiceName);
        Assert.Equal("Hot LRS Data Stored", queries[0].MeterName);
        Assert.Null(queries[0].ProductName);
    }

    [Fact]
    public void StorageAccountMapper_CalculateCost_PrefersBlobPricingAndUses1000GbBaseline()
    {
        var mapper = new StorageAccountMapper();
        var resource = CreateResource(
            "Microsoft.Storage/storageAccounts",
            properties: new { _kind = "StorageV2", accessTier = "Hot" },
            sku: new { name = "Standard_LRS" });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                MeterName = "Hot LRS Data Stored",
                ProductName = "Files v2",
                UnitPrice = 0.0287,
                TierMinimumUnits = 0
            },
            new PriceRecord
            {
                MeterName = "Hot LRS Data Stored",
                ProductName = "General Block Blob v2",
                UnitPrice = 0.0208,
                TierMinimumUnits = 0
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(20.80m, cost.Amount);
        Assert.Contains("1000 GB", cost.Details);
        Assert.Contains("$0.0208/GB", cost.Details);
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
    public void SqlServerMapper_CanMap_CorrectType()
    {
        var mapper = new SqlServerMapper();
        var resource = CreateResource("Microsoft.Sql/servers");

        Assert.True(mapper.CanMap(resource));
        Assert.Empty(mapper.BuildQueries(resource));
        Assert.Equal(0m, mapper.CalculateCost(resource, new List<PriceRecord>()).Amount);
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
        Assert.Equal("P1v3", queries[0].SkuName);
        Assert.Null(queries[0].ArmSkuName);
    }

    [Fact]
    public void AppServicePlanMapper_CalculateCost_PrefersWindowsPriceByDefault()
    {
        var mapper = new AppServicePlanMapper();
        var resource = CreateResource("Microsoft.Web/serverfarms",
            sku: new { name = "B1" });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                SkuName = "B1",
                ProductName = "Azure App Service Basic Plan - Linux",
                UnitPrice = 0.017,
                UnitOfMeasure = "1 Hour"
            },
            new PriceRecord
            {
                SkuName = "B1",
                ProductName = "Azure App Service Basic Plan",
                UnitPrice = 0.075,
                UnitOfMeasure = "1 Hour"
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(54.75m, cost.Amount);
        Assert.Contains("$0.0750/hr", cost.Details);
    }

    [Fact]
    public void AppServicePlanMapper_CalculateCost_UsesLinuxPrice_WhenReserved()
    {
        var mapper = new AppServicePlanMapper();
        var resource = CreateResource("Microsoft.Web/serverfarms",
            properties: new { reserved = true },
            sku: new { name = "B1" });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                SkuName = "B1",
                ProductName = "Azure App Service Basic Plan",
                UnitPrice = 0.075,
                UnitOfMeasure = "1 Hour"
            },
            new PriceRecord
            {
                SkuName = "B1",
                ProductName = "Azure App Service Basic Plan - Linux",
                UnitPrice = 0.017,
                UnitOfMeasure = "1 Hour"
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(12.41m, cost.Amount);
        Assert.Contains("$0.0170/hr", cost.Details);
    }

    [Fact]
    public void MapperRegistry_FindsCorrectMapper()
    {
        var registry = new MapperRegistry();

        var vmResource = CreateResource("Microsoft.Compute/virtualMachines");
        var storageResource = CreateResource("Microsoft.Storage/storageAccounts");
        var blobServiceResource = CreateResource("Microsoft.Storage/storageAccounts/blobServices");
        var blobContainerResource = CreateResource("Microsoft.Storage/storageAccounts/blobServices/containers");
        var nicResource = CreateResource("Microsoft.Network/networkInterfaces");
        var aksResource = CreateResource("Microsoft.ContainerService/managedClusters");
        var publicIpResource = CreateResource("Microsoft.Network/publicIPAddresses");
        var appGwResource = CreateResource("Microsoft.Network/applicationGateways");
        var cosmosResource = CreateResource("Microsoft.DocumentDB/databaseAccounts");
        var kvResource = CreateResource("Microsoft.KeyVault/vaults");
        var acrResource = CreateResource("Microsoft.ContainerRegistry/registries");
        var lbResource = CreateResource("Microsoft.Network/loadBalancers");
        var unknownResource = CreateResource("Microsoft.CustomProviders/resourceProviders");

        Assert.NotNull(registry.GetMapper(vmResource));
        Assert.NotNull(registry.GetMapper(storageResource));
        Assert.NotNull(registry.GetMapper(blobServiceResource));
        Assert.NotNull(registry.GetMapper(blobContainerResource));
        Assert.NotNull(registry.GetMapper(nicResource));
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
        var webAppResource = CreateResource("Microsoft.Web/sites", properties: new { _kind = "app" });
        var sqlServerResource = CreateResource("Microsoft.Sql/servers");
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

        var functionAppMapper = registry.GetMapper(funcResource);
        var webAppMapper = registry.GetMapper(webAppResource);
        var sqlServerMapper = registry.GetMapper(sqlServerResource);

        Assert.NotNull(registry.GetMapper(diskResource));
        Assert.NotNull(functionAppMapper);
        Assert.NotNull(webAppMapper);
        Assert.NotNull(sqlServerMapper);
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
        Assert.IsType<FunctionAppMapper>(functionAppMapper);
        Assert.IsType<WebAppMapper>(webAppMapper);
        Assert.IsType<SqlServerMapper>(sqlServerMapper);
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
    public void CosmosDbAccountMapper_CalculateCost_IgnoresFreeTierPrice_WhenDisabled()
    {
        var mapper = new CosmosDbAccountMapper();
        var resource = CreateResource("Microsoft.DocumentDB/databaseAccounts",
            properties: new { databaseAccountOfferType = "Standard", enableFreeTier = false });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                MeterName = "100 RU/s",
                SkuName = "Free Tier",
                UnitOfMeasure = "1/Hour",
                UnitPrice = 0.0
            },
            new PriceRecord
            {
                MeterName = "100 RU/s",
                SkuName = "RUs",
                UnitOfMeasure = "1/Hour",
                UnitPrice = 0.008
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(23.36m, cost.Amount);
        Assert.Contains("$0.0080/100 RU/hr", cost.Details);
    }

    [Fact]
    public void CosmosDbAccountMapper_CalculateCost_UsesFreeTierPrice_WhenEnabled()
    {
        var mapper = new CosmosDbAccountMapper();
        var resource = CreateResource("Microsoft.DocumentDB/databaseAccounts",
            properties: new { databaseAccountOfferType = "Standard", enableFreeTier = true });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                MeterName = "100 RU/s",
                SkuName = "RUs",
                UnitOfMeasure = "1/Hour",
                UnitPrice = 0.008
            },
            new PriceRecord
            {
                MeterName = "100 RU/s",
                SkuName = "Free Tier",
                UnitOfMeasure = "1/Hour",
                UnitPrice = 0.0
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(0m, cost.Amount);
        Assert.Contains("$0.0000/100 RU/hr", cost.Details);
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
    public void KeyVaultMapper_BuildQueries_UsesAdvancedOperationsMeterAndNormalizedSku()
    {
        var mapper = new KeyVaultMapper();
        var resource = CreateResource("Microsoft.KeyVault/vaults",
            properties: new { sku = new { name = "premium", family = "A" } });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Key Vault", queries[0].ProductName);
        Assert.Equal("Premium", queries[0].SkuName);
        Assert.Equal("Advanced Key Operations", queries[0].MeterName);
    }

    [Fact]
    public void KeyVaultMapper_CalculateCost_UsesOneMillionAdvancedOperations()
    {
        var mapper = new KeyVaultMapper();
        var resource = CreateResource("Microsoft.KeyVault/vaults",
            properties: new { sku = new { name = "standard", family = "A" } });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                MeterName = "Advanced Key Operations",
                SkuName = "Standard",
                UnitOfMeasure = "10K",
                UnitPrice = 0.15
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(15.0m, cost.Amount);
        Assert.Contains("1000000 Advanced Operations", cost.Details);
        Assert.Contains("$0.1500/10K ops", cost.Details);
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
    public void WebAppMapper_CanMap_RegularWebApp()
    {
        var mapper = new WebAppMapper();
        var resource = CreateResource("Microsoft.Web/sites",
            properties: new { _kind = "app", serverFarmId = "/subscriptions/test/serverfarms/plan" });

        Assert.True(mapper.CanMap(resource));
        Assert.Empty(mapper.BuildQueries(resource));
        Assert.Equal(0m, mapper.CalculateCost(resource, new List<PriceRecord>()).Amount);
    }

    [Fact]
    public void WebAppMapper_CannotMap_FunctionApp()
    {
        var mapper = new WebAppMapper();
        var resource = CreateResource("Microsoft.Web/sites",
            properties: new { _kind = "functionapp" });

        Assert.False(mapper.CanMap(resource));
    }

    [Fact]
    public void SqlManagedInstanceMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new SqlManagedInstanceMapper();
        var resource = CreateResource("Microsoft.Sql/managedInstances",
            sku: new { name = "GP_Gen5", tier = "GeneralPurpose", family = "Gen5", capacity = 8 });

        var queries = mapper.BuildQueries(resource);

        Assert.Equal(2, queries.Count);
        Assert.Equal("SQL Managed Instance", queries[0].ServiceName);
        Assert.Equal("SQLMI_GP_Compute_Gen5", queries[0].ArmSkuName);
        Assert.Equal("General Purpose Data Stored", queries[1].MeterName);
    }

    [Fact]
    public void SqlManagedInstanceMapper_CalculateCost_UsesGenericComputeAndStoragePrices()
    {
        var mapper = new SqlManagedInstanceMapper();
        var resource = CreateResource("Microsoft.Sql/managedInstances",
            properties: new { vCores = 4, storageSizeInGB = 32 },
            sku: new { name = "GP_Gen5", tier = "GeneralPurpose", family = "Gen5", capacity = 4 });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                ArmSkuName = "SQLMI_GP_Compute_Gen5",
                ProductName = "SQL Managed Instance General Purpose - Compute Gen5",
                MeterName = "vCore",
                SkuName = "vCore",
                UnitOfMeasure = "1 Hour",
                UnitPrice = 0.152218
            },
            new PriceRecord
            {
                ProductName = "SQL Managed Instance General Purpose - Storage",
                MeterName = "General Purpose Data Stored",
                SkuName = "General Purpose",
                UnitOfMeasure = "1 GB/Month",
                UnitPrice = 0.115
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(448.16m, decimal.Round(cost.Amount, 2));
        Assert.Contains("$0.1522/vCore/hr", cost.Details);
        Assert.Contains("32 GB storage", cost.Details);
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
    public void LogAnalyticsWorkspaceMapper_CalculateCost_UsesPaidTierAfterIncludedAllowance()
    {
        var mapper = new LogAnalyticsWorkspaceMapper();
        var resource = CreateResource("Microsoft.OperationalInsights/workspaces",
            properties: new { sku = new { name = "PerGB2018" }, retentionInDays = 30 });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                MeterName = "Analytics Logs Data Ingestion",
                UnitOfMeasure = "1 GB",
                TierMinimumUnits = 0,
                UnitPrice = 0.0
            },
            new PriceRecord
            {
                MeterName = "Analytics Logs Data Ingestion",
                UnitOfMeasure = "1 GB",
                TierMinimumUnits = 5,
                UnitPrice = 2.3
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(333.5m, cost.Amount);
        Assert.Contains("145 billable", cost.Details);
        Assert.Contains("$2.3000/GB", cost.Details);
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
    public void RedisCacheMapper_CalculateCost_MatchesExactCacheCodeAndTier()
    {
        var mapper = new RedisCacheMapper();
        var resource = CreateResource("Microsoft.Cache/redis",
            properties: new { sku = new { name = "Basic", family = "C", capacity = 1 } });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                MeterName = "C0",
                SkuName = "Basic",
                UnitOfMeasure = "1 Hour",
                UnitPrice = 0.022
            },
            new PriceRecord
            {
                MeterName = "C1",
                SkuName = "Standard",
                UnitOfMeasure = "1 Hour",
                UnitPrice = 0.138
            },
            new PriceRecord
            {
                MeterName = "C1",
                SkuName = "Basic",
                UnitOfMeasure = "1 Hour",
                UnitPrice = 0.055
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(40.15m, cost.Amount);
        Assert.Contains("$0.0550/hr", cost.Details);
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
    public void ServiceBusMapper_CalculateCost_StandardFallback_Uses100MOperations()
    {
        var mapper = new ServiceBusMapper();
        var resource = CreateResource("Microsoft.ServiceBus/namespaces",
            sku: new { name = "Standard", capacity = 1 });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                MeterName = "Standard Messaging Operations",
                UnitOfMeasure = "1M",
                TierMinimumUnits = 13,
                UnitPrice = 0.8
            },
            new PriceRecord
            {
                MeterName = "Standard Messaging Operations",
                UnitOfMeasure = "1M",
                TierMinimumUnits = 100,
                UnitPrice = 0.5
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(50.0m, cost.Amount);
        Assert.Contains("100M ops", cost.Details);
        Assert.Contains("$0.5000/M ops", cost.Details);
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
        Assert.Equal("Standard", queries[0].SkuName);
    }

    [Fact]
    public void ContainerAppMapper_CalculateCost_UsesIdleBaselineForMinimumReplicas()
    {
        var mapper = new ContainerAppMapper();
        var resource = CreateResource("Microsoft.App/containerApps",
            properties: new
            {
                template = new
                {
                    containers = new[] { new { name = "app", resources = new { cpu = 0.5, memory = "1Gi" } } },
                    scale = new { minReplicas = 1, maxReplicas = 1 }
                }
            });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                SkuName = "Standard",
                MeterName = "Standard vCPU Active Usage",
                UnitOfMeasure = "1 Second",
                UnitPrice = 0.000024
            },
            new PriceRecord
            {
                SkuName = "Standard",
                MeterName = "Standard vCPU Idle Usage",
                UnitOfMeasure = "1 Second",
                UnitPrice = 0.000003
            },
            new PriceRecord
            {
                SkuName = "Standard",
                MeterName = "Standard Memory Active Usage",
                UnitOfMeasure = "1 GiB Second",
                UnitPrice = 0.000003
            },
            new PriceRecord
            {
                SkuName = "Standard",
                MeterName = "Standard Memory Idle Usage",
                UnitOfMeasure = "1 GiB Second",
                UnitPrice = 0.000003
            },
            new PriceRecord
            {
                SkuName = "Standard",
                MeterName = "Standard Requests",
                UnitOfMeasure = "1M",
                UnitPrice = 0.4
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(11.826m, cost.Amount);
        Assert.Contains("idle min replica", cost.Details);
        Assert.Contains("active usage/requests", cost.Details);
    }

    [Fact]
    public void ContainerAppMapper_CalculateCost_ScaleToZeroHasNoBaselineCost()
    {
        var mapper = new ContainerAppMapper();
        var resource = CreateResource("Microsoft.App/containerApps",
            properties: new
            {
                template = new
                {
                    containers = new[] { new { name = "app", resources = new { cpu = 0.5, memory = "1Gi" } } },
                    scale = new { minReplicas = 0, maxReplicas = 5 }
                }
            });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                SkuName = "Standard",
                MeterName = "Standard vCPU Idle Usage",
                UnitOfMeasure = "1 Second",
                UnitPrice = 0.000003
            },
            new PriceRecord
            {
                SkuName = "Standard",
                MeterName = "Standard Memory Idle Usage",
                UnitOfMeasure = "1 GiB Second",
                UnitPrice = 0.000003
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(0m, cost.Amount);
        Assert.Contains("scale-to-zero", cost.Details);
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

    // ===== P4: Compute mappers =====

    [Fact]
    public void VirtualMachineScaleSetMapper_CanMap_CorrectType()
    {
        var mapper = new VirtualMachineScaleSetMapper();
        var resource = CreateResource("Microsoft.Compute/virtualMachineScaleSets",
            sku: new { name = "Standard_D2s_v3", capacity = 3 });

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void VirtualMachineScaleSetMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new VirtualMachineScaleSetMapper();
        var resource = CreateResource("Microsoft.Compute/virtualMachineScaleSets",
            sku: new { name = "Standard_D2s_v3", capacity = 3 });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Virtual Machines", queries[0].ServiceName);
        Assert.Equal("Standard_D2s_v3", queries[0].ArmSkuName);
    }

    [Fact]
    public void VirtualMachineScaleSetMapper_CalculateCost_MultipliesInstances()
    {
        var mapper = new VirtualMachineScaleSetMapper();
        var resource = CreateResource("Microsoft.Compute/virtualMachineScaleSets",
            sku: new { name = "Standard_D2s_v3", capacity = 3 });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                ArmSkuName = "Standard_D2s_v3",
                UnitPrice = 0.096,
                UnitOfMeasure = "1 Hour",
                MeterName = "D2s v3",
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        // 0.096 * 730 * 3 = 210.24
        Assert.Equal(210.24m, cost.Amount);
        Assert.Contains("× 3", cost.Details);
    }

    [Fact]
    public void BatchAccountMapper_CanMap_CorrectType()
    {
        var mapper = new BatchAccountMapper();
        var resource = CreateResource("Microsoft.Batch/batchAccounts");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void BatchAccountMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new BatchAccountMapper();
        var resource = CreateResource("Microsoft.Batch/batchAccounts");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Batch", queries[0].ServiceName);
    }

    [Fact]
    public void SpringAppMapper_CanMap_CorrectType()
    {
        var mapper = new SpringAppMapper();
        var resource = CreateResource("Microsoft.AppPlatform/Spring");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void SpringAppMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new SpringAppMapper();
        var resource = CreateResource("Microsoft.AppPlatform/Spring",
            sku: new { name = "Standard" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Spring Apps", queries[0].ServiceName);
    }

    // ===== P4: Networking mappers =====

    [Fact]
    public void VirtualNetworkMapper_CanMap_CorrectType()
    {
        var mapper = new VirtualNetworkMapper();
        var resource = CreateResource("Microsoft.Network/virtualNetworks");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void VirtualNetworkMapper_NoPeering_ReturnsFree()
    {
        var mapper = new VirtualNetworkMapper();
        var resource = CreateResource("Microsoft.Network/virtualNetworks");

        var cost = mapper.CalculateCost(resource, new List<PriceRecord>());

        Assert.Equal(0m, cost.Amount);
        Assert.Contains("free", cost.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NatGatewayMapper_CanMap_CorrectType()
    {
        var mapper = new NatGatewayMapper();
        var resource = CreateResource("Microsoft.Network/natGateways");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void NatGatewayMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new NatGatewayMapper();
        var resource = CreateResource("Microsoft.Network/natGateways");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Virtual Network", queries[0].ServiceName);
        Assert.Equal("NAT Gateway", queries[0].ProductName);
    }

    [Fact]
    public void PrivateDnsZoneMapper_CanMap_CorrectType()
    {
        var mapper = new PrivateDnsZoneMapper();
        var resource = CreateResource("Microsoft.Network/privateDnsZones");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void PrivateDnsZoneMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new PrivateDnsZoneMapper();
        var resource = CreateResource("Microsoft.Network/privateDnsZones");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure DNS", queries[0].ServiceName);
    }

    [Fact]
    public void TrafficManagerMapper_CanMap_CorrectType()
    {
        var mapper = new TrafficManagerMapper();
        var resource = CreateResource("Microsoft.Network/trafficManagerProfiles");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void TrafficManagerMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new TrafficManagerMapper();
        var resource = CreateResource("Microsoft.Network/trafficManagerProfiles");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Traffic Manager", queries[0].ServiceName);
    }

    [Fact]
    public void BastionHostMapper_CanMap_CorrectType()
    {
        var mapper = new BastionHostMapper();
        var resource = CreateResource("Microsoft.Network/bastionHosts");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void BastionHostMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new BastionHostMapper();
        var resource = CreateResource("Microsoft.Network/bastionHosts",
            sku: new { name = "Standard" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Bastion", queries[0].ServiceName);
    }

    [Fact]
    public void DdosProtectionPlanMapper_CanMap_CorrectType()
    {
        var mapper = new DdosProtectionPlanMapper();
        var resource = CreateResource("Microsoft.Network/ddosProtectionPlans");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void DdosProtectionPlanMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new DdosProtectionPlanMapper();
        var resource = CreateResource("Microsoft.Network/ddosProtectionPlans");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure DDoS Protection", queries[0].ServiceName);
    }

    [Fact]
    public void ExpressRouteCircuitMapper_CanMap_CorrectType()
    {
        var mapper = new ExpressRouteCircuitMapper();
        var resource = CreateResource("Microsoft.Network/expressRouteCircuits");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void ExpressRouteCircuitMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new ExpressRouteCircuitMapper();
        var resource = CreateResource("Microsoft.Network/expressRouteCircuits");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("ExpressRoute", queries[0].ServiceName);
    }

    // ===== P4: Database mappers =====

    [Fact]
    public void SqlElasticPoolMapper_CanMap_CorrectType()
    {
        var mapper = new SqlElasticPoolMapper();
        var resource = CreateResource("Microsoft.Sql/servers/elasticPools",
            sku: new { name = "StandardPool", capacity = 50 });

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void SqlElasticPoolMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new SqlElasticPoolMapper();
        var resource = CreateResource("Microsoft.Sql/servers/elasticPools",
            sku: new { name = "StandardPool", capacity = 50 });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("SQL Database", queries[0].ServiceName);
        Assert.Equal("StandardPool", queries[0].ArmSkuName);
    }

    [Fact]
    public void MariaDbServerMapper_CanMap_CorrectType()
    {
        var mapper = new MariaDbServerMapper();
        var resource = CreateResource("Microsoft.DBforMariaDB/servers",
            sku: new { name = "GP_Gen5_2" });

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void MariaDbServerMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new MariaDbServerMapper();
        var resource = CreateResource("Microsoft.DBforMariaDB/servers",
            sku: new { name = "GP_Gen5_2" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Database for MariaDB", queries[0].ServiceName);
        Assert.Equal("GP_Gen5_2", queries[0].ArmSkuName);
    }

    // ===== P4: AI / ML mappers =====

    [Fact]
    public void CognitiveServicesMapper_CanMap_CorrectType()
    {
        var mapper = new CognitiveServicesMapper();
        var resource = CreateResource("Microsoft.CognitiveServices/accounts",
            properties: new { _kind = "OpenAI" });

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void CognitiveServicesMapper_BuildQueries_OpenAI()
    {
        var mapper = new CognitiveServicesMapper();
        var resource = CreateResource("Microsoft.CognitiveServices/accounts",
            properties: new { _kind = "OpenAI" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure OpenAI Service", queries[0].ServiceName);
    }

    [Fact]
    public void CognitiveServicesMapper_BuildQueries_GenericCognitive()
    {
        var mapper = new CognitiveServicesMapper();
        var resource = CreateResource("Microsoft.CognitiveServices/accounts",
            properties: new { _kind = "SomeOtherService" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Cognitive Services", queries[0].ServiceName);
    }

    [Fact]
    public void MachineLearningWorkspaceMapper_CanMap_CorrectType()
    {
        var mapper = new MachineLearningWorkspaceMapper();
        var resource = CreateResource("Microsoft.MachineLearningServices/workspaces");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void MachineLearningWorkspaceMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new MachineLearningWorkspaceMapper();
        var resource = CreateResource("Microsoft.MachineLearningServices/workspaces");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Machine Learning", queries[0].ServiceName);
    }

    [Fact]
    public void SearchServiceMapper_CanMap_CorrectType()
    {
        var mapper = new SearchServiceMapper();
        var resource = CreateResource("Microsoft.Search/searchServices",
            sku: new { name = "standard" });

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void SearchServiceMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new SearchServiceMapper();
        var resource = CreateResource("Microsoft.Search/searchServices",
            sku: new { name = "standard" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Cognitive Search", queries[0].ServiceName);
        Assert.Equal("Standard S1", queries[0].SkuName);
    }

    [Fact]
    public void SearchServiceMapper_CalculateCost_UsesBasicUnitPrice()
    {
        var mapper = new SearchServiceMapper();
        var resource = CreateResource("Microsoft.Search/searchServices",
            properties: new { replicaCount = 1, partitionCount = 1 },
            sku: new { name = "basic" });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                SkuName = "Semantic Ranker",
                MeterName = "Semantic Ranker Unit",
                UnitOfMeasure = "1/Day",
                UnitPrice = 16.12
            },
            new PriceRecord
            {
                SkuName = "Basic",
                MeterName = "Basic Unit",
                UnitOfMeasure = "1 Hour",
                UnitPrice = 0.101
            },
            new PriceRecord
            {
                SkuName = "Basic CC",
                MeterName = "Basic CC Unit",
                UnitOfMeasure = "1 Hour",
                UnitPrice = 0.1111
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(73.73m, cost.Amount);
        Assert.Contains("AI Search basic", cost.Details);
        Assert.Contains("$0.1010/hr", cost.Details);
    }

    // ===== P4: Storage & Messaging mappers =====

    [Fact]
    public void EventGridMapper_CanMap_CorrectType()
    {
        var mapper = new EventGridMapper();
        var resource = CreateResource("Microsoft.EventGrid/topics");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void EventGridMapper_CanMap_Domains()
    {
        var mapper = new EventGridMapper();
        var resource = CreateResource("Microsoft.EventGrid/domains");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void EventGridMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new EventGridMapper();
        var resource = CreateResource("Microsoft.EventGrid/topics");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Event Grid", queries[0].ServiceName);
        Assert.Equal("Standard Operations", queries[0].MeterName);
    }

    [Fact]
    public void EventGridMapper_CalculateCost_UsesPaidTierAfterFreeAllowance()
    {
        var mapper = new EventGridMapper();
        var resource = CreateResource("Microsoft.EventGrid/topics");

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                MeterName = "Standard Operations",
                UnitOfMeasure = "100K",
                TierMinimumUnits = 0,
                UnitPrice = 0.0
            },
            new PriceRecord
            {
                MeterName = "Standard Operations",
                UnitOfMeasure = "100K",
                TierMinimumUnits = 1,
                UnitPrice = 0.06
            },
            new PriceRecord
            {
                MeterName = "Standard Event Operations",
                UnitOfMeasure = "1M",
                TierMinimumUnits = 1,
                UnitPrice = 0.6
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(0.54m, cost.Amount);
        Assert.Contains("900,000 billable", cost.Details);
        Assert.Contains("$0.0600/100K ops", cost.Details);
    }

    [Fact]
    public void NotificationHubMapper_CanMap_CorrectType()
    {
        var mapper = new NotificationHubMapper();
        var resource = CreateResource("Microsoft.NotificationHubs/namespaces");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void NotificationHubMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new NotificationHubMapper();
        var resource = CreateResource("Microsoft.NotificationHubs/namespaces",
            sku: new { name = "Basic" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Notification Hubs", queries[0].ServiceName);
    }

    // ===== P4: Container mappers =====

    [Fact]
    public void ContainerInstanceMapper_CanMap_CorrectType()
    {
        var mapper = new ContainerInstanceMapper();
        var resource = CreateResource("Microsoft.ContainerInstance/containerGroups");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void ContainerInstanceMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new ContainerInstanceMapper();
        var resource = CreateResource("Microsoft.ContainerInstance/containerGroups");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Container Instances", queries[0].ServiceName);
    }

    [Fact]
    public void ContainerAppsEnvironmentMapper_CanMap_CorrectType()
    {
        var mapper = new ContainerAppsEnvironmentMapper();
        var resource = CreateResource("Microsoft.App/managedEnvironments");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void ContainerAppsEnvironmentMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new ContainerAppsEnvironmentMapper();
        var resource = CreateResource("Microsoft.App/managedEnvironments");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Container Apps", queries[0].ServiceName);
    }

    [Fact]
    public void ContainerAppsEnvironmentMapper_Consumption_ReturnsFree()
    {
        var mapper = new ContainerAppsEnvironmentMapper();
        var resource = CreateResource("Microsoft.App/managedEnvironments");

        var cost = mapper.CalculateCost(resource, new List<PriceRecord>());

        Assert.Equal(0m, cost.Amount);
        Assert.Contains("Consumption", cost.Details);
    }

    // ===== P4: Monitoring & Management mappers =====

    [Fact]
    public void ApplicationInsightsMapper_CanMap_CorrectType()
    {
        var mapper = new ApplicationInsightsMapper();
        var resource = CreateResource("Microsoft.Insights/components");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void ApplicationInsightsMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new ApplicationInsightsMapper();
        var resource = CreateResource("Microsoft.Insights/components");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Application Insights", queries[0].ServiceName);
    }

    [Fact]
    public void AutomationAccountMapper_CanMap_CorrectType()
    {
        var mapper = new AutomationAccountMapper();
        var resource = CreateResource("Microsoft.Automation/automationAccounts");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void AutomationAccountMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new AutomationAccountMapper();
        var resource = CreateResource("Microsoft.Automation/automationAccounts");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Automation", queries[0].ServiceName);
    }

    // ===== P4: Integration mappers =====

    [Fact]
    public void LogicAppMapper_CanMap_CorrectType()
    {
        var mapper = new LogicAppMapper();
        var resource = CreateResource("Microsoft.Logic/workflows");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void LogicAppMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new LogicAppMapper();
        var resource = CreateResource("Microsoft.Logic/workflows");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Logic Apps", queries[0].ServiceName);
    }

    [Fact]
    public void DataFactoryMapper_CanMap_CorrectType()
    {
        var mapper = new DataFactoryMapper();
        var resource = CreateResource("Microsoft.DataFactory/factories");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void DataFactoryMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new DataFactoryMapper();
        var resource = CreateResource("Microsoft.DataFactory/factories");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Data Factory v2", queries[0].ServiceName);
    }

    // ===== P4: Analytics & Other mappers =====

    [Fact]
    public void DatabricksWorkspaceMapper_CanMap_CorrectType()
    {
        var mapper = new DatabricksWorkspaceMapper();
        var resource = CreateResource("Microsoft.Databricks/workspaces");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void DatabricksWorkspaceMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new DatabricksWorkspaceMapper();
        var resource = CreateResource("Microsoft.Databricks/workspaces",
            sku: new { name = "premium" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Databricks", queries[0].ServiceName);
    }

    [Fact]
    public void SynapseWorkspaceMapper_CanMap_CorrectType()
    {
        var mapper = new SynapseWorkspaceMapper();
        var resource = CreateResource("Microsoft.Synapse/workspaces");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void SynapseWorkspaceMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new SynapseWorkspaceMapper();
        var resource = CreateResource("Microsoft.Synapse/workspaces");

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("Azure Synapse Analytics", queries[0].ServiceName);
    }

    [Fact]
    public void IoTHubMapper_CanMap_CorrectType()
    {
        var mapper = new IoTHubMapper();
        var resource = CreateResource("Microsoft.Devices/IotHubs",
            sku: new { name = "S1", capacity = 1 });

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void IoTHubMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new IoTHubMapper();
        var resource = CreateResource("Microsoft.Devices/IotHubs",
            sku: new { name = "S1", capacity = 1 });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("IoT Hub", queries[0].ServiceName);
        Assert.Equal("S1", queries[0].SkuName);
    }

    [Fact]
    public void IoTHubMapper_FreeTier_ReturnsZero()
    {
        var mapper = new IoTHubMapper();
        var resource = CreateResource("Microsoft.Devices/IotHubs",
            sku: new { name = "F1", capacity = 1 });

        var cost = mapper.CalculateCost(resource, new List<PriceRecord>());

        Assert.Equal(0m, cost.Amount);
        Assert.Contains("Free", cost.Details);
    }

    [Fact]
    public void AppConfigurationMapper_CanMap_CorrectType()
    {
        var mapper = new AppConfigurationMapper();
        var resource = CreateResource("Microsoft.AppConfiguration/configurationStores");

        Assert.True(mapper.CanMap(resource));
    }

    [Fact]
    public void AppConfigurationMapper_BuildQueries_CorrectServiceName()
    {
        var mapper = new AppConfigurationMapper();
        var resource = CreateResource("Microsoft.AppConfiguration/configurationStores",
            sku: new { name = "standard" });

        var queries = mapper.BuildQueries(resource);

        Assert.Single(queries);
        Assert.Equal("App Configuration", queries[0].ServiceName);
        Assert.Equal("Standard", queries[0].SkuName);
    }

    [Fact]
    public void AppConfigurationMapper_FreeTier_ReturnsZero()
    {
        var mapper = new AppConfigurationMapper();
        var resource = CreateResource("Microsoft.AppConfiguration/configurationStores",
            sku: new { name = "free" });

        var cost = mapper.CalculateCost(resource, new List<PriceRecord>());

        Assert.Equal(0m, cost.Amount);
        Assert.Contains("Free", cost.Details);
    }

    [Fact]
    public void AppConfigurationMapper_CalculateCost_UsesStandardInstancePrice()
    {
        var mapper = new AppConfigurationMapper();
        var resource = CreateResource("Microsoft.AppConfiguration/configurationStores",
            sku: new { name = "Standard" });

        var prices = new List<PriceRecord>
        {
            new PriceRecord
            {
                SkuName = "Standard",
                MeterName = "Standard Experimentation Events",
                UnitOfMeasure = "1K",
                UnitPrice = 0.0
            },
            new PriceRecord
            {
                SkuName = "Standard",
                MeterName = "Standard Overage Operations",
                UnitOfMeasure = "10K",
                UnitPrice = 0.06
            },
            new PriceRecord
            {
                SkuName = "Standard",
                MeterName = "Standard Instance",
                UnitOfMeasure = "1/Day",
                UnitPrice = 1.2
            }
        };

        var cost = mapper.CalculateCost(resource, prices);

        Assert.Equal(36m, cost.Amount);
        Assert.Contains("$1.20/day", cost.Details);
    }

    // ===== Registry covers all new mappers =====

    [Fact]
    public void MapperRegistry_FindsAllNewMappers()
    {
        var registry = new MapperRegistry();

        // P4: Compute
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Compute/virtualMachineScaleSets")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Batch/batchAccounts")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.AppPlatform/Spring")));

        // P4: Networking
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Network/virtualNetworks")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Network/natGateways")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Network/privateDnsZones")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Network/trafficManagerProfiles")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Network/bastionHosts")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Network/ddosProtectionPlans")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Network/expressRouteCircuits")));

        // P4: Databases
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Sql/servers/elasticPools")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.DBforMariaDB/servers")));

        // P4: AI / ML
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.CognitiveServices/accounts")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.MachineLearningServices/workspaces")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Search/searchServices")));

        // P4: Storage & Messaging
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.EventGrid/topics")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.NotificationHubs/namespaces")));

        // P4: Containers
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.ContainerInstance/containerGroups")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.App/managedEnvironments")));

        // P4: Monitoring & Management
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Insights/components")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Automation/automationAccounts")));

        // P4: Integration
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Logic/workflows")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.DataFactory/factories")));

        // P4: Analytics & Other
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Databricks/workspaces")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Synapse/workspaces")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.Devices/IotHubs")));
        Assert.NotNull(registry.GetMapper(CreateResource("Microsoft.AppConfiguration/configurationStores")));
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
