using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class TrafficManagerMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/trafficManagerProfiles";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Traffic Manager",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var endpointCount = GetEndpointCount(resource);

        // Look for endpoint-based pricing
        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Endpoint") || p.MeterName.Contains("Health Check")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        // Look for DNS query pricing
        var queryPrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Queries"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null && queryPrice == null)
            return new MonthlyCost(0, $"Traffic Manager ({endpointCount} endpoints) - no pricing found");

        var monthlyCost = 0m;
        var details = $"Traffic Manager ({endpointCount} endpoints)";

        if (price != null)
        {
            monthlyCost += (decimal)price.UnitPrice * endpointCount;
            details += $" @ ${price.UnitPrice:F4}/endpoint";
        }

        // Estimate 1M queries/month
        if (queryPrice != null)
        {
            var queries = 1m; // in millions
            monthlyCost += (decimal)queryPrice.UnitPrice * queries;
            details += $" + ~1M queries";
        }

        return new MonthlyCost(monthlyCost, details);
    }

    private static int GetEndpointCount(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("endpoints", out var endpoints) &&
            endpoints.ValueKind == JsonValueKind.Array)
            return endpoints.GetArrayLength();
        return 2;
    }
}
