using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class BastionHostMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/bastionHosts";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Bastion",
                ArmRegionName: region,
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuName(resource);
        var scaleUnits = GetScaleUnits(resource);

        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Gateway") &&
                p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitOfMeasure == "1 Hour" && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Bastion {sku} — no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * scaleUnits;
        return new MonthlyCost(monthlyCost, $"Bastion {sku} ({scaleUnits} scale units) @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Standard";
        return "Standard";
    }

    private static int GetScaleUnits(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("scaleUnits", out var su))
        {
            if (su.ValueKind == JsonValueKind.Number)
                return su.GetInt32();
        }
        return 2;
    }
}
