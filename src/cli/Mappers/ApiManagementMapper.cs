using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ApiManagementMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.ApiManagement/service";

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
                ServiceName: "API Management",
                ArmRegionName: region,
                SkuName: skuName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var units = GetCapacity(resource);

        if (skuName.Equals("Consumption", StringComparison.OrdinalIgnoreCase))
        {
            var callPrice = prices
                .Where(p => p.MeterName != null && p.MeterName.Contains("Call"))
                .OrderBy(p => p.UnitPrice)
                .FirstOrDefault();

            if (callPrice == null)
                return new MonthlyCost(0, "APIM Consumption - pay per call");

            // Estimate 1M calls/month (first 1M free)
            return new MonthlyCost(0, "APIM Consumption - first 1M calls free");
        }

        var price = prices
            .Where(p => p.SkuName != null &&
                p.SkuName.Equals(skuName, StringComparison.OrdinalIgnoreCase) &&
                p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitOfMeasure == "1 Hour" && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"APIM {skuName} {units} unit(s) - no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * units;
        return new MonthlyCost(monthlyCost, $"APIM {skuName} {units} unit(s) @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Developer";
        return "Developer";
    }

    private static int GetCapacity(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var cap) && cap.ValueKind == JsonValueKind.Number)
            return cap.GetInt32();
        return 1;
    }
}
