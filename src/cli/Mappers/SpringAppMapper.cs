using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class SpringAppMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.AppPlatform/Spring";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Spring Apps",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuName(resource);

        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("vCPU") || p.MeterName.Contains("App Instance")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Spring Apps {sku} - no pricing found");

        // Estimate 2 vCPU baseline
        var vCpus = 2m;
        var monthlyCost = (decimal)price.UnitPrice * vCpus * HoursPerMonth;
        return new MonthlyCost(monthlyCost, $"Spring Apps {sku} ~{vCpus} vCPU @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Standard";
        return "Standard";
    }
}
