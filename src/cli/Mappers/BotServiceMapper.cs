using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class BotServiceMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.BotService/botServices";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Bot Service",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuName(resource);

        var price = prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Bot Service ({sku}) - usage-based (messages)");

        // Standard channels are free, Premium is per-message
        if (sku.Equals("F0", StringComparison.OrdinalIgnoreCase))
            return new MonthlyCost(0, $"Bot Service ({sku}) - free tier");

        var monthlyCost = (decimal)price.UnitPrice * 1000m;
        return new MonthlyCost(monthlyCost, $"Bot Service ({sku}) ~1000 messages @ ${price.UnitPrice:F4}/msg");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "F0";
        return "F0";
    }
}
