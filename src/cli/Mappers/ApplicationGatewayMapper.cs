using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ApplicationGatewayMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/applicationGateways";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var tier = GetTier(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Application Gateway",
                ArmRegionName: region,
                ProductName: GetProductName(tier),
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var tier = GetTier(resource);
        var fixedCostPrice = prices
            .Where(IsFixedHourlyPrice)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        var capacityUnitPrice = prices
            .Where(IsCapacityUnitHourlyPrice)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (fixedCostPrice == null)
            return new MonthlyCost(0, $"App Gateway {FormatTier(tier)} - no pricing found");

        var monthlyCost = (decimal)fixedCostPrice.UnitPrice * HoursPerMonth;
        var details = $"App Gateway {FormatTier(tier)} fixed cost @ ${fixedCostPrice.UnitPrice:F4}/hr × {HoursPerMonth:F0} hrs";

        details += capacityUnitPrice == null
            ? " + capacity units (usage-based)"
            : $" + capacity units @ ${capacityUnitPrice.UnitPrice:F4}/hr (usage-based)";

        return new MonthlyCost(monthlyCost, details);
    }

    private static string GetTier(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("tier", out var tier) && tier.ValueKind == JsonValueKind.String)
            return tier.GetString() ?? "Standard_v2";
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Standard_v2";
        return "Standard_v2";
    }

    private static string GetProductName(string tier) => tier switch
    {
        "Standard_v2" => "Application Gateway Standard v2",
        "WAF_v2" => "Application Gateway WAF v2",
        "Basic_v2" => "Application Gateway Basic v2",
        _ => $"Application Gateway {FormatTier(tier)}"
    };

    private static string FormatTier(string tier) => tier.Replace("_", " ", StringComparison.Ordinal);

    private static bool IsFixedHourlyPrice(PriceRecord price) =>
        IsHourlyPrice(price)
        && price.MeterName?.Contains("Fixed Cost", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsCapacityUnitHourlyPrice(PriceRecord price) =>
        IsHourlyPrice(price)
        && price.MeterName?.Contains("Capacity Units", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsHourlyPrice(PriceRecord price) =>
        string.Equals(price.UnitOfMeasure, "1 Hour", StringComparison.OrdinalIgnoreCase)
        || string.Equals(price.UnitOfMeasure, "1/Hour", StringComparison.OrdinalIgnoreCase);
}
