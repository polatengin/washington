using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class FunctionAppMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Web/sites";

    public bool CanMap(ResourceDescriptor resource)
    {
        if (!resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase))
            return false;

        // Only match function apps, not regular web apps
        var kind = GetKind(resource);
        return kind.Contains("functionapp", StringComparison.OrdinalIgnoreCase);
    }

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Functions",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        // Look for execution-based pricing
        var execPrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Execution"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        var memoryPrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Memory"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (execPrice == null && memoryPrice == null)
            return new MonthlyCost(0, "Azure Functions Consumption - pay per execution + free grant");

        // Estimate 1M executions/month and 400,000 GB-s
        var monthlyCost = 0m;
        var details = "Azure Functions";

        if (execPrice != null)
        {
            var estimatedExecs = 1_000_000m;
            monthlyCost += (decimal)execPrice.UnitPrice * (estimatedExecs / 1_000_000m);
            details += $" ~{estimatedExecs:N0} execs";
        }

        if (memoryPrice != null)
        {
            var estimatedGbSeconds = 400_000m;
            monthlyCost += (decimal)memoryPrice.UnitPrice * estimatedGbSeconds;
            details += $" + memory";
        }

        return new MonthlyCost(monthlyCost, details + " (Consumption plan)");
    }

    private static string GetKind(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("_kind", out var kind) && kind.ValueKind == JsonValueKind.String)
            return kind.GetString() ?? "";
        return "";
    }
}
