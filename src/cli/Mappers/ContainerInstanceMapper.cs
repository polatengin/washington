using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ContainerInstanceMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.ContainerInstance/containerGroups";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Container Instances",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var (totalCpu, totalMemoryGb) = GetResources(resource);

        var cpuPrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("vCPU Duration"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        var memPrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Memory Duration"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (cpuPrice == null && memPrice == null)
            return new MonthlyCost(0, $"Container Instance {totalCpu} vCPU / {totalMemoryGb} GB - no pricing found");

        var secondsPerMonth = HoursPerMonth * 3600m;
        var monthlyCost = 0m;

        if (cpuPrice != null)
            monthlyCost += (decimal)cpuPrice.UnitPrice * totalCpu * secondsPerMonth;

        if (memPrice != null)
            monthlyCost += (decimal)memPrice.UnitPrice * totalMemoryGb * secondsPerMonth;

        return new MonthlyCost(monthlyCost, $"Container Instance {totalCpu} vCPU / {totalMemoryGb} GB @ {HoursPerMonth} hrs/mo");
    }

    private static (decimal cpu, decimal memoryGb) GetResources(ResourceDescriptor resource)
    {
        var totalCpu = 0m;
        var totalMem = 0m;

        if (resource.Properties.TryGetValue("containers", out var containers) &&
            containers.ValueKind == JsonValueKind.Array)
        {
            foreach (var container in containers.EnumerateArray())
            {
                if (container.TryGetProperty("properties", out var props) &&
                    props.ValueKind == JsonValueKind.Object &&
                    props.TryGetProperty("resources", out var res) &&
                    res.ValueKind == JsonValueKind.Object &&
                    res.TryGetProperty("requests", out var requests) &&
                    requests.ValueKind == JsonValueKind.Object)
                {
                    if (requests.TryGetProperty("cpu", out var cpu))
                    {
                        if (cpu.ValueKind == JsonValueKind.Number)
                            totalCpu += cpu.GetDecimal();
                        else if (cpu.ValueKind == JsonValueKind.String && decimal.TryParse(cpu.GetString(), out var c))
                            totalCpu += c;
                    }

                    if (requests.TryGetProperty("memoryInGB", out var mem))
                    {
                        if (mem.ValueKind == JsonValueKind.Number)
                            totalMem += mem.GetDecimal();
                        else if (mem.ValueKind == JsonValueKind.String && decimal.TryParse(mem.GetString(), out var m))
                            totalMem += m;
                    }
                }
            }
        }

        return (totalCpu > 0 ? totalCpu : 1m, totalMem > 0 ? totalMem : 1.5m);
    }
}
