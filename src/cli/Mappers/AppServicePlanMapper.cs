using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class AppServicePlanMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Web/serverfarms";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var skuName = GetSkuName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure App Service",
                ArmRegionName: region,
                ArmSkuName: skuName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);

        var price = prices
            .Where(p => p.ArmSkuName != null
                && p.ArmSkuName.Equals(skuName, StringComparison.OrdinalIgnoreCase)
                && p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        // Fallback: match by SKU name in the skuName field
        price ??= prices
            .Where(p => p.SkuName != null
                && p.SkuName.Contains(skuName, StringComparison.OrdinalIgnoreCase)
                && p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"App Service Plan {skuName} - no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
        return new MonthlyCost(monthlyCost, $"App Service Plan {skuName} @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "F1";
        return "F1";
    }
}
