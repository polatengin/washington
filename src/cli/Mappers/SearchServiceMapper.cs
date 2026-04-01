using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class SearchServiceMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Search/searchServices";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;
        var skuName = GetSkuName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure AI Search",
                ArmRegionName: region,
                SkuName: skuName,
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var replicaCount = GetReplicaCount(resource);
        var partitionCount = GetPartitionCount(resource);

        var price = prices
            .Where(p => p.UnitOfMeasure == "1 Hour" && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"AI Search {skuName} ({replicaCount}R × {partitionCount}P) — no pricing found");

        var units = replicaCount * partitionCount;
        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * units;
        return new MonthlyCost(monthlyCost, $"AI Search {skuName} ({replicaCount}R × {partitionCount}P = {units} SU) @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "basic";
        return "basic";
    }

    private static int GetReplicaCount(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("replicaCount", out var rc) && rc.ValueKind == JsonValueKind.Number)
            return rc.GetInt32();
        return 1;
    }

    private static int GetPartitionCount(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("partitionCount", out var pc) && pc.ValueKind == JsonValueKind.Number)
            return pc.GetInt32();
        return 1;
    }
}
