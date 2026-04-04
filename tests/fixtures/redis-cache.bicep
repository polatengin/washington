param location string = 'eastus'

resource redisCache 'Microsoft.Cache/redis@2019-07-01' = {
  name: 'washington-demo-redis'
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