using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class AppConfigurationMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.AppConfiguration/configurationStores";

    private const decimal HoursPerMonth = 730m;
    private const decimal DaysPerMonth = 30m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var sku = NormalizeSkuName(GetSkuName(resource));

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "App Configuration",
                ArmRegionName: region,
                SkuName: sku,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = NormalizeSkuName(GetSkuName(resource));

        if (sku.Equals("free", StringComparison.OrdinalIgnoreCase))
            return new MonthlyCost(0, "App Configuration Free tier");

        var price = prices
            .Where(p => MatchesSku(p, sku) && IsBaseInstancePrice(p))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => MatchesSku(p, sku) && IsInstancePrice(p))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => MatchesSku(p, sku) && p.UnitPrice > 0)
            .OrderByDescending(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderByDescending(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"App Configuration {sku} - no pricing found");

        decimal monthlyCost;
        string details;

        if (price.UnitOfMeasure == "1/Day")
        {
            monthlyCost = (decimal)price.UnitPrice * DaysPerMonth;
            details = $"App Configuration {sku} @ ${price.UnitPrice:F2}/day × {DaysPerMonth:F0} days + requests";
        }
        else if (price.UnitOfMeasure == "1 Hour")
        {
            monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
            details = $"App Configuration {sku} @ ${price.UnitPrice:F4}/hr × {HoursPerMonth:F0} hrs + requests";
        }
        else
        {
            monthlyCost = (decimal)price.UnitPrice;
            details = $"App Configuration {sku} @ ${price.UnitPrice:F4}/{price.UnitOfMeasure} + requests";
        }

        return new MonthlyCost(monthlyCost, details);
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "standard";
        return "standard";
    }

    private static string NormalizeSkuName(string skuName) => skuName.Trim().ToLowerInvariant() switch
    {
        "free" => "Free",
        "developer" => "Developer",
        "premium" => "Premium",
        _ => "Standard"
    };

    private static bool MatchesSku(PriceRecord price, string sku) =>
        string.Equals(price.SkuName, sku, StringComparison.OrdinalIgnoreCase)
        || string.Equals(price.ArmSkuName, sku, StringComparison.OrdinalIgnoreCase);

    private static bool IsInstancePrice(PriceRecord price) =>
        !string.IsNullOrEmpty(price.MeterName)
        && price.MeterName.Contains("Instance", StringComparison.OrdinalIgnoreCase);

    private static bool IsBaseInstancePrice(PriceRecord price) =>
        IsInstancePrice(price)
        && !price.MeterName!.Contains("Replica", StringComparison.OrdinalIgnoreCase);
}
