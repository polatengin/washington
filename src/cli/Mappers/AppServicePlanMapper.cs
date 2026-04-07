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
                SkuName: skuName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var isLinuxPlan = IsLinuxPlan(resource);

        var matchingPrices = prices
            .Where(p => p.UnitOfMeasure == "1 Hour" && MatchesSku(p, skuName))
            .ToList();

        var platformPrices = matchingPrices
            .Where(p => IsLinuxPrice(p) == isLinuxPlan)
            .ToList();

        var price = (platformPrices.Count != 0 ? platformPrices : matchingPrices)
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

    private static bool MatchesSku(PriceRecord price, string skuName)
    {
        var normalizedSkuName = NormalizeSkuName(skuName);

        if (!string.IsNullOrEmpty(price.ArmSkuName) && NormalizeSkuName(price.ArmSkuName) == normalizedSkuName)
            return true;

        return !string.IsNullOrEmpty(price.SkuName) && NormalizeSkuName(price.SkuName) == normalizedSkuName;
    }

    private static string NormalizeSkuName(string skuName) =>
        string.Concat(skuName.Where(c => !char.IsWhiteSpace(c)));

    private static bool IsLinuxPlan(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("reserved", out var reserved) && reserved.ValueKind == JsonValueKind.True)
            return true;

        return resource.Properties.TryGetValue("_kind", out var kind)
            && kind.ValueKind == JsonValueKind.String
            && kind.GetString()?.Contains("linux", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsLinuxPrice(PriceRecord price) =>
        (price.ProductName?.Contains("Linux", StringComparison.OrdinalIgnoreCase) ?? false)
        || (price.MeterName?.Contains("Linux", StringComparison.OrdinalIgnoreCase) ?? false);
}
