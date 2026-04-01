using Washington.Models;

namespace Washington.Mappers;

public class DnsZoneMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/dnsZones";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure DNS",
                ArmRegionName: region,
                MeterName: "DNS Zone",
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Zone"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "Azure DNS Zone — no pricing found");

        var monthlyCost = (decimal)price.UnitPrice;
        return new MonthlyCost(monthlyCost, $"Azure DNS Zone @ ${price.UnitPrice:F4}/mo + query charges");
    }
}
