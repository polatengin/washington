using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ContainerAppMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.App/containerApps";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Container Apps",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var (vCpu, memoryGb) = GetResources(resource);

        var vCpuPrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("vCPU"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        var memPrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Memory"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (vCpuPrice == null && memPrice == null)
            return new MonthlyCost(0, $"Container App {vCpu} vCPU / {memoryGb} GB - no pricing found");

        // Assume always-on: 730 hrs/month
        var hoursPerMonth = 730m;
        var monthlyCost = 0m;
        var details = $"Container App {vCpu} vCPU / {memoryGb} GB";

        if (vCpuPrice != null)
            monthlyCost += (decimal)vCpuPrice.UnitPrice * vCpu * hoursPerMonth;

        if (memPrice != null)
            monthlyCost += (decimal)memPrice.UnitPrice * memoryGb * hoursPerMonth;

        return new MonthlyCost(monthlyCost, details + $" @ {hoursPerMonth} hrs/mo");
    }

    private static (decimal vCpu, decimal memoryGb) GetResources(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("template", out var template) &&
            template.ValueKind == JsonValueKind.Object &&
            template.TryGetProperty("containers", out var containers) &&
            containers.ValueKind == JsonValueKind.Array)
        {
            foreach (var container in containers.EnumerateArray())
            {
                if (container.TryGetProperty("resources", out var res) &&
                    res.ValueKind == JsonValueKind.Object)
                {
                    var cpu = 0.5m;
                    var mem = 1.0m;

                    if (res.TryGetProperty("cpu", out var cpuProp))
                    {
                        if (cpuProp.ValueKind == JsonValueKind.Number)
                            cpu = cpuProp.GetDecimal();
                        else if (cpuProp.ValueKind == JsonValueKind.String &&
                            decimal.TryParse(cpuProp.GetString(), out var parsed))
                            cpu = parsed;
                    }

                    if (res.TryGetProperty("memory", out var memProp) &&
                        memProp.ValueKind == JsonValueKind.String)
                    {
                        var memStr = memProp.GetString() ?? "1Gi";
                        if (memStr.EndsWith("Gi") &&
                            decimal.TryParse(memStr.Replace("Gi", ""), out var gi))
                            mem = gi;
                    }

                    return (cpu, mem);
                }
            }
        }
        return (0.5m, 1.0m);
    }
}
