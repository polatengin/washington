using System.Text.Json;

public class ResourceType
{
  public class AzureResourceType
  {
    public required string Name { get; set; }

    public required string ServiceName { get; set; }

    public required Func<Resource, string> Size { get; set; }

    public Func<Resource, string> Kind { get; set; }

    public Func<string> Location { get; set; }
  }

  public static AzureResourceType[] Types = {
    new AzureResourceType()
    {
      Name = "Microsoft.Compute/virtualMachines",
      ServiceName = "Virtual Machines",
      Location = () => "us-west",
      Size = (element) => element.properties.Deserialize<VirtualMachineProperties>()?.hardwareProfile.vmSize ?? "",
      Kind = (element) =>
      {
        var parts = element.size.ToLower().Split('_');
        if (parts.Length < 2)
        {
          return element.size;
        }
        return $"linux-{parts[1]}{parts[2]}-{parts[0]}";
      }
    },
    new AzureResourceType()
    {
      Name = "Microsoft.ContainerService/managedClusters",
      ServiceName = "kubernetes-service",
      Location = () => "us-west",
      Size = (element) => element.properties.Deserialize<ManagedClusterProperties>()?.agentPoolProfiles?[0]?.vmSize ?? "",
      Kind = (element) =>
      {
        return $"linux";
      }
    },
    new AzureResourceType()
    {
      Name = "Microsoft.Storage/storageAccounts",
      ServiceName = "storage",
      Location = () => "us-west",
      Size = (element) => "storageAccount",
      Kind = (element) =>
      {
        return $"linux";
      }
    },
    new AzureResourceType()
    {
      Name = "Microsoft.Storage/storageAccounts/blobServices",
      ServiceName = "",
      Location = () => "us-west",
      Size = (element) => "0",
      Kind = (element) =>
      {
        return $"linux";
      }
    },
    new AzureResourceType()
    {
      Name = "Microsoft.Storage/storageAccounts/blobServices/containers",
      ServiceName = "",
      Location = () => "us-west",
      Size = (element) => "0",
      Kind = (element) =>
      {
        return $"linux";
      }
    }
  };
}
