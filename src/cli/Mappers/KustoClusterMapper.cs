using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class KustoClusterMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Kusto/clusters";

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
                ServiceName: "Azure Data Explorer",
                ArmRegionName: region,
                SkuName: skuName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var capacity = GetCapacity(resource);

        var price = prices
            .Where(p => p.UnitOfMeasure == "1 Hour" && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Data Explorer {skuName} ({capacity} instances) - no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * capacity;
        return new MonthlyCost(monthlyCost, $"Data Explorer {skuName} ({capacity} instances) @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Dev(No SLA)_Standard_E2a_v4";
        return "Dev(No SLA)_Standard_E2a_v4";
    }

    private static int GetCapacity(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var cap) && cap.ValueKind == JsonValueKind.Number)
            return cap.GetInt32();
        return 2;
    }
}
