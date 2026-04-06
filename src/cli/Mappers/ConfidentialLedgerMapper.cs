using Washington.Models;

namespace Washington.Mappers;

public class ConfidentialLedgerMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.ConfidentialLedger/ledgers";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Confidential Ledger",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var price = prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "Confidential Ledger - usage-based (writes + storage)");

        var monthlyCost = (decimal)price.UnitPrice;
        return new MonthlyCost(monthlyCost, $"Confidential Ledger @ ${price.UnitPrice:F4}/unit + storage");
    }
}
