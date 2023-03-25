import fetch from "node-fetch";

let token = "";

const getToken = async () => {
  if (token) {
    return token;
  }
  const tenantId = '<tenant_id>';
  const clientId = '<client_id>';
  const clientSecret = '<client_secret>';

  const authenticationEndpoint = `https://login.microsoftonline.com/${tenantId}/oauth2/token`;
  const resource = 'https://management.core.windows.net/';

  const response = await fetch(authenticationEndpoint, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded'
    },
    body: `grant_type=client_credentials&client_id=${clientId}&client_secret=${encodeURIComponent(clientSecret)}&resource=${resource}`
  });

  const json = await response.json() as { access_token: string };
  token = json.access_token;
  return token;
};

export const getCost = async (serviceName: string) => {
  const response = await fetch(`https://azure.microsoft.com/api/v3/pricing/${serviceName}/calculator/`);
  const json: any = await response.json();
  console.log({ json, getcost: JSON.stringify(json) });
};

export const getEstimatedCost = async () => {
  const payload = {
    'type': 'Microsoft.Compute/virtualMachines',
    'billingPeriod': 'P1M',
    'region': 'westus2',
    'properties': {
      'reservedInstance': false,
      'quantity': 1,
      'sku': {
        'name': 'Standard_D2s_v3',
        'tier': 'Standard',
        'size': 'Standard_D2s_v3',
        'family': 'standardDv3Family'
      },
      'location': 'westus2',
      'purchaseModel': 'OnDemand',
      'diskDetails': [
        {
          'name': 'osdisk',
          'quantity': 1,
          'size': 128,
          'type': 'Standard_LRS'
        },
        {
          'name': 'datadisk',
          'quantity': 2,
          'size': 512,
          'type': 'StandardSSD_LRS'
        }
      ]
    }
  };

  const response = await fetch('https://management.azure.com/providers/Microsoft.Pricing/calculate?api-version=2019-02-01', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${await getToken()}`
    },
    body: JSON.stringify(payload)
  });

  const data = await response.json() as { properties: { prices: { unitPrice: number }[] } };
  const estimatedCost = data?.properties?.prices?.[0]?.unitPrice ?? 0;

  console.log({data, getestimatedcost: JSON.stringify(data)});

  console.log(`Estimated cost: ${estimatedCost} USD`);
};
