using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class IoTHubMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Devices/IotHubs";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var skuName = GetSkuName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "IoT Hub",
                ArmRegionName: region,
                SkuName: skuName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var units = GetUnitCount(resource);

        if (skuName.Equals("F1", StringComparison.OrdinalIgnoreCase))
            return new MonthlyCost(0, "IoT Hub Free tier (8,000 msgs/day)");

        var price = prices
            .Where(p => p.SkuName != null &&
                p.SkuName.Contains(skuName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"IoT Hub {skuName} × {units} — no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * units;
        return new MonthlyCost(monthlyCost, $"IoT Hub {skuName} × {units} unit(s) @ ${price.UnitPrice:F2}/unit/mo");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "S1";
        return "S1";
    }

    private static int GetUnitCount(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var cap) && cap.ValueKind == JsonValueKind.Number)
            return cap.GetInt32();
        return 1;
    }
}
