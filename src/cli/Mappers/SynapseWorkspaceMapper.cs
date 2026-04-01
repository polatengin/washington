using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class SynapseWorkspaceMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Synapse/workspaces";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Synapse Analytics",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        // Serverless SQL pricing (per TB processed)
        var sqlPrice = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Serverless") || p.MeterName.Contains("Data Processed")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        // Dedicated pool pricing
        var poolPrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("DWU") &&
                p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (sqlPrice == null && poolPrice == null)
            return new MonthlyCost(0, "Synapse Workspace — usage-based (serverless + pools)");

        var monthlyCost = 0m;
        var details = "Synapse Workspace";

        if (sqlPrice != null)
        {
            // Estimate 1 TB/month serverless
            var tbProcessed = 1m;
            monthlyCost += (decimal)sqlPrice.UnitPrice * tbProcessed;
            details += $" ~{tbProcessed} TB serverless SQL";
        }

        if (poolPrice != null)
        {
            monthlyCost += (decimal)poolPrice.UnitPrice * HoursPerMonth;
            details += $" + pool @ ${poolPrice.UnitPrice:F4}/DWU-hr";
        }

        return new MonthlyCost(monthlyCost, details);
    }
}
