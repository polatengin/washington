using Washington.Models;

namespace Washington.Mappers;

public class LoadTestingMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.LoadTestService/loadTests";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Load Testing",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Virtual User") &&
                p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "Azure Load Testing — usage-based (VUH)");

        // Estimate 100 VUH/month
        var vuh = 100m;
        var monthlyCost = (decimal)price.UnitPrice * vuh;
        return new MonthlyCost(monthlyCost, $"Azure Load Testing ~{vuh} VUH @ ${price.UnitPrice:F4}/VUH");
    }
}
