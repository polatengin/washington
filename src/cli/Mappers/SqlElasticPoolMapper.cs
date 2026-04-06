using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class SqlElasticPoolMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Sql/servers/elasticPools";

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
                ServiceName: "SQL Database",
                ArmRegionName: region,
                ArmSkuName: skuName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var dtuCapacity = GetDtuCapacity(resource);

        var price = prices
            .Where(p => !string.IsNullOrEmpty(p.ArmSkuName)
                && p.ArmSkuName.Equals(skuName, StringComparison.OrdinalIgnoreCase)
                && p.MeterName != null && p.MeterName.Contains("Elastic Pool")
                && p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("eDTU") &&
                p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"SQL Elastic Pool {skuName} ({dtuCapacity} DTU) - no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
        return new MonthlyCost(monthlyCost, $"SQL Elastic Pool {skuName} ({dtuCapacity} DTU) @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "StandardPool";
        return "StandardPool";
    }

    private static int GetDtuCapacity(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var cap) && cap.ValueKind == JsonValueKind.Number)
            return cap.GetInt32();
        return 50;
    }
}
