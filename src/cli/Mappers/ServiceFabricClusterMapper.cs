using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ServiceFabricClusterMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.ServiceFabric/clusters";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var vmSize = GetNodeVmSize(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Virtual Machines",
                ArmRegionName: region,
                ArmSkuName: vmSize,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var vmSize = GetNodeVmSize(resource);
        var nodeCount = GetNodeCount(resource);

        var price = prices
            .Where(p => !string.IsNullOrEmpty(p.ArmSkuName) &&
                p.ArmSkuName.Equals(vmSize, StringComparison.OrdinalIgnoreCase) &&
                p.UnitOfMeasure == "1 Hour" &&
                p.MeterName != null && !p.MeterName.Contains("Low Priority") &&
                p.MeterName != null && !p.MeterName.Contains("Spot"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Service Fabric {nodeCount}× {vmSize} - no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * nodeCount;
        return new MonthlyCost(monthlyCost, $"Service Fabric {nodeCount}× {vmSize} @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetNodeVmSize(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("nodeTypes", out var nodeTypes) &&
            nodeTypes.ValueKind == JsonValueKind.Array)
        {
            foreach (var nodeType in nodeTypes.EnumerateArray())
            {
                if (nodeType.TryGetProperty("vmSize", out var vmSize) &&
                    vmSize.ValueKind == JsonValueKind.String)
                    return vmSize.GetString() ?? "Standard_D2s_v3";
            }
        }
        return "Standard_D2s_v3";
    }

    private static int GetNodeCount(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("nodeTypes", out var nodeTypes) &&
            nodeTypes.ValueKind == JsonValueKind.Array)
        {
            var total = 0;
            foreach (var nodeType in nodeTypes.EnumerateArray())
            {
                if (nodeType.TryGetProperty("vmInstanceCount", out var count) &&
                    count.ValueKind == JsonValueKind.Number)
                    total += count.GetInt32();
            }
            if (total > 0) return total;
        }
        return 5;
    }
}
