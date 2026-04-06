using Washington.Models;

namespace Washington.Mappers;

public class PurviewAccountMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Purview/accounts";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Microsoft Purview",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Capacity Unit") &&
                p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "Microsoft Purview - usage-based (capacity units + scanning)");

        // Estimate 1 capacity unit
        var monthlyCost = (decimal)price.UnitPrice;
        return new MonthlyCost(monthlyCost, $"Microsoft Purview ~1 CU @ ${price.UnitPrice:F2}/mo + scanning");
    }
}
