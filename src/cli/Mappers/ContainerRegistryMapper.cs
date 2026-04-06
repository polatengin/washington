using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ContainerRegistryMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.ContainerRegistry/registries";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var tier = GetSkuTier(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Container Registry",
                ArmRegionName: region,
                SkuName: tier,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var tier = GetSkuTier(resource);

        // Look for per-day registry pricing
        var price = prices
            .Where(p => p.SkuName != null &&
                p.SkuName.Equals(tier, StringComparison.OrdinalIgnoreCase) &&
                p.MeterName != null && p.MeterName.Contains("Registry Unit"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        // Fallback: any matching SKU price
        price ??= prices
            .Where(p => p.SkuName != null &&
                p.SkuName.Equals(tier, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Container Registry {tier} - no pricing found");

        decimal monthlyCost;
        if (price.UnitOfMeasure == "1/Day")
            monthlyCost = (decimal)price.UnitPrice * 30m;
        else if (price.UnitOfMeasure == "1 Hour")
            monthlyCost = (decimal)price.UnitPrice * 730m;
        else
            monthlyCost = (decimal)price.UnitPrice;

        return new MonthlyCost(monthlyCost, $"Container Registry {tier} @ ${price.UnitPrice:F4}/{price.UnitOfMeasure}");
    }

    private static string GetSkuTier(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Basic";
        return "Basic";
    }
}
