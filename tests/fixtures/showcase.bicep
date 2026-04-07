param dnsPrefix string
param linuxAdminUsername string
param sshRSAPublicKey string
param servicePrincipalClientId string

@secure()
param servicePrincipalClientSecret string

param clusterName string = 'aks101cluster'
param location string = resourceGroup().location

@minValue(0)
@maxValue(1023)
param osDiskSizeGB int = 0

@minValue(1)
@maxValue(50)
param agentCount int = 3

param agentVMSize string = 'Standard_DS2_v2'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'washington-showcase-plan'
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
}

resource appInsights 'Microsoft.Insights/components@2018-05-01-preview' = {
  name: 'washington-showcase-ai'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2019-12-01-preview' = {
  name: 'washingtonshowcasereg01'
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2021-04-15' = {
  name: 'washingtonshowcaseacct'
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableFreeTier: false
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
  }
}

resource eventHubNamespace 'Microsoft.EventHub/namespaces@2018-01-01-preview' = {
  name: 'washingtonshowcaseeh'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    isAutoInflateEnabled: false
    maximumThroughputUnits: 0
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: 'washington-showcase-kv'
  location: location
  properties: {
    enabledForDeployment: false
    enabledForTemplateDeployment: false
    enabledForDiskEncryption: false
    tenantId: subscription().tenantId
    accessPolicies: []
    sku: {
      name: 'standard'
      family: 'A'
    }
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2020-10-01' = {
  name: 'washington-showcase-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource publicIp 'Microsoft.Network/publicIPAddresses@2020-06-01' = {
  name: 'washington-showcase-pip'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAddressVersion: 'IPv4'
    publicIPAllocationMethod: 'Static'
    idleTimeoutInMinutes: 4
  }
}

resource redisCache 'Microsoft.Cache/redis@2019-07-01' = {
  name: 'washington-showcase-redis'
  location: location
  properties: {
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 1
    }
  }
}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2017-04-01' = {
  name: 'washington-showcase-sbus'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
  properties: {}
}

resource vm 'Microsoft.Compute/virtualMachines@2020-06-01' = {
  name: 'SampleVM'
  location: location
  properties: {
    osProfile: {
      computerName: 'SampleVM'
      adminUsername: 'adminUsername'
      adminPassword: 'adminPassword'
      windowsConfiguration: {
        provisionVMAgent: true
      }
    }
    hardwareProfile: {
      vmSize: 'Standard_DS1_v2'
    }
    storageProfile: {
      imageReference: {
        publisher: 'MicrosoftWindowsServer'
        offer: 'WindowsServer'
        sku: '2019-Datacenter'
        version: 'latest'
      }
      osDisk: {
        createOption: 'FromImage'
      }
    }
  }
}

resource aks 'Microsoft.ContainerService/managedClusters@2020-09-01' = {
  name: clusterName
  location: location
  properties: {
    dnsPrefix: dnsPrefix
    agentPoolProfiles: [
      {
        name: 'agentpool'
        osDiskSizeGB: osDiskSizeGB
        count: agentCount
        vmSize: agentVMSize
        osType: 'Linux'
        mode: 'System'
      }
    ]
    linuxProfile: {
      adminUsername: linuxAdminUsername
      ssh: {
        publicKeys: [
          {
            keyData: sshRSAPublicKey
          }
        ]
      }
    }
    servicePrincipalProfile: {
      clientId: servicePrincipalClientId
      secret: servicePrincipalClientSecret
    }
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: 'name'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2021-04-01' = {
  parent: storage
  name: 'default'
}

resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-04-01' = {
  parent: blobService
  name: 'azbenchpresscontainer'
}
