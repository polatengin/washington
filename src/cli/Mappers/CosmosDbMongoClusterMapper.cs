using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class CosmosDbMongoClusterMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.DocumentDB/mongoClusters";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Cosmos DB for MongoDB vCore",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuName(resource);
        var nodeCount = GetNodeCount(resource);

        var price = prices
            .Where(p => p.UnitOfMeasure == "1 Hour" && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Cosmos DB Mongo vCore ({sku}) {nodeCount} nodes - no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * nodeCount;
        return new MonthlyCost(monthlyCost, $"Cosmos DB Mongo vCore ({sku}) {nodeCount} nodes @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("nodeGroupSpecs", out var specs) &&
            specs.ValueKind == JsonValueKind.Array)
        {
            foreach (var spec in specs.EnumerateArray())
            {
                if (spec.TryGetProperty("sku", out var sku) &&
                    sku.ValueKind == JsonValueKind.String)
                    return sku.GetString() ?? "M30";
            }
        }
        return "M30";
    }

    private static int GetNodeCount(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("nodeGroupSpecs", out var specs) &&
            specs.ValueKind == JsonValueKind.Array)
        {
            var total = 0;
            foreach (var spec in specs.EnumerateArray())
            {
                if (spec.TryGetProperty("nodeCount", out var count) &&
                    count.ValueKind == JsonValueKind.Number)
                    total += count.GetInt32();
            }
            if (total > 0) return total;
        }
        return 1;
    }
}
