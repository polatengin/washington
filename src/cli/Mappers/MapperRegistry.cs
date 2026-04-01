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

        // P4: Compute
        Register(new VirtualMachineScaleSetMapper());
        Register(new BatchAccountMapper());
        Register(new SpringAppMapper());

        // P4: Networking
        Register(new VirtualNetworkMapper());
        Register(new NatGatewayMapper());
        Register(new NetworkInterfaceMapper());
        Register(new PrivateDnsZoneMapper());
        Register(new TrafficManagerMapper());
        Register(new BastionHostMapper());
        Register(new DdosProtectionPlanMapper());
        Register(new ExpressRouteCircuitMapper());

        // P4: Databases
        Register(new SqlElasticPoolMapper());
        Register(new MariaDbServerMapper());

        // P4: AI / ML
        Register(new CognitiveServicesMapper());
        Register(new MachineLearningWorkspaceMapper());
        Register(new SearchServiceMapper());

        // P4: Storage & Messaging
        Register(new EventGridMapper());
        Register(new NotificationHubMapper());

        // P4: Containers
        Register(new ContainerInstanceMapper());
        Register(new ContainerAppsEnvironmentMapper());

        // P4: Monitoring & Management
        Register(new ApplicationInsightsMapper());
        Register(new AutomationAccountMapper());

        // P4: Integration
        Register(new LogicAppMapper());
        Register(new DataFactoryMapper());

        // P4: Analytics & Other
        Register(new DatabricksWorkspaceMapper());
        Register(new SynapseWorkspaceMapper());
        Register(new IoTHubMapper());
        Register(new AppConfigurationMapper());

        // P5: Networking (extended)
        Register(new DnsZoneMapper());
        Register(new NetworkSecurityGroupMapper());
        Register(new RouteTableMapper());
        Register(new NetworkWatcherMapper());
        Register(new FirewallPolicyMapper());

        // P5: Security
        Register(new ManagedIdentityMapper());
        Register(new RecoveryServicesVaultMapper());
        Register(new DefenderForCloudMapper());

        // P5: AI / ML
        Register(new BotServiceMapper());

        // P5: Analytics
        Register(new KustoClusterMapper());
        Register(new StreamAnalyticsMapper());
        Register(new HdInsightClusterMapper());
        Register(new PowerBIEmbeddedMapper());

        // P5: Storage
        Register(new NetAppFilesMapper());

        // P5: Databases
        Register(new RedisEnterpriseMapper());
        Register(new CosmosDbMongoClusterMapper());

        // P5: Developer
        Register(new DevCenterMapper());
        Register(new LoadTestingMapper());
        Register(new DevTestLabMapper());

        // P5: Integration
        Register(new RelayMapper());
        Register(new HealthcareApisMapper());
        Register(new CommunicationServicesMapper());

        // P5: Media & Maps
        Register(new MediaServicesMapper());
        Register(new MapsAccountMapper());

        // P5: IoT
        Register(new DigitalTwinsMapper());

        // P5: Governance
        Register(new PurviewAccountMapper());
        Register(new ConfidentialLedgerMapper());

        // P5: Virtual Desktop
        Register(new VirtualDesktopHostPoolMapper());

        // P5: Service Fabric
        Register(new ServiceFabricClusterMapper());

        // P5: Monitoring
        Register(new MonitorWorkspaceMapper());
        Register(new ManagedGrafanaMapper());
    }

    public void Register(IResourceCostMapper mapper) => _mappers.Add(mapper);

    public IResourceCostMapper? GetMapper(ResourceDescriptor resource) =>
        _mappers.FirstOrDefault(m => m.CanMap(resource));

    public IReadOnlyList<IResourceCostMapper> All => _mappers.AsReadOnly();
}
