---
title: Supported Resources
sidebar_position: 51
---

# Supported Resources

Washington currently ships with pricing mappers for <!-- GENERATED:RESOURCE_COUNT -->87<!-- /GENERATED:RESOURCE_COUNT --> Azure resource types. Support is implemented in the CLI mapper registry and shared by the CLI, VS Code extension, and GitHub Action.

## Generated Matrix

<!-- BEGIN GENERATED SUPPORTED RESOURCE MATRIX -->
This matrix is generated from `src/cli/Mappers/MapperRegistry.cs` and each mapper's `ResourceType` property.
The registry order is preserved so the table stays aligned with the implementation.

### Registry Summary

| Registry Group | Mappers |
| --- | ---: |
| P0: Core resource types | 4 |
| P1: High-impact resource types | 4 |
| P2: Additional resource types | 3 |
| P3: Extended resource types | 17 |
| P4: Compute | 3 |
| P4: Networking | 8 |
| P4: Databases | 2 |
| P4: AI / ML | 3 |
| P4: Storage & Messaging | 2 |
| P4: Containers | 2 |
| P4: Monitoring & Management | 2 |
| P4: Integration | 2 |
| P4: Analytics & Other | 4 |
| P5: Networking (extended) | 5 |
| P5: Security | 3 |
| P5: AI / ML | 1 |
| P5: Analytics | 4 |
| P5: Storage | 1 |
| P5: Databases | 2 |
| P5: Developer | 3 |
| P5: Integration | 3 |
| P5: Media & Maps | 2 |
| P5: IoT | 1 |
| P5: Governance | 2 |
| P5: Virtual Desktop | 1 |
| P5: Service Fabric | 1 |
| P5: Monitoring | 2 |

### Coverage Matrix

| Registry Group | ARM Resource Type | Mapper |
| --- | --- | --- |
| P0: Core resource types | `Microsoft.Compute/virtualMachines` | `VirtualMachineMapper` |
| P0: Core resource types | `Microsoft.Storage/storageAccounts` | `StorageAccountMapper` |
| P0: Core resource types | `Microsoft.Sql/servers/databases` | `SqlDatabaseMapper` |
| P0: Core resource types | `Microsoft.Web/serverfarms` | `AppServicePlanMapper` |
| P1: High-impact resource types | `Microsoft.ContainerService/managedClusters` | `ManagedClusterMapper` |
| P1: High-impact resource types | `Microsoft.Network/publicIPAddresses` | `PublicIpAddressMapper` |
| P1: High-impact resource types | `Microsoft.Network/applicationGateways` | `ApplicationGatewayMapper` |
| P1: High-impact resource types | `Microsoft.DocumentDB/databaseAccounts` | `CosmosDbAccountMapper` |
| P2: Additional resource types | `Microsoft.KeyVault/vaults` | `KeyVaultMapper` |
| P2: Additional resource types | `Microsoft.ContainerRegistry/registries` | `ContainerRegistryMapper` |
| P2: Additional resource types | `Microsoft.Network/loadBalancers` | `LoadBalancerMapper` |
| P3: Extended resource types | `Microsoft.Compute/disks` | `ManagedDiskMapper` |
| P3: Extended resource types | `Microsoft.Web/sites` | `FunctionAppMapper` |
| P3: Extended resource types | `Microsoft.Sql/managedInstances` | `SqlManagedInstanceMapper` |
| P3: Extended resource types | `Microsoft.Network/virtualNetworkGateways` | `VirtualNetworkGatewayMapper` |
| P3: Extended resource types | `Microsoft.Network/azureFirewalls` | `AzureFirewallMapper` |
| P3: Extended resource types | `Microsoft.Network/privateEndpoints` | `PrivateEndpointMapper` |
| P3: Extended resource types | `Microsoft.OperationalInsights/workspaces` | `LogAnalyticsWorkspaceMapper` |
| P3: Extended resource types | `Microsoft.Cache/redis` | `RedisCacheMapper` |
| P3: Extended resource types | `Microsoft.EventHub/namespaces` | `EventHubMapper` |
| P3: Extended resource types | `Microsoft.ServiceBus/namespaces` | `ServiceBusMapper` |
| P3: Extended resource types | `Microsoft.Cdn/profiles` | `FrontDoorMapper` |
| P3: Extended resource types | `Microsoft.App/containerApps` | `ContainerAppMapper` |
| P3: Extended resource types | `Microsoft.DBforPostgreSQL/flexibleServers` | `PostgreSqlFlexibleServerMapper` |
| P3: Extended resource types | `Microsoft.DBforMySQL/flexibleServers` | `MySqlFlexibleServerMapper` |
| P3: Extended resource types | `Microsoft.ApiManagement/service` | `ApiManagementMapper` |
| P3: Extended resource types | `Microsoft.Web/staticSites` | `StaticWebAppMapper` |
| P3: Extended resource types | `Microsoft.SignalRService/signalR` | `SignalRMapper` |
| P4: Compute | `Microsoft.Compute/virtualMachineScaleSets` | `VirtualMachineScaleSetMapper` |
| P4: Compute | `Microsoft.Batch/batchAccounts` | `BatchAccountMapper` |
| P4: Compute | `Microsoft.AppPlatform/Spring` | `SpringAppMapper` |
| P4: Networking | `Microsoft.Network/virtualNetworks` | `VirtualNetworkMapper` |
| P4: Networking | `Microsoft.Network/natGateways` | `NatGatewayMapper` |
| P4: Networking | `Microsoft.Network/networkInterfaces` | `NetworkInterfaceMapper` |
| P4: Networking | `Microsoft.Network/privateDnsZones` | `PrivateDnsZoneMapper` |
| P4: Networking | `Microsoft.Network/trafficManagerProfiles` | `TrafficManagerMapper` |
| P4: Networking | `Microsoft.Network/bastionHosts` | `BastionHostMapper` |
| P4: Networking | `Microsoft.Network/ddosProtectionPlans` | `DdosProtectionPlanMapper` |
| P4: Networking | `Microsoft.Network/expressRouteCircuits` | `ExpressRouteCircuitMapper` |
| P4: Databases | `Microsoft.Sql/servers/elasticPools` | `SqlElasticPoolMapper` |
| P4: Databases | `Microsoft.DBforMariaDB/servers` | `MariaDbServerMapper` |
| P4: AI / ML | `Microsoft.CognitiveServices/accounts` | `CognitiveServicesMapper` |
| P4: AI / ML | `Microsoft.MachineLearningServices/workspaces` | `MachineLearningWorkspaceMapper` |
| P4: AI / ML | `Microsoft.Search/searchServices` | `SearchServiceMapper` |
| P4: Storage & Messaging | `Microsoft.EventGrid/topics` | `EventGridMapper` |
| P4: Storage & Messaging | `Microsoft.NotificationHubs/namespaces` | `NotificationHubMapper` |
| P4: Containers | `Microsoft.ContainerInstance/containerGroups` | `ContainerInstanceMapper` |
| P4: Containers | `Microsoft.App/managedEnvironments` | `ContainerAppsEnvironmentMapper` |
| P4: Monitoring & Management | `Microsoft.Insights/components` | `ApplicationInsightsMapper` |
| P4: Monitoring & Management | `Microsoft.Automation/automationAccounts` | `AutomationAccountMapper` |
| P4: Integration | `Microsoft.Logic/workflows` | `LogicAppMapper` |
| P4: Integration | `Microsoft.DataFactory/factories` | `DataFactoryMapper` |
| P4: Analytics & Other | `Microsoft.Databricks/workspaces` | `DatabricksWorkspaceMapper` |
| P4: Analytics & Other | `Microsoft.Synapse/workspaces` | `SynapseWorkspaceMapper` |
| P4: Analytics & Other | `Microsoft.Devices/IotHubs` | `IoTHubMapper` |
| P4: Analytics & Other | `Microsoft.AppConfiguration/configurationStores` | `AppConfigurationMapper` |
| P5: Networking (extended) | `Microsoft.Network/dnsZones` | `DnsZoneMapper` |
| P5: Networking (extended) | `Microsoft.Network/networkSecurityGroups` | `NetworkSecurityGroupMapper` |
| P5: Networking (extended) | `Microsoft.Network/routeTables` | `RouteTableMapper` |
| P5: Networking (extended) | `Microsoft.Network/networkWatchers` | `NetworkWatcherMapper` |
| P5: Networking (extended) | `Microsoft.Network/firewallPolicies` | `FirewallPolicyMapper` |
| P5: Security | `Microsoft.ManagedIdentity/userAssignedIdentities` | `ManagedIdentityMapper` |
| P5: Security | `Microsoft.RecoveryServices/vaults` | `RecoveryServicesVaultMapper` |
| P5: Security | `Microsoft.Security/pricings` | `DefenderForCloudMapper` |
| P5: AI / ML | `Microsoft.BotService/botServices` | `BotServiceMapper` |
| P5: Analytics | `Microsoft.Kusto/clusters` | `KustoClusterMapper` |
| P5: Analytics | `Microsoft.StreamAnalytics/streamingjobs` | `StreamAnalyticsMapper` |
| P5: Analytics | `Microsoft.HDInsight/clusters` | `HdInsightClusterMapper` |
| P5: Analytics | `Microsoft.PowerBIDedicated/capacities` | `PowerBIEmbeddedMapper` |
| P5: Storage | `Microsoft.NetApp/netAppAccounts/capacityPools` | `NetAppFilesMapper` |
| P5: Databases | `Microsoft.Cache/redisEnterprise` | `RedisEnterpriseMapper` |
| P5: Databases | `Microsoft.DocumentDB/mongoClusters` | `CosmosDbMongoClusterMapper` |
| P5: Developer | `Microsoft.DevCenter/devcenters` | `DevCenterMapper` |
| P5: Developer | `Microsoft.LoadTestService/loadTests` | `LoadTestingMapper` |
| P5: Developer | `Microsoft.DevTestLab/labs` | `DevTestLabMapper` |
| P5: Integration | `Microsoft.Relay/namespaces` | `RelayMapper` |
| P5: Integration | `Microsoft.HealthcareApis/services` | `HealthcareApisMapper` |
| P5: Integration | `Microsoft.Communication/communicationServices` | `CommunicationServicesMapper` |
| P5: Media & Maps | `Microsoft.Media/mediaservices` | `MediaServicesMapper` |
| P5: Media & Maps | `Microsoft.Maps/accounts` | `MapsAccountMapper` |
| P5: IoT | `Microsoft.DigitalTwins/digitalTwinsInstances` | `DigitalTwinsMapper` |
| P5: Governance | `Microsoft.Purview/accounts` | `PurviewAccountMapper` |
| P5: Governance | `Microsoft.ConfidentialLedger/ledgers` | `ConfidentialLedgerMapper` |
| P5: Virtual Desktop | `Microsoft.DesktopVirtualization/hostPools` | `VirtualDesktopHostPoolMapper` |
| P5: Service Fabric | `Microsoft.ServiceFabric/clusters` | `ServiceFabricClusterMapper` |
| P5: Monitoring | `Microsoft.Monitor/accounts` | `MonitorWorkspaceMapper` |
| P5: Monitoring | `Microsoft.Dashboard/grafana` | `ManagedGrafanaMapper` |
<!-- END GENERATED SUPPORTED RESOURCE MATRIX -->

## What Supported Means

Supported does not mean that every billing nuance for a service is modeled.

In practice, support means:

- Washington can recognize the resource type.
- Washington can derive one or more pricing queries from the resource's SKU or properties.
- Washington can produce a monthly cost line item from the returned price records.

Some services have a simple one-to-one mapping. Others are approximated from the most relevant recurring meter for the chosen SKU.

## Unsupported Resources

When a resource type does not have a mapper yet, Washington does not fail the whole estimate. It emits a warning like this instead:

```text
⚠ No pricing mapper for Microsoft.Xyz/abc - skipped
```

That behavior is useful in mixed templates, but it also means your total can be incomplete if some resource types are not yet mapped.

## Pricing Assumptions

The current mapper set uses pay-as-you-go retail pricing by default.

- Spot and low-priority pricing are excluded from the default queries.
- Reserved Instance, Savings Plan, and contract-specific pricing are not modeled yet.
- If a region cannot be resolved from the template, the estimator falls back to `eastus`.

## Choosing Good Validation Templates

If you want to validate mapper coverage quickly, start with templates that contain:

- one resource type per file
- explicit `location` and `sku` values
- minimal ARM expression indirection around SKU and sizing properties

That makes it easier to confirm whether a cost line is missing because the resource is unsupported or because an expression could not be resolved.

## Related Reading

- [How Estimates Work](/guides/how-estimates-work)
- [Troubleshooting](/guides/troubleshooting)
