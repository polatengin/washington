using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class CosmosDbAccountMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.DocumentDB/databaseAccounts";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Cosmos DB",
                ArmRegionName: region,
                MeterName: "100 RU/s",
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var throughput = GetProvisionedThroughput(resource);

        // Look for RU/s pricing
        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("RU"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Cosmos DB {throughput} RU/s — no pricing found");

        // Pricing is per 100 RU/s per hour
        var ruBlocks = throughput / 100m;
        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * ruBlocks;
        return new MonthlyCost(monthlyCost, $"Cosmos DB {throughput} RU/s @ ${price.UnitPrice:F4}/100 RU/hr × {HoursPerMonth} hrs + usage");
    }

    private static int GetProvisionedThroughput(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("databaseAccountOfferType", out var offer) &&
            offer.ValueKind == JsonValueKind.String)
        {
            // Standard tier - check for throughput in capabilities or use default
        }

        // Default provisioned throughput (400 RU/s minimum)
        return 400;
    }
}
