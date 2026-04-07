param location string = 'eastus'

resource containerEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: 'washingtoncontainerenv'
  location: location
  properties: {}
}

resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'washingtoncontainerapp'
  location: location
  properties: {
    managedEnvironmentId: containerEnvironment.id
    template: {
      containers: [
        {
          name: 'app'
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          resources: {
            cpu: 0.5
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}
