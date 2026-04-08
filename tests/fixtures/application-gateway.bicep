param location string = 'eastus'

resource publicIp 'Microsoft.Network/publicIPAddresses@2023-09-01' = {
  name: 'washingtonappgwpip'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
  }
}

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2023-09-01' = {
  name: 'washingtonappgwvnet'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.30.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'appGatewaySubnet'
        properties: {
          addressPrefix: '10.30.0.0/24'
        }
      }
    ]
  }
}

resource applicationGateway 'Microsoft.Network/applicationGateways@2023-09-01' = {
  name: 'washingtonappgw01'
  location: location
  sku: {
    name: 'Standard_v2'
    tier: 'Standard_v2'
    capacity: 1
  }
  properties: {
    gatewayIPConfigurations: [
      {
        name: 'appGatewayIpConfig'
        properties: {
          subnet: {
            id: resourceId('Microsoft.Network/virtualNetworks/subnets', virtualNetwork.name, 'appGatewaySubnet')
          }
        }
      }
    ]
    frontendIPConfigurations: [
      {
        name: 'appGatewayFrontendIp'
        properties: {
          publicIPAddress: {
            id: publicIp.id
          }
        }
      }
    ]
    frontendPorts: [
      {
        name: 'port80'
        properties: {
          port: 80
        }
      }
    ]
    backendAddressPools: [
      {
        name: 'backendPool'
        properties: {
          backendAddresses: [
            {
              ipAddress: '10.30.1.4'
            }
          ]
        }
      }
    ]
    backendHttpSettingsCollection: [
      {
        name: 'httpSettings'
        properties: {
          port: 80
          protocol: 'Http'
          cookieBasedAffinity: 'Disabled'
          requestTimeout: 20
        }
      }
    ]
    httpListeners: [
      {
        name: 'listener'
        properties: {
          frontendIPConfiguration: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendIPConfigurations', 'washingtonappgw01', 'appGatewayFrontendIp')
          }
          frontendPort: {
            id: resourceId('Microsoft.Network/applicationGateways/frontendPorts', 'washingtonappgw01', 'port80')
          }
          protocol: 'Http'
        }
      }
    ]
    requestRoutingRules: [
      {
        name: 'rule1'
        properties: {
          ruleType: 'Basic'
          priority: 100
          httpListener: {
            id: resourceId('Microsoft.Network/applicationGateways/httpListeners', 'washingtonappgw01', 'listener')
          }
          backendAddressPool: {
            id: resourceId('Microsoft.Network/applicationGateways/backendAddressPools', 'washingtonappgw01', 'backendPool')
          }
          backendHttpSettings: {
            id: resourceId('Microsoft.Network/applicationGateways/backendHttpSettingsCollection', 'washingtonappgw01', 'httpSettings')
          }
        }
      }
    ]
  }
}
