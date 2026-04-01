using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class DatabricksWorkspaceMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Databricks/workspaces";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Databricks",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuName(resource);

        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("DBU") || p.MeterName.Contains("All Purpose")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Databricks ({sku}) — usage-based (DBU + VM costs)");

        // Estimate 100 DBU-hours/month
        var dbuHours = 100m;
        var monthlyCost = (decimal)price.UnitPrice * dbuHours;
        return new MonthlyCost(monthlyCost, $"Databricks ({sku}) ~{dbuHours} DBU-hrs @ ${price.UnitPrice:F4}/DBU-hr + VM costs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "standard";
        return "standard";
    }
}
