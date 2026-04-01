using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class VirtualDesktopHostPoolMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.DesktopVirtualization/hostPools";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Virtual Desktop",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var hostPoolType = GetHostPoolType(resource);

        var price = prices
            .Where(p => p.UnitOfMeasure == "1 Hour" && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Azure Virtual Desktop ({hostPoolType}) — usage-based (session hosts + licensing)");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
        return new MonthlyCost(monthlyCost, $"Azure Virtual Desktop ({hostPoolType}) @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetHostPoolType(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("hostPoolType", out var hpt) &&
            hpt.ValueKind == JsonValueKind.String)
            return hpt.GetString() ?? "Pooled";
        return "Pooled";
    }
}
