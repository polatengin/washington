param location string = 'eastus'

resource mysqlFlexibleServer 'Microsoft.DBforMySQL/flexibleServers@2023-12-01-preview' = {
  name: 'washingtonmysqlflex01'
  location: location
  sku: {
    name: 'Standard_D2ds_v4'
    tier: 'GeneralPurpose'
  }
  properties: {
    administratorLogin: 'mysqladminuser'
    administratorLoginPassword: 'Placeholder123!'
    version: '8.0.21'
    storage: {
      storageSizeGB: 128
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}
