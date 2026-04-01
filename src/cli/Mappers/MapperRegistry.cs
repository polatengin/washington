using Washington.Models;

namespace Washington.Mappers;

public class MapperRegistry
{
    private readonly List<IResourceCostMapper> _mappers = new();

    public MapperRegistry()
    {
        // P0: Core resource types
        Register(new VirtualMachineMapper());
        Register(new StorageAccountMapper());
        Register(new SqlDatabaseMapper());
        Register(new AppServicePlanMapper());

        // P1: High-impact resource types
        Register(new ManagedClusterMapper());
        Register(new PublicIpAddressMapper());
        Register(new ApplicationGatewayMapper());
        Register(new CosmosDbAccountMapper());

        // P2: Additional resource types
        Register(new KeyVaultMapper());
        Register(new ContainerRegistryMapper());
        Register(new LoadBalancerMapper());

        // P3: Extended resource types
        Register(new ManagedDiskMapper());
        Register(new FunctionAppMapper());
        Register(new SqlManagedInstanceMapper());
        Register(new VirtualNetworkGatewayMapper());
        Register(new AzureFirewallMapper());
        Register(new PrivateEndpointMapper());
        Register(new LogAnalyticsWorkspaceMapper());
        Register(new RedisCacheMapper());
        Register(new EventHubMapper());
        Register(new ServiceBusMapper());
        Register(new FrontDoorMapper());
        Register(new ContainerAppMapper());
        Register(new PostgreSqlFlexibleServerMapper());
        Register(new MySqlFlexibleServerMapper());
        Register(new ApiManagementMapper());
        Register(new StaticWebAppMapper());
        Register(new SignalRMapper());
    }

    public void Register(IResourceCostMapper mapper) => _mappers.Add(mapper);

    public IResourceCostMapper? GetMapper(ResourceDescriptor resource) =>
        _mappers.FirstOrDefault(m => m.CanMap(resource));

    public IReadOnlyList<IResourceCostMapper> All => _mappers.AsReadOnly();
}
