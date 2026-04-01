using Washington.Mappers;
using Washington.Models;
using Washington.Services;

namespace Washington.Services;

public class CostAggregator
{
    private readonly MapperRegistry _mapperRegistry;
    private readonly PricingApiClient _pricingClient;

    public CostAggregator(MapperRegistry mapperRegistry, PricingApiClient pricingClient)
    {
        _mapperRegistry = mapperRegistry;
        _pricingClient = pricingClient;
    }

    public async Task<CostReport> GenerateReportAsync(
        List<ResourceDescriptor> resources)
    {
        var lines = new List<ResourceCostLine>();
        var warnings = new List<string>();
        var grandTotal = 0m;

        foreach (var resource in resources)
        {
            var mapper = _mapperRegistry.GetMapper(resource);
            if (mapper == null)
            {
                warnings.Add($"⚠ No pricing mapper for {resource.ResourceType} — skipped");
                continue;
            }

            var queries = mapper.BuildQueries(resource);
            var allPrices = new List<PriceRecord>();

            foreach (var query in queries)
            {
                var prices = await _pricingClient.QueryPricesAsync(query);
                allPrices.AddRange(prices);
            }

            var cost = mapper.CalculateCost(resource, allPrices);

            lines.Add(new ResourceCostLine(
                ResourceType: resource.ResourceType,
                ResourceName: resource.Name,
                PricingDetails: cost.Details,
                MonthlyCost: cost.Amount
            ));

            grandTotal += cost.Amount;
        }

        return new CostReport(lines, grandTotal, warnings);
    }
}
