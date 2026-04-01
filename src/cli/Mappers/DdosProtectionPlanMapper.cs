using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class DdosProtectionPlanMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/ddosProtectionPlans";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure DDoS Protection",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("DDoS Protection"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "DDoS Protection Plan — no pricing found");

        var monthlyCost = (decimal)price.UnitPrice;
        return new MonthlyCost(monthlyCost, $"DDoS Protection Plan @ ${price.UnitPrice:F2}/mo + overage");
    }
}
