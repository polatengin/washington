using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class LoadBalancerMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/loadBalancers";

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
                ServiceName: "Load Balancer",
                ArmRegionName: region,
                SkuName: skuName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var ruleCount = GetRuleCount(resource);

        // Basic SKU is free
        if (skuName.Equals("Basic", StringComparison.OrdinalIgnoreCase))
            return new MonthlyCost(0, "Load Balancer Basic - free");

        // Standard SKU: look for hourly pricing
        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Load Balancer") &&
                p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        // Fallback to rules pricing
        var rulesPrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Rule"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null && rulesPrice == null)
            return new MonthlyCost(0, $"Load Balancer {skuName} - no pricing found");

        var monthlyCost = 0m;
        var details = $"Load Balancer {skuName}";

        if (price != null)
        {
            monthlyCost += (decimal)price.UnitPrice * HoursPerMonth;
            details += $" @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs";
        }

        if (rulesPrice != null && ruleCount > 0)
        {
            var rulesCost = (decimal)rulesPrice.UnitPrice * HoursPerMonth * ruleCount;
            monthlyCost += rulesCost;
            details += $" + {ruleCount} rules";
        }

        return new MonthlyCost(monthlyCost, details);
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Standard";
        return "Standard";
    }

    private static int GetRuleCount(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("loadBalancingRules", out var rules) &&
            rules.ValueKind == JsonValueKind.Array)
            return rules.GetArrayLength();
        return 0;
    }
}
