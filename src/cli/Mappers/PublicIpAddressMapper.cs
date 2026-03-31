using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class PublicIpAddressMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/publicIPAddresses";

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
                ServiceName: "Virtual Network",
                ArmRegionName: region,
                MeterName: skuName == "Standard" ? "Standard IPv4 Static Public IP" : "Basic IPv4 Static Public IP",
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var allocation = GetAllocationMethod(resource);

        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Public IP"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Public IP {skuName} {allocation} — no pricing found");

        decimal monthlyCost;
        if (price.UnitOfMeasure == "1 Hour")
            monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
        else
            monthlyCost = (decimal)price.UnitPrice;

        return new MonthlyCost(monthlyCost, $"Public IP {skuName} {allocation} @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Standard";
        return "Standard";
    }

    private static string GetAllocationMethod(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("publicIPAllocationMethod", out var method) &&
            method.ValueKind == JsonValueKind.String)
            return method.GetString() ?? "Static";
        return "Static";
    }
}
