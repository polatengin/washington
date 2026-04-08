using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class FrontDoorMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Cdn/profiles";

    private const decimal HoursPerMonth = 730m;
    private const decimal EstimatedRequestsPerMonth = 1m;
    private const int IncludedRoutingRules = 5;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var skuName = GetPricingSkuName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Front Door Service",
                ArmRegionName: string.Empty,
                SkuName: skuName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetPricingSkuName(resource);
        var displaySku = GetDisplaySkuName(resource);
        var requestPrice = GetRequestPrice(prices, skuName);
        var routingRulePrice = GetRoutingRulePrice(prices, skuName);
        var routingRuleCount = GetRoutingRuleCount(resource);

        var monthlyCost = 0m;
        var details = $"Front Door {displaySku}";

        if (requestPrice != null)
        {
            var requestCost = EstimateRequestCost(requestPrice);
            monthlyCost += requestCost;
            details += $" ~1M requests @ {FormatRequestPrice(requestPrice)}";
        }

        if (routingRulePrice != null && routingRuleCount > IncludedRoutingRules)
        {
            var billableRules = routingRuleCount - IncludedRoutingRules;
            var rulesCost = (decimal)routingRulePrice.UnitPrice * HoursPerMonth * billableRules;
            monthlyCost += rulesCost;
            details += $" + {billableRules} routing rule overage @ ${routingRulePrice.UnitPrice:F4}/hr × {HoursPerMonth:F0} hrs";
        }

        if (monthlyCost == 0)
            return new MonthlyCost(0, $"Front Door {displaySku} - usage-based (requests + data transfer + rules)");

        details += routingRuleCount > IncludedRoutingRules
            ? " + data transfer"
            : " + data transfer/rules";

        return new MonthlyCost(monthlyCost, details);
    }

    private static string GetPricingSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() switch
            {
                "Premium_AzureFrontDoor" => "Premium",
                _ => "Standard"
            };

        return "Standard";
    }

    private static string GetDisplaySkuName(ResourceDescriptor resource) => GetPricingSkuName(resource);

    private static PriceRecord? GetRequestPrice(List<PriceRecord> prices, string skuName) =>
        PreferGlobalPrice(prices.Where(p =>
            string.Equals(p.MeterName, $"{skuName} Requests", StringComparison.OrdinalIgnoreCase)
            && IsRequestPrice(p)))
        ?? PreferGlobalPrice(prices.Where(p =>
            string.Equals(p.MeterName, $"{skuName} Default Request", StringComparison.OrdinalIgnoreCase)
            && IsRequestPrice(p)));

    private static PriceRecord? GetRoutingRulePrice(List<PriceRecord> prices, string skuName) =>
        PreferGlobalPrice(prices.Where(p =>
            string.Equals(p.MeterName, $"{skuName} Overage Routing Rules", StringComparison.OrdinalIgnoreCase)
            && IsHourlyPrice(p)))
        ?? PreferGlobalPrice(prices.Where(p =>
            string.Equals(p.MeterName, $"{skuName} Included Routing Rules", StringComparison.OrdinalIgnoreCase)
            && IsHourlyPrice(p)
            && p.TierMinimumUnits >= IncludedRoutingRules
            && p.UnitPrice > 0));

    private static PriceRecord? PreferGlobalPrice(IEnumerable<PriceRecord> prices) =>
        prices
            .OrderBy(p => string.IsNullOrEmpty(p.ArmRegionName) ? 0 : 1)
            .ThenBy(p => GetUnitPreference(p.UnitOfMeasure))
            .ThenBy(p => p.UnitPrice)
            .FirstOrDefault();

    private static int GetUnitPreference(string? unitOfMeasure) => unitOfMeasure switch
    {
        "1M/Month" => 0,
        "100K" => 1,
        "10K" => 2,
        _ => 3
    };

    private static bool IsRequestPrice(PriceRecord price) =>
        price.UnitPrice > 0
        && (string.Equals(price.UnitOfMeasure, "1M/Month", StringComparison.OrdinalIgnoreCase)
            || string.Equals(price.UnitOfMeasure, "100K", StringComparison.OrdinalIgnoreCase)
            || string.Equals(price.UnitOfMeasure, "10K", StringComparison.OrdinalIgnoreCase));

    private static bool IsHourlyPrice(PriceRecord price) =>
        string.Equals(price.UnitOfMeasure, "1 Hour", StringComparison.OrdinalIgnoreCase)
        || string.Equals(price.UnitOfMeasure, "1/Hour", StringComparison.OrdinalIgnoreCase);

    private static decimal EstimateRequestCost(PriceRecord price)
    {
        var units = price.UnitOfMeasure switch
        {
            "1M/Month" => EstimatedRequestsPerMonth,
            "100K" => EstimatedRequestsPerMonth * 10m,
            "10K" => EstimatedRequestsPerMonth * 100m,
            _ => 0m
        };

        return (decimal)price.UnitPrice * units;
    }

    private static string FormatRequestPrice(PriceRecord price) => price.UnitOfMeasure switch
    {
        "1M/Month" => $"${price.UnitPrice:F4}/M requests",
        "100K" => $"${price.UnitPrice:F4}/100K requests",
        "10K" => $"${price.UnitPrice:F4}/10K requests",
        _ => $"${price.UnitPrice:F4}/{price.UnitOfMeasure}"
    };

    private static int GetRoutingRuleCount(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("routes", out var routes) && routes.ValueKind == JsonValueKind.Array)
            return routes.GetArrayLength();

        if (resource.Properties.TryGetValue("routingRules", out var routingRules) && routingRules.ValueKind == JsonValueKind.Array)
            return routingRules.GetArrayLength();

        return 0;
    }
}
