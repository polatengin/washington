using System.Text.Json;

public record ARMTemplate(List<Resource> resources);

public record ARMParameter(Dictionary<string, Parameter> parameters);

public class Resource
{
  public required string type { get; set; }
  public required string apiVersion { get; set; }
  public required string name { get; set; }
  public JsonElement properties { get; set; }
  public string? location { get; set; }
  public string? size { get; set; }
  public string? serviceName { get; set; }
  public string? kind { get; set; }
  public double estimatedMonthlyCost { get; set; }
}

public class Parameter
{
  public string? Value { get; set; }
}

public class VirtualMachineProperties
{
  public class HardwareProfile
  {
    public required string vmSize { get; set; }
  }

  public required HardwareProfile hardwareProfile { get; set; }
}

public class ManagedClusterProperties
{
  public required string dnsPrefix { get; set; }
  public required AgentPoolProfile[] agentPoolProfiles { get; set; }
  public required LinuxProfile linuxProfile { get; set; }
  public required ServicePrincipalProfile servicePrincipalProfile { get; set; }

  public class LinuxProfile
  {
    public required string adminUsername { get; set; }
    public required Ssh ssh { get; set; }
  }

  public class Ssh
  {
    public required Publickey[] publicKeys { get; set; }
  }

  public class Publickey
  {
    public required string keyData { get; set; }
  }

  public class ServicePrincipalProfile
  {
    public required string clientId { get; set; }
    public string? secret { get; set; }
  }

  public class AgentPoolProfile
  {
    public required string name { get; set; }
    public required string osDiskSizeGB { get; set; }
    public required string count { get; set; }
    public required string vmSize { get; set; }
    public required string osType { get; set; }
    public required string mode { get; set; }
  }
}
