export type PlaygroundFixture = {
  id: string;
  name: string;
  source: string;
};

export const playgroundFixtures: PlaygroundFixture[] = [
  {
    "id": "aks-vm.bicep",
    "name": "aks-vm",
    "source": "// This is the virtual machine that you're building.\nresource vm 'Microsoft.Compute/virtualMachines@2020-06-01' = {\n  name: 'SampleVM'\n  location: resourceGroup().location\n  properties: {\n    osProfile: {\n      computerName: 'SampleVM'\n      adminUsername: 'adminUsername'\n      adminPassword: 'adminPassword'\n      windowsConfiguration: {\n        provisionVMAgent: true\n      }\n    }\n    hardwareProfile: {\n      vmSize: 'Standard_DS1_v2'\n    }\n    storageProfile: {\n      imageReference: {\n        publisher: 'MicrosoftWindowsServer'\n        offer: 'WindowsServer'\n        sku: '2019-Datacenter'\n        version: 'latest'\n      }\n      osDisk: {\n        createOption: 'FromImage'\n      }\n    }\n  }\n}\n\n// mandatory params\nparam dnsPrefix string\nparam linuxAdminUsername string\nparam sshRSAPublicKey string\nparam servicePrincipalClientId string\n\n@secure()\nparam servicePrincipalClientSecret string\n\n// optional params\nparam clusterName string = 'aks101cluster'\nparam location string = resourceGroup().location\n\n@minValue(0)\n@maxValue(1023)\nparam osDiskSizeGB int = 0\n\n@minValue(1)\n@maxValue(50)\nparam agentCount int = 3\n\nparam agentVMSize string = 'Standard_DS2_v2'\n// osType was a defaultValue with only one allowedValue, which seems strange?, could be a good TTK test\n\nresource aks 'Microsoft.ContainerService/managedClusters@2020-09-01' = {\n  name: clusterName\n  location: location\n  properties: {\n    dnsPrefix: dnsPrefix\n    agentPoolProfiles: [\n      {\n        name: 'agentpool'\n        osDiskSizeGB: osDiskSizeGB\n        count: agentCount\n        vmSize: agentVMSize\n        osType: 'Linux'\n        mode: 'System'\n      }\n    ]\n    linuxProfile: {\n      adminUsername: linuxAdminUsername\n      ssh: {\n        publicKeys: [\n          {\n            keyData: sshRSAPublicKey\n          }\n        ]\n      }\n    }\n    servicePrincipalProfile: {\n      clientId: servicePrincipalClientId\n      secret: servicePrincipalClientSecret\n    }\n  }\n}\n\noutput controlPlaneFQDN string = aks.properties.fqdn\n"
  },
  {
    "id": "aks.bicep",
    "name": "aks",
    "source": "// mandatory params\nparam dnsPrefix string\nparam linuxAdminUsername string\nparam sshRSAPublicKey string\nparam servicePrincipalClientId string\n\n@secure()\nparam servicePrincipalClientSecret string\n\n// optional params\nparam clusterName string = 'aks101cluster'\nparam location string = resourceGroup().location\n\n@minValue(0)\n@maxValue(1023)\nparam osDiskSizeGB int = 0\n\n@minValue(1)\n@maxValue(50)\nparam agentCount int = 3\n\nparam agentVMSize string = 'Standard_DS2_v2'\n// osType was a defaultValue with only one allowedValue, which seems strange?, could be a good TTK test\n\nresource aks 'Microsoft.ContainerService/managedClusters@2020-09-01' = {\n  name: clusterName\n  location: location\n  properties: {\n    dnsPrefix: dnsPrefix\n    agentPoolProfiles: [\n      {\n        name: 'agentpool'\n        osDiskSizeGB: osDiskSizeGB\n        count: agentCount\n        vmSize: agentVMSize\n        osType: 'Linux'\n        mode: 'System'\n      }\n    ]\n    linuxProfile: {\n      adminUsername: linuxAdminUsername\n      ssh: {\n        publicKeys: [\n          {\n            keyData: sshRSAPublicKey\n          }\n        ]\n      }\n    }\n    servicePrincipalProfile: {\n      clientId: servicePrincipalClientId\n      secret: servicePrincipalClientSecret\n    }\n  }\n}\n\noutput controlPlaneFQDN string = aks.properties.fqdn\n"
  },
  {
    "id": "all.bicep",
    "name": "all",
    "source": "resource vm 'Microsoft.Compute/virtualMachines@2020-06-01' = {\n  name: 'SampleVM'\n  location: location\n  properties: {\n    osProfile: {\n      computerName: 'SampleVM'\n      adminUsername: 'adminUsername'\n      adminPassword: 'adminPassword'\n      windowsConfiguration: {\n        provisionVMAgent: true\n      }\n    }\n    hardwareProfile: {\n      vmSize: 'Standard_DS1_v2'\n    }\n    storageProfile: {\n      imageReference: {\n        publisher: 'MicrosoftWindowsServer'\n        offer: 'WindowsServer'\n        sku: '2019-Datacenter'\n        version: 'latest'\n      }\n      osDisk: {\n        createOption: 'FromImage'\n      }\n    }\n  }\n}\n\nparam dnsPrefix string\nparam linuxAdminUsername string\nparam sshRSAPublicKey string\nparam servicePrincipalClientId string\n\n@secure()\nparam servicePrincipalClientSecret string\n\nparam clusterName string = 'aks101cluster'\nparam location string = resourceGroup().location\n\n@minValue(0)\n@maxValue(1023)\nparam osDiskSizeGB int = 0\n\n@minValue(1)\n@maxValue(50)\nparam agentCount int = 3\n\nparam agentVMSize string = 'Standard_DS2_v2'\n\nresource aks 'Microsoft.ContainerService/managedClusters@2020-09-01' = {\n  name: clusterName\n  location: location\n  properties: {\n    dnsPrefix: dnsPrefix\n    agentPoolProfiles: [\n      {\n        name: 'agentpool'\n        osDiskSizeGB: osDiskSizeGB\n        count: agentCount\n        vmSize: agentVMSize\n        osType: 'Linux'\n        mode: 'System'\n      }\n    ]\n    linuxProfile: {\n      adminUsername: linuxAdminUsername\n      ssh: {\n        publicKeys: [\n          {\n            keyData: sshRSAPublicKey\n          }\n        ]\n      }\n    }\n    servicePrincipalProfile: {\n      clientId: servicePrincipalClientId\n      secret: servicePrincipalClientSecret\n    }\n  }\n}\n\noutput controlPlaneFQDN string = aks.properties.fqdn\n\nresource storage 'Microsoft.Storage/storageAccounts@2022-09-01' = {\n  name: 'name'\n  location: location\n  kind: 'StorageV2'\n  sku: {\n    name: 'Standard_LRS'\n  }\n}\n\nresource blobService 'Microsoft.Storage/storageAccounts/blobServices@2021-04-01' = {\n  parent: storage\n  name: 'default'\n}\n\nresource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-04-01' = {\n  parent: blobService\n  name: 'azbenchpresscontainer'\n}\n"
  },
  {
    "id": "app-insights.bicep",
    "name": "app-insights",
    "source": "param location string = 'eastus'\n\nresource appInsights 'Microsoft.Insights/components@2018-05-01-preview' = {\n  name: 'washington-demo-ai'\n  location: location\n  kind: 'web'\n  properties: {\n    Application_Type: 'web'\n  }\n}"
  },
  {
    "id": "app-service-plan.bicep",
    "name": "app-service-plan",
    "source": "param location string = 'eastus'\n\nresource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {\n  name: 'washington-demo-plan'\n  location: location\n  sku: {\n    name: 'B1'\n    tier: 'Basic'\n  }\n}"
  },
  {
    "id": "container-registry.bicep",
    "name": "container-registry",
    "source": "param location string = 'eastus'\n\nresource containerRegistry 'Microsoft.ContainerRegistry/registries@2019-12-01-preview' = {\n  name: 'washingtondemoregistry01'\n  location: location\n  sku: {\n    name: 'Basic'\n  }\n  properties: {\n    adminUserEnabled: false\n  }\n}"
  },
  {
    "id": "cosmos-db-account.bicep",
    "name": "cosmos-db-account",
    "source": "param location string = 'eastus'\n\nresource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2021-04-15' = {\n  name: 'washingtondemoacct01'\n  location: location\n  kind: 'GlobalDocumentDB'\n  properties: {\n    databaseAccountOfferType: 'Standard'\n    enableFreeTier: false\n    consistencyPolicy: {\n      defaultConsistencyLevel: 'Session'\n    }\n    locations: [\n      {\n        locationName: location\n        failoverPriority: 0\n        isZoneRedundant: false\n      }\n    ]\n  }\n}"
  },
  {
    "id": "event-hub-namespace.bicep",
    "name": "event-hub-namespace",
    "source": "param location string = 'eastus'\n\nresource eventHubNamespace 'Microsoft.EventHub/namespaces@2018-01-01-preview' = {\n  name: 'washingtondemoeh'\n  location: location\n  sku: {\n    name: 'Standard'\n    tier: 'Standard'\n    capacity: 1\n  }\n  properties: {\n    isAutoInflateEnabled: false\n    maximumThroughputUnits: 0\n  }\n}"
  },
  {
    "id": "key-vault.bicep",
    "name": "key-vault",
    "source": "param location string = 'eastus'\n\nresource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {\n  name: 'washington-demo-kv'\n  location: location\n  properties: {\n    enabledForDeployment: false\n    enabledForTemplateDeployment: false\n    enabledForDiskEncryption: false\n    tenantId: subscription().tenantId\n    accessPolicies: []\n    sku: {\n      name: 'standard'\n      family: 'A'\n    }\n    networkAcls: {\n      defaultAction: 'Allow'\n      bypass: 'AzureServices'\n    }\n  }\n}"
  },
  {
    "id": "log-analytics-workspace.bicep",
    "name": "log-analytics-workspace",
    "source": "param location string = 'eastus'\n\nresource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2020-10-01' = {\n  name: 'washington-demo-logs'\n  location: location\n  properties: {\n    sku: {\n      name: 'PerGB2018'\n    }\n    retentionInDays: 30\n  }\n}"
  },
  {
    "id": "public-ip.bicep",
    "name": "public-ip",
    "source": "param location string = 'eastus'\n\nresource publicIp 'Microsoft.Network/publicIPAddresses@2020-06-01' = {\n  name: 'washington-demo-pip'\n  location: location\n  sku: {\n    name: 'Standard'\n  }\n  properties: {\n    publicIPAddressVersion: 'IPv4'\n    publicIPAllocationMethod: 'Static'\n    idleTimeoutInMinutes: 4\n  }\n}"
  },
  {
    "id": "redis-cache.bicep",
    "name": "redis-cache",
    "source": "param location string = 'eastus'\n\nresource redisCache 'Microsoft.Cache/redis@2019-07-01' = {\n  name: 'washington-demo-redis'\n  location: location\n  properties: {\n    enableNonSslPort: false\n    minimumTlsVersion: '1.2'\n    sku: {\n      name: 'Basic'\n      family: 'C'\n      capacity: 1\n    }\n  }\n}"
  },
  {
    "id": "service-bus-namespace.bicep",
    "name": "service-bus-namespace",
    "source": "param location string = 'eastus'\n\nresource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2017-04-01' = {\n  name: 'washington-demo-sbus'\n  location: location\n  sku: {\n    name: 'Standard'\n    tier: 'Standard'\n    capacity: 1\n  }\n  properties: {}\n}"
  },
  {
    "id": "showcase.bicep",
    "name": "showcase",
    "source": "param location string = 'eastus'\n\nresource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {\n  name: 'washington-showcase-plan'\n  location: location\n  sku: {\n    name: 'B1'\n    tier: 'Basic'\n  }\n}\n\nresource appInsights 'Microsoft.Insights/components@2018-05-01-preview' = {\n  name: 'washington-showcase-ai'\n  location: location\n  kind: 'web'\n  properties: {\n    Application_Type: 'web'\n  }\n}\n\nresource containerRegistry 'Microsoft.ContainerRegistry/registries@2019-12-01-preview' = {\n  name: 'washingtonshowcasereg01'\n  location: location\n  sku: {\n    name: 'Basic'\n  }\n  properties: {\n    adminUserEnabled: false\n  }\n}\n\nresource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2021-04-15' = {\n  name: 'washingtonshowcaseacct'\n  location: location\n  kind: 'GlobalDocumentDB'\n  properties: {\n    databaseAccountOfferType: 'Standard'\n    enableFreeTier: false\n    consistencyPolicy: {\n      defaultConsistencyLevel: 'Session'\n    }\n    locations: [\n      {\n        locationName: location\n        failoverPriority: 0\n        isZoneRedundant: false\n      }\n    ]\n  }\n}\n\nresource eventHubNamespace 'Microsoft.EventHub/namespaces@2018-01-01-preview' = {\n  name: 'washingtonshowcaseeh'\n  location: location\n  sku: {\n    name: 'Standard'\n    tier: 'Standard'\n    capacity: 1\n  }\n  properties: {\n    isAutoInflateEnabled: false\n    maximumThroughputUnits: 0\n  }\n}\n\nresource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {\n  name: 'washington-showcase-kv'\n  location: location\n  properties: {\n    enabledForDeployment: false\n    enabledForTemplateDeployment: false\n    enabledForDiskEncryption: false\n    tenantId: subscription().tenantId\n    accessPolicies: []\n    sku: {\n      name: 'standard'\n      family: 'A'\n    }\n    networkAcls: {\n      defaultAction: 'Allow'\n      bypass: 'AzureServices'\n    }\n  }\n}\n\nresource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2020-10-01' = {\n  name: 'washington-showcase-logs'\n  location: location\n  properties: {\n    sku: {\n      name: 'PerGB2018'\n    }\n    retentionInDays: 30\n  }\n}\n\nresource publicIp 'Microsoft.Network/publicIPAddresses@2020-06-01' = {\n  name: 'washington-showcase-pip'\n  location: location\n  sku: {\n    name: 'Standard'\n  }\n  properties: {\n    publicIPAddressVersion: 'IPv4'\n    publicIPAllocationMethod: 'Static'\n    idleTimeoutInMinutes: 4\n  }\n}\n\nresource redisCache 'Microsoft.Cache/redis@2019-07-01' = {\n  name: 'washington-showcase-redis'\n  location: location\n  properties: {\n    enableNonSslPort: false\n    minimumTlsVersion: '1.2'\n    sku: {\n      name: 'Basic'\n      family: 'C'\n      capacity: 1\n    }\n  }\n}\n\nresource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2017-04-01' = {\n  name: 'washington-showcase-sbus'\n  location: location\n  sku: {\n    name: 'Standard'\n    tier: 'Standard'\n    capacity: 1\n  }\n  properties: {}\n}"
  },
  {
    "id": "simple-vm.bicep",
    "name": "simple-vm",
    "source": "// This is a simple VM for testing\nresource vm 'Microsoft.Compute/virtualMachines@2023-09-01' = {\n  name: 'test-vm'\n  location: 'eastus'\n  properties: {\n    hardwareProfile: {\n      vmSize: 'Standard_D2s_v3'\n    }\n    osProfile: {\n      computerName: 'test-vm'\n      adminUsername: 'azureuser'\n      adminPassword: 'P@ssw0rd1234!'\n    }\n    storageProfile: {\n      imageReference: {\n        publisher: 'Canonical'\n        offer: '0001-com-ubuntu-server-jammy'\n        sku: '22_04-lts'\n        version: 'latest'\n      }\n      osDisk: {\n        createOption: 'FromImage'\n      }\n    }\n  }\n}\n"
  },
  {
    "id": "vm.bicep",
    "name": "vm",
    "source": "// This is the virtual machine that you're building.\nresource vm 'Microsoft.Compute/virtualMachines@2020-06-01' = {\n  name: 'SampleVM'\n  location: resourceGroup().location\n  properties: {\n    osProfile: {\n      computerName: 'SampleVM'\n      adminUsername: 'adminUsername'\n      adminPassword: 'adminPassword'\n      windowsConfiguration: {\n        provisionVMAgent: true\n      }\n    }\n    hardwareProfile: {\n      vmSize: 'Standard_DS1_v2'\n    }\n    storageProfile: {\n      imageReference: {\n        publisher: 'MicrosoftWindowsServer'\n        offer: 'WindowsServer'\n        sku: '2019-Datacenter'\n        version: 'latest'\n      }\n      osDisk: {\n        createOption: 'FromImage'\n      }\n    }\n  }\n}\n"
  }
];
