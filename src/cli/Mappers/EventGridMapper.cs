using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class EventGridMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.EventGrid/topics";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase) ||
        resource.ResourceType.Equals("Microsoft.EventGrid/domains", StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Event Grid",
                ArmRegionName: region,
                MeterName: "Standard Operations",
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var estimatedUnitsPerMonth = 10m;
        var price = GetOperationsPrice(prices, estimatedUnitsPerMonth, out var includedUnitsPerMonth);

        if (price == null)
            return new MonthlyCost(0, "Event Grid - usage-based (per million operations)");

        var billableUnitsPerMonth = Math.Max(estimatedUnitsPerMonth - includedUnitsPerMonth, 0m);
        var monthlyCost = (decimal)price.UnitPrice * billableUnitsPerMonth;
        var billableOperations = billableUnitsPerMonth * 100_000m;

        if (includedUnitsPerMonth > 0)
        {
            return new MonthlyCost(
                monthlyCost,
                $"Event Grid ~{billableOperations:N0} billable after {includedUnitsPerMonth * 100_000m:N0} included @ ${price.UnitPrice:F4}/100K ops");
        }

        return new MonthlyCost(monthlyCost, $"Event Grid ~1M ops @ ${price.UnitPrice:F4}/100K ops");
    }

    private static PriceRecord? GetOperationsPrice(
        List<PriceRecord> prices,
        decimal estimatedUnitsPerMonth,
        out decimal includedUnitsPerMonth)
    {
        var operationPrices = prices
            .Where(IsTopicOperationsPrice)
            .OrderBy(p => p.TierMinimumUnits)
            .ToList();

        includedUnitsPerMonth = 0m;

        var paidPrice = operationPrices
            .Where(p => p.UnitPrice > 0 && (decimal)p.TierMinimumUnits <= estimatedUnitsPerMonth)
            .OrderByDescending(p => p.TierMinimumUnits)
            .FirstOrDefault()
            ?? operationPrices
                .Where(p => p.UnitPrice > 0)
                .OrderBy(p => p.TierMinimumUnits)
                .FirstOrDefault();

        if (paidPrice == null)
            return operationPrices.FirstOrDefault();

        if (operationPrices.Any(p => p.UnitPrice == 0) && paidPrice.TierMinimumUnits > 0)
            includedUnitsPerMonth = (decimal)paidPrice.TierMinimumUnits;

        return paidPrice;
    }

    private static bool IsTopicOperationsPrice(PriceRecord price) =>
        string.Equals(price.MeterName, "Standard Operations", StringComparison.OrdinalIgnoreCase)
        && string.Equals(price.UnitOfMeasure, "100K", StringComparison.OrdinalIgnoreCase);
}
