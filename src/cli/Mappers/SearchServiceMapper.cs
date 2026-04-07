using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class SearchServiceMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Search/searchServices";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var skuName = GetRetailSkuName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Cognitive Search",
                ArmRegionName: region,
                SkuName: skuName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetRetailSkuName(resource);
        var displaySkuName = GetDisplaySkuName(resource);
        var replicaCount = GetReplicaCount(resource);
        var partitionCount = GetPartitionCount(resource);

        if (skuName.Equals("Free", StringComparison.OrdinalIgnoreCase))
            return new MonthlyCost(0, "AI Search Free - no charge");

        var price = prices
            .Where(p => p.UnitOfMeasure == "1 Hour"
                && p.UnitPrice > 0
                && string.Equals(p.SkuName, skuName, StringComparison.OrdinalIgnoreCase)
                && p.MeterName != null
                && p.MeterName.EndsWith("Unit", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitOfMeasure == "1 Hour"
                && p.UnitPrice > 0
                && p.MeterName != null
                && p.MeterName.EndsWith("Unit", StringComparison.OrdinalIgnoreCase)
                && !p.MeterName.Contains("Semantic", StringComparison.OrdinalIgnoreCase)
                && !p.MeterName.Contains("CC", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"AI Search {displaySkuName} ({replicaCount}R x {partitionCount}P) - no pricing found");

        var units = replicaCount * partitionCount;
        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * units;
        return new MonthlyCost(monthlyCost, $"AI Search {displaySkuName} ({replicaCount}R x {partitionCount}P = {units} SU) @ ${price.UnitPrice:F4}/hr x {HoursPerMonth} hrs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "basic";
        return "basic";
    }

    private static string GetRetailSkuName(ResourceDescriptor resource) => GetSkuName(resource).Trim().ToLowerInvariant() switch
    {
        "free" => "Free",
        "basic" => "Basic",
        "standard" or "standard_s1" => "Standard S1",
        "standard2" or "standard_s2" => "Standard S2",
        "standard3" or "standard_s3" => "Standard S3",
        "storage_optimized_l1" => "Storage Optimized L1",
        "storage_optimized_l2" => "Storage Optimized L2",
        _ => GetSkuName(resource)
    };

    private static string GetDisplaySkuName(ResourceDescriptor resource)
    {
        var rawSkuName = GetSkuName(resource);
        return rawSkuName.Equals("standard", StringComparison.OrdinalIgnoreCase)
            ? "standard"
            : rawSkuName;
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
