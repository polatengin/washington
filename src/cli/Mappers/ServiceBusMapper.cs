using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ServiceBusMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.ServiceBus/namespaces";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;
        var tier = GetSkuTier(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Service Bus",
                ArmRegionName: region,
                SkuName: tier,
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var tier = GetSkuTier(resource);

        if (tier.Equals("Basic", StringComparison.OrdinalIgnoreCase))
        {
            var opPrice = prices
                .Where(p => p.MeterName != null && p.MeterName.Contains("Messaging"))
                .OrderBy(p => p.UnitPrice)
                .FirstOrDefault();

            if (opPrice == null)
                return new MonthlyCost(0, $"Service Bus {tier} — pay per operation");

            // Estimate 1M operations/month
            var estimatedOps = 1_000_000m;
            var monthlyCost = (decimal)opPrice.UnitPrice * (estimatedOps / 1_000_000m);
            return new MonthlyCost(monthlyCost, $"Service Bus {tier} ~{estimatedOps:N0} ops");
        }

        // Standard/Premium: base charge
        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Base") || p.MeterName.Contains("Unit")) &&
                p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitOfMeasure == "1 Hour" && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Service Bus {tier} — no pricing found");

        var mu = GetMessagingUnits(resource);
        var cost = (decimal)price.UnitPrice * HoursPerMonth * mu;
        return new MonthlyCost(cost, $"Service Bus {tier} {mu} unit(s) @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuTier(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Standard";
        return "Standard";
    }

    private static int GetMessagingUnits(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var cap) && cap.ValueKind == JsonValueKind.Number)
            return cap.GetInt32();
        return 1;
    }
}
