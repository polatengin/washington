using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class AppConfigurationMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.AppConfiguration/configurationStores";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure App Configuration",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuName(resource);

        if (sku.Equals("free", StringComparison.OrdinalIgnoreCase))
            return new MonthlyCost(0, "App Configuration Free tier");

        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Standard") || p.MeterName.Contains("Unit")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"App Configuration {sku} — no pricing found");

        var monthlyCost = (decimal)price.UnitPrice;
        return new MonthlyCost(monthlyCost, $"App Configuration {sku} @ ${price.UnitPrice:F2}/day + requests");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "standard";
        return "standard";
    }
}
