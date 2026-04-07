using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ServiceBusMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.ServiceBus/namespaces";

    private const decimal HoursPerMonth = 730m;
    private const decimal BasicEstimatedOperationMillions = 1m;
    private const decimal FallbackEstimatedOperationMillions = 100m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var tier = GetSkuTier(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Service Bus",
                ArmRegionName: region,
                SkuName: tier,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var tier = GetSkuTier(resource);

        if (tier.Equals("Basic", StringComparison.OrdinalIgnoreCase))
        {
            var opPrice = GetMessagingOperationsPrice(prices, BasicEstimatedOperationMillions);

            if (opPrice == null)
                return new MonthlyCost(0, $"Service Bus {tier} - pay per operation");

            return CreateMessagingOperationsCost(tier, opPrice, BasicEstimatedOperationMillions);
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
        {
            var opPrice = GetMessagingOperationsPrice(prices, FallbackEstimatedOperationMillions);
            if (opPrice != null)
                return CreateMessagingOperationsCost(tier, opPrice, FallbackEstimatedOperationMillions);

            return new MonthlyCost(0, $"Service Bus {tier} - no pricing found");
        }

        var mu = GetMessagingUnits(resource);
        var cost = (decimal)price.UnitPrice * HoursPerMonth * mu;
        return new MonthlyCost(cost, $"Service Bus {tier} {mu} unit(s) @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static PriceRecord? GetMessagingOperationsPrice(List<PriceRecord> prices, decimal estimatedOperationMillions)
    {
        var operationPrices = prices
            .Where(p => p.MeterName != null &&
                p.MeterName.Contains("Messaging Operations", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.UnitOfMeasure, "1M", StringComparison.OrdinalIgnoreCase) &&
                p.UnitPrice > 0)
            .OrderBy(p => p.TierMinimumUnits)
            .ToList();

        return operationPrices
            .Where(p => (decimal)p.TierMinimumUnits <= estimatedOperationMillions)
            .OrderByDescending(p => p.TierMinimumUnits)
            .FirstOrDefault()
            ?? operationPrices.FirstOrDefault();
    }

    private static MonthlyCost CreateMessagingOperationsCost(string tier, PriceRecord price, decimal estimatedOperationMillions)
    {
        var monthlyCost = (decimal)price.UnitPrice * estimatedOperationMillions;
        return new MonthlyCost(monthlyCost, $"Service Bus {tier} ~{estimatedOperationMillions:F0}M ops @ ${price.UnitPrice:F4}/M ops");
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
