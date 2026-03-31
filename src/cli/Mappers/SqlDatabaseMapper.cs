using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class SqlDatabaseMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Sql/servers/databases";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;
        var skuName = GetSkuName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "SQL Database",
                ArmRegionName: region,
                SkuName: skuName,
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);

        // For DTU model, look for compute pricing
        var price = prices
            .Where(p => p.SkuName != null
                && p.SkuName.Equals(skuName, StringComparison.OrdinalIgnoreCase)
                && p.MeterName != null
                && (p.MeterName.Contains("DTU") || p.MeterName.Contains("vCore") || p.MeterName.Contains("Compute")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        // Fallback to any matching price
        price ??= prices
            .Where(p => p.SkuName != null
                && p.SkuName.Equals(skuName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"SQL DB {skuName} — no pricing found");

        decimal monthlyCost;
        if (price.UnitOfMeasure == "1 Hour")
            monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
        else if (price.UnitOfMeasure == "1/Day")
            monthlyCost = (decimal)price.UnitPrice * 30m;
        else
            monthlyCost = (decimal)price.UnitPrice;

        return new MonthlyCost(monthlyCost, $"SQL DB {skuName} @ ${price.UnitPrice:F4}/{price.UnitOfMeasure}");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "S0";
        return "S0";
    }
}
