using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class MapsAccountMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Maps/accounts";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Maps",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuName(resource);

        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Map") &&
                p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Azure Maps ({sku}) - usage-based (transactions)");

        // Estimate 10,000 transactions/month
        var transactions = 10_000m;
        var monthlyCost = (decimal)price.UnitPrice * (transactions / 1000m);
        return new MonthlyCost(monthlyCost, $"Azure Maps ({sku}) ~{transactions:N0} txn @ ${price.UnitPrice:F4}/1K txn");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "G2";
        return "G2";
    }
}
