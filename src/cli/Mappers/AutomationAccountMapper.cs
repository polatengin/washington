using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class AutomationAccountMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Automation/automationAccounts";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Automation",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        // Free tier includes 500 minutes of job run time
        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Job Run") || p.MeterName.Contains("Basic Runtime")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "Automation Account — 500 free mins/mo + usage");

        // Estimate 1000 minutes/month (500 free)
        var billableMinutes = 500m;
        var monthlyCost = (decimal)price.UnitPrice * billableMinutes;
        return new MonthlyCost(monthlyCost, $"Automation Account ~1000 min/mo (500 billable) @ ${price.UnitPrice:F6}/min");
    }
}
