param location string = 'eastus'

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'washingtonmlai01'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: 'washingtonmlstore01'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2021-10-01' = {
  name: 'washington-ml-kv'
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: []
    publicNetworkAccess: 'Enabled'
  }
}

resource workspace 'Microsoft.MachineLearningServices/workspaces@2022-05-01' = {
  name: 'washingtonmlws01'
  location: location
  properties: {
    applicationInsights: appInsights.id
    keyVault: keyVault.id
    publicNetworkAccess: 'Enabled'
    storageAccount: storageAccount.id
    friendlyName: 'washingtonmlws01'
    v1LegacyMode: false
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}
