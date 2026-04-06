using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ContainerAppsEnvironmentMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.App/managedEnvironments";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Container Apps",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var workloadProfile = GetWorkloadProfileType(resource);

        // Consumption-only environments are free (apps pay per use)
        if (workloadProfile.Equals("Consumption", StringComparison.OrdinalIgnoreCase))
            return new MonthlyCost(0, "Container Apps Environment (Consumption) - apps billed per use");

        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Workload Profile") || p.MeterName.Contains("Dedicated")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Container Apps Environment ({workloadProfile}) - no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
        return new MonthlyCost(monthlyCost, $"Container Apps Environment ({workloadProfile}) @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetWorkloadProfileType(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("workloadProfiles", out var profiles) &&
            profiles.ValueKind == JsonValueKind.Array)
        {
            foreach (var profile in profiles.EnumerateArray())
            {
                if (profile.TryGetProperty("workloadProfileType", out var pType) &&
                    pType.ValueKind == JsonValueKind.String)
                    return pType.GetString() ?? "Consumption";
            }
        }
        return "Consumption";
    }
}
