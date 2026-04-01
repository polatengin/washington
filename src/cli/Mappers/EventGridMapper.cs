using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class EventGridMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.EventGrid/topics";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase) ||
        resource.ResourceType.Equals("Microsoft.EventGrid/domains", StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Event Grid",
                ArmRegionName: region,
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Operations"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "Event Grid — usage-based (per million operations)");

        // Estimate 1M operations/month
        var estimatedMillions = 1m;
        var monthlyCost = (decimal)price.UnitPrice * estimatedMillions;
        return new MonthlyCost(monthlyCost, $"Event Grid ~1M ops @ ${price.UnitPrice:F4}/M ops");
    }
}
