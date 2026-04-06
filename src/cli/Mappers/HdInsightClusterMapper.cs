using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class HdInsightClusterMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.HDInsight/clusters";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var vmSize = GetHeadNodeVmSize(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "HDInsight",
                ArmRegionName: region,
                ArmSkuName: vmSize,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var vmSize = GetHeadNodeVmSize(resource);
        var clusterKind = GetClusterKind(resource);

        var price = prices
            .Where(p => p.UnitOfMeasure == "1 Hour" && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"HDInsight ({clusterKind}) {vmSize} - no pricing found");

        // Estimate 2 head nodes + 3 worker nodes
        var nodeCount = 5;
        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * nodeCount;
        return new MonthlyCost(monthlyCost, $"HDInsight ({clusterKind}) ~{nodeCount} nodes × {vmSize} @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetHeadNodeVmSize(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("computeProfile", out var cp) &&
            cp.ValueKind == JsonValueKind.Object &&
            cp.TryGetProperty("roles", out var roles) &&
            roles.ValueKind == JsonValueKind.Array)
        {
            foreach (var role in roles.EnumerateArray())
            {
                if (role.TryGetProperty("hardwareProfile", out var hw) &&
                    hw.ValueKind == JsonValueKind.Object &&
                    hw.TryGetProperty("vmSize", out var vmSize) &&
                    vmSize.ValueKind == JsonValueKind.String)
                    return vmSize.GetString() ?? "Standard_D3_v2";
            }
        }
        return "Standard_D3_v2";
    }

    private static string GetClusterKind(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("clusterDefinition", out var cd) &&
            cd.ValueKind == JsonValueKind.Object &&
            cd.TryGetProperty("kind", out var kind) &&
            kind.ValueKind == JsonValueKind.String)
            return kind.GetString() ?? "hadoop";
        return "hadoop";
    }
}
