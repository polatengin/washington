using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class BatchAccountMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Batch/batchAccounts";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Batch",
                ArmRegionName: region,
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var poolAllocationMode = GetPoolAllocationMode(resource);

        // Batch account itself is free; cost is from underlying VMs
        // Look for low-priority pricing as an estimator
        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Low Priority"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Batch ({poolAllocationMode}) — base free, pool VMs billed separately");

        return new MonthlyCost(0, $"Batch ({poolAllocationMode}) — base free, pool VMs billed separately");
    }

    private static string GetPoolAllocationMode(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("poolAllocationMode", out var mode) &&
            mode.ValueKind == JsonValueKind.String)
            return mode.GetString() ?? "BatchService";
        return "BatchService";
    }
}
