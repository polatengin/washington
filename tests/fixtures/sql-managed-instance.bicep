param location string = 'eastus'

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2023-09-01' = {
  name: 'washingtonmivnet'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.30.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'managedinstancesubnet'
        properties: {
          addressPrefix: '10.30.0.0/24'
          delegations: [
            {
              name: 'sqlManagedInstanceDelegation'
              properties: {
                serviceName: 'Microsoft.Sql/managedInstances'
              }
            }
          ]
        }
      }
    ]
  }
}

resource sqlManagedInstance 'Microsoft.Sql/managedInstances@2023-08-01-preview' = {
  name: 'washingtonsqlmi01'
  location: location
  sku: {
    name: 'GP_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 4
  }
  properties: {
    administratorLogin: 'sqlmiadmin'
    administratorLoginPassword: 'Placeholder123!'
    subnetId: resourceId('Microsoft.Network/virtualNetworks/subnets', virtualNetwork.name, 'managedinstancesubnet')
    licenseType: 'LicenseIncluded'
    vCores: 4
    storageSizeInGB: 32
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}
