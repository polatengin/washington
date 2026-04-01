using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class NotificationHubMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.NotificationHubs/namespaces";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase) ||
        resource.ResourceType.Equals("Microsoft.NotificationHubs/namespaces/notificationHubs", StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Notification Hubs",
                ArmRegionName: region,
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuName(resource);

        if (sku.Equals("Free", StringComparison.OrdinalIgnoreCase))
            return new MonthlyCost(0, "Notification Hub Free tier");

        var price = prices
            .Where(p => p.MeterName != null &&
                p.MeterName.Contains(sku, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Notification Hub {sku} — no pricing found");

        var monthlyCost = (decimal)price.UnitPrice;
        return new MonthlyCost(monthlyCost, $"Notification Hub {sku} @ ${price.UnitPrice:F2}/mo + pushes");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Basic";
        return "Basic";
    }
}
