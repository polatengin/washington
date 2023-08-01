using System.Text.Json;

public class ARMTemplate
{
  public List<Resource> resources { get; set; }
}

public class Resource
{
  public string type { get; set; }
  public string apiVersion { get; set; }
  public string name { get; set; }
  public JsonElement properties { get; set; }
  public string location { get; set; }
  public string size { get; set; }
  public string serviceName { get; set; }
  public string kind { get; set; }
  public decimal estimatedMonthlyCost { get; set; }
}

public class VirtualMachineProperties
{
  public class HardwareProfile
  {
    public string vmSize { get; set; }
  }

  public HardwareProfile hardwareProfile { get; set; }
}

public class ManagedClusterProperties
{
  public string dnsPrefix { get; set; }
  public AgentPoolProfile[] agentPoolProfiles { get; set; }
  public LinuxProfile linuxProfile { get; set; }
  public ServicePrincipalProfile servicePrincipalProfile { get; set; }

  public class LinuxProfile
  {
    public string adminUsername { get; set; }
    public Ssh ssh { get; set; }
  }

  public class Ssh
  {
    public Publickey[] publicKeys { get; set; }
  }

  public class Publickey
  {
    public string keyData { get; set; }
  }

  public class ServicePrincipalProfile
  {
    public string clientId { get; set; }
    public string secret { get; set; }
  }

  public class AgentPoolProfile
  {
    public string name { get; set; }
    public string osDiskSizeGB { get; set; }
    public string count { get; set; }
    public string vmSize { get; set; }
    public string osType { get; set; }
    public string mode { get; set; }
  }
}
