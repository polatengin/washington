using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class StaticWebAppMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Web/staticSites";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var skuName = GetSkuName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Static Web Apps",
                ArmRegionName: region,
                SkuName: skuName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);

        if (skuName.Equals("Free", StringComparison.OrdinalIgnoreCase))
            return new MonthlyCost(0, "Static Web App Free — no charge");

        var price = prices
            .Where(p => p.SkuName != null &&
                p.SkuName.Equals(skuName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Static Web App {skuName} — no pricing found");

        decimal monthlyCost;
        if (price.UnitOfMeasure == "1/Month")
            monthlyCost = (decimal)price.UnitPrice;
        else if (price.UnitOfMeasure == "1 Hour")
            monthlyCost = (decimal)price.UnitPrice * 730m;
        else if (price.UnitOfMeasure == "1/Day")
            monthlyCost = (decimal)price.UnitPrice * 30m;
        else
            monthlyCost = (decimal)price.UnitPrice;

        return new MonthlyCost(monthlyCost, $"Static Web App {skuName} @ ${price.UnitPrice:F4}/{price.UnitOfMeasure}");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Free";
        if (resource.Sku.TryGetValue("tier", out var tier) && tier.ValueKind == JsonValueKind.String)
            return tier.GetString() ?? "Free";
        return "Free";
    }
}
