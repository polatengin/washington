using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class DataFactoryMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.DataFactory/factories";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Data Factory v2",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        // Orchestration pricing (pipeline activity runs)
        var orchestrationPrice = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Orchestration") || p.MeterName.Contains("Activity Runs")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        // Data movement pricing
        var dataMovementPrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Data Movement"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (orchestrationPrice == null && dataMovementPrice == null)
            return new MonthlyCost(0, "Data Factory — usage-based (per activity run + data movement)");

        var monthlyCost = 0m;
        var details = "Data Factory";

        // Estimate 1,000 activity runs + 100 DIU-hours
        if (orchestrationPrice != null)
        {
            var activityRuns = 1_000m;
            monthlyCost += (decimal)orchestrationPrice.UnitPrice * activityRuns;
            details += $" ~{activityRuns:N0} activity runs";
        }

        if (dataMovementPrice != null)
        {
            var diuHours = 100m;
            monthlyCost += (decimal)dataMovementPrice.UnitPrice * diuHours;
            details += $" + ~{diuHours} DIU-hrs";
        }

        return new MonthlyCost(monthlyCost, details);
    }
}
