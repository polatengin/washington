using Washington.Models;

namespace Washington.Mappers;

public class PurviewAccountMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Purview/accounts";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var skuName = GetSkuName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Purview",
                ArmRegionName: region,
                SkuName: skuName,
                MeterName: $"{skuName} Capacity Unit",
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var capacityUnits = GetCapacityUnits(resource);

        var price = prices
            .Where(p => string.Equals(p.SkuName, skuName, StringComparison.OrdinalIgnoreCase)
                && p.MeterName != null
                && p.MeterName.Contains("Capacity Unit", StringComparison.OrdinalIgnoreCase)
                && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "Microsoft Purview - usage-based (capacity units + scanning)");

        decimal monthlyCost;
        string details;

        if (price.UnitOfMeasure == "1 Hour")
        {
            monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * capacityUnits;
            details = $"Microsoft Purview {skuName} {capacityUnits} CU @ ${price.UnitPrice:F4}/hr x {HoursPerMonth:F0} hrs + scanning";
        }
        else
        {
            monthlyCost = (decimal)price.UnitPrice * capacityUnits;
            details = $"Microsoft Purview {skuName} {capacityUnits} CU @ ${price.UnitPrice:F2}/{price.UnitOfMeasure} + scanning";
        }

        return new MonthlyCost(monthlyCost, details);
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == System.Text.Json.JsonValueKind.String)
            return name.GetString() ?? "Standard";

        return "Standard";
    }

    private static int GetCapacityUnits(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var capacity) && capacity.ValueKind == System.Text.Json.JsonValueKind.Number)
            return capacity.GetInt32();

        return 1;
    }
}
