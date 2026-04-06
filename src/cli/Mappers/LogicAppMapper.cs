using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class LogicAppMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Logic/workflows";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Logic Apps",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        // Look for action execution pricing
        var actionPrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Action"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        var triggerPrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Trigger"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (actionPrice == null && triggerPrice == null)
            return new MonthlyCost(0, "Logic App - usage-based (per execution)");

        var monthlyCost = 0m;
        var details = "Logic App";

        // Estimate 10,000 executions/month with 5 actions each
        var executions = 10_000m;
        var actionsPerExec = 5m;

        if (triggerPrice != null)
        {
            monthlyCost += (decimal)triggerPrice.UnitPrice * executions;
            details += $" ~{executions:N0} triggers";
        }

        if (actionPrice != null)
        {
            monthlyCost += (decimal)actionPrice.UnitPrice * executions * actionsPerExec;
            details += $" + ~{executions * actionsPerExec:N0} actions";
        }

        return new MonthlyCost(monthlyCost, details);
    }
}
