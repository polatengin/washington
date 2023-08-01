using System.Text.Json;

public class ResourceType
{
  public class AzureResourceType
  {
    public string Name { get; set; }

    public string ServiceName { get; set; }

    public Func<JsonElement, string> Size { get; set; }

    public Func<string, string> Kind { get; set; }

    public Func<string> Location { get; set; }
  }

  public static AzureResourceType[] Types = {
    new AzureResourceType()
    {
      Name = "Microsoft.Compute/virtualMachines",
      ServiceName = "virtual-machines",
      Location = () => "us-west",
      Size = (element) => element.Deserialize<VirtualMachineProperties>()?.hardwareProfile.vmSize ?? "",
      Kind = (size) =>
      {
        var parts = size.ToLower().Split('_');
        if (parts.Length < 2)
        {
          return size;
        }
        return $"linux-{parts[1]}{parts[2]}-{parts[0]}";
      }
    },
    new AzureResourceType()
    {
      Name = "Microsoft.ContainerService/managedClusters",
      ServiceName = "kubernetes-service",
      Location = () => "us-west",
      Size = (element) => element.Deserialize<ManagedClusterProperties>()?.agentPoolProfiles?[0]?.vmSize ?? "", //element.Deserialize<ManagedClusterProperties>()?.agentPoolProfiles?[0]?.vmSize ??
      Kind = (size) =>
      {
        return $"linux";
      }
    },
    new AzureResourceType()
    {
      Name = "Microsoft.Storage/storageAccounts",
      ServiceName = "storage",
      Size = (element) => element.ValueKind == JsonValueKind.Undefined ? element.Deserialize<ManagedClusterProperties>()?.agentPoolProfiles?[0]?.vmSize ?? "" : "",
      Offer = (size) =>
      {
        return $"linux";
      }
    }
  };
}
