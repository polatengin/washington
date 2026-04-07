using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ContainerAppMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.App/containerApps";

    private const string ConsumptionSkuName = "Standard";
    private const decimal HoursPerMonth = 730m;
    private const decimal SecondsPerMonth = HoursPerMonth * 3600m;

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
                SkuName: ConsumptionSkuName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var (vCpu, memoryGb, minReplicas) = GetResources(resource);

        if (minReplicas <= 0)
            return new MonthlyCost(0, $"Container App {vCpu} vCPU / {memoryGb} GB - scale-to-zero baseline; active usage + requests");

        var vCpuPrice = GetUsagePrice(prices, "vCPU Idle Usage", "vCPU Active Usage");
        var memPrice = GetUsagePrice(prices, "Memory Idle Usage", "Memory Active Usage");

        if (vCpuPrice == null && memPrice == null)
            return new MonthlyCost(0, $"Container App {vCpu} vCPU / {memoryGb} GB - no pricing found");

        var monthlyCost = 0m;
        var replicaLabel = minReplicas == 1 ? "1 idle min replica" : $"{minReplicas} idle min replicas";
        var details = $"Container App {vCpu} vCPU / {memoryGb} GB ({replicaLabel}) @ {HoursPerMonth} hrs/mo + active usage/requests";

        if (vCpuPrice != null)
            monthlyCost += (decimal)vCpuPrice.UnitPrice * vCpu * SecondsPerMonth * minReplicas;

        if (memPrice != null)
            monthlyCost += (decimal)memPrice.UnitPrice * memoryGb * SecondsPerMonth * minReplicas;

        return new MonthlyCost(monthlyCost, details);
    }

    private static (decimal vCpu, decimal memoryGb, int minReplicas) GetResources(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("template", out var template) &&
            template.ValueKind == JsonValueKind.Object)
        {
            var minReplicas = GetMinReplicas(template);

            if (template.TryGetProperty("containers", out var containers) &&
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

                        return (cpu, mem, minReplicas);
                    }
                }
            }

            return (0.5m, 1.0m, minReplicas);
        }

        return (0.5m, 1.0m, 0);
    }

    private static int GetMinReplicas(JsonElement template)
    {
        if (template.TryGetProperty("scale", out var scale) &&
            scale.ValueKind == JsonValueKind.Object &&
            scale.TryGetProperty("minReplicas", out var minReplicas))
        {
            if (minReplicas.ValueKind == JsonValueKind.Number)
                return minReplicas.GetInt32();

            if (minReplicas.ValueKind == JsonValueKind.String &&
                int.TryParse(minReplicas.GetString(), out var parsed))
                return parsed;
        }

        return 0;
    }

    private static PriceRecord? GetUsagePrice(List<PriceRecord> prices, string preferredMeterSuffix, string fallbackMeterSuffix) =>
        prices.FirstOrDefault(p => MatchesMeter(p, preferredMeterSuffix))
        ?? prices.FirstOrDefault(p => MatchesMeter(p, fallbackMeterSuffix));

    private static bool MatchesMeter(PriceRecord price, string meterSuffix) =>
        string.Equals(price.SkuName, ConsumptionSkuName, StringComparison.OrdinalIgnoreCase)
        && price.MeterName != null
        && price.MeterName.EndsWith(meterSuffix, StringComparison.OrdinalIgnoreCase);
}
