using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class MachineLearningWorkspaceMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.MachineLearningServices/workspaces";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Machine Learning",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuName(resource);

        // Workspace itself may be free (Basic) or charged (Enterprise)
        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Workspace") || p.MeterName.Contains("Studio")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"ML Workspace ({sku}) — base free, compute billed separately");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
        return new MonthlyCost(monthlyCost, $"ML Workspace ({sku}) @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs + compute");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Basic";
        return "Basic";
    }
}
