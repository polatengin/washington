using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ExpressRouteCircuitMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/expressRouteCircuits";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "ExpressRoute",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var (tier, bandwidth) = GetTierAndBandwidth(resource);

        var price = prices
            .Where(p => p.MeterName != null &&
                p.MeterName.Contains(bandwidth, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0 && p.UnitOfMeasure != null &&
                p.UnitOfMeasure.Contains("Month", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"ExpressRoute {tier} {bandwidth} — no pricing found");

        var monthlyCost = (decimal)price.UnitPrice;
        return new MonthlyCost(monthlyCost, $"ExpressRoute {tier} {bandwidth} @ ${price.UnitPrice:F2}/mo + data");
    }

    private static (string tier, string bandwidth) GetTierAndBandwidth(ResourceDescriptor resource)
    {
        var tier = "Standard";
        var bandwidth = "50Mbps";

        if (resource.Sku.TryGetValue("tier", out var skuTier) && skuTier.ValueKind == JsonValueKind.String)
            tier = skuTier.GetString() ?? "Standard";

        if (resource.Properties.TryGetValue("bandwidthInMbps", out var bw))
        {
            if (bw.ValueKind == JsonValueKind.Number)
                bandwidth = $"{bw.GetInt32()}Mbps";
            else if (bw.ValueKind == JsonValueKind.String)
                bandwidth = $"{bw.GetString()}Mbps";
        }

        return (tier, bandwidth);
    }
}
