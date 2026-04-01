using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class SqlManagedInstanceMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Sql/managedInstances";

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
                ServiceName: "SQL Managed Instance",
                ArmRegionName: region,
                SkuName: skuName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var vCores = GetVCores(resource);

        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("vCore") || p.MeterName.Contains("Compute")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"SQL MI {skuName} {vCores} vCores — no pricing found");

        decimal monthlyCost;
        if (price.UnitOfMeasure == "1 Hour")
            monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * vCores;
        else
            monthlyCost = (decimal)price.UnitPrice * vCores;

        return new MonthlyCost(monthlyCost, $"SQL MI {skuName} {vCores} vCores @ ${price.UnitPrice:F4}/{price.UnitOfMeasure}");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "GP_Gen5";
        return "GP_Gen5";
    }

    private static int GetVCores(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var cap) && cap.ValueKind == JsonValueKind.Number)
            return cap.GetInt32();
        if (resource.Properties.TryGetValue("vCores", out var vc) && vc.ValueKind == JsonValueKind.Number)
            return vc.GetInt32();
        return 4;
    }
}
