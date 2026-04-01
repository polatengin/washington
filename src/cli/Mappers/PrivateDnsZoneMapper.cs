using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class PrivateDnsZoneMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/privateDnsZones";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure DNS",
                ArmRegionName: region,
                ProductName: "Azure DNS - Private Zones",
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        // Hosted zones pricing
        var zonePrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Zone"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (zonePrice == null)
            return new MonthlyCost(0, "Private DNS Zone — no pricing found");

        var monthlyCost = (decimal)zonePrice.UnitPrice;
        return new MonthlyCost(monthlyCost, $"Private DNS Zone @ ${zonePrice.UnitPrice:F4}/zone/mo + queries");
    }
}
