using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class CosmosDbAccountMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.DocumentDB/databaseAccounts";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Cosmos DB",
                ArmRegionName: region,
                MeterName: "100 RU/s",
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var throughput = GetProvisionedThroughput(resource);
        var freeTierEnabled = IsFreeTierEnabled(resource);

        var matchingPrices = prices
            .Where(IsProvisionedThroughputPrice)
            .ToList();

        var preferredPrices = matchingPrices
            .Where(p => IsFreeTierPrice(p) == freeTierEnabled)
            .ToList();

        var price = (preferredPrices.Count != 0 ? preferredPrices : matchingPrices)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Cosmos DB {throughput} RU/s - no pricing found");

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

    private static bool IsFreeTierEnabled(ResourceDescriptor resource) =>
        resource.Properties.TryGetValue("enableFreeTier", out var enableFreeTier)
        && enableFreeTier.ValueKind == JsonValueKind.True;

    private static bool IsProvisionedThroughputPrice(PriceRecord price) =>
        price.MeterName != null
        && price.MeterName.Equals("100 RU/s", StringComparison.OrdinalIgnoreCase)
        && (price.UnitOfMeasure == "1/Hour" || price.UnitOfMeasure == "1 Hour");

    private static bool IsFreeTierPrice(PriceRecord price) =>
        price.SkuName?.Contains("Free Tier", StringComparison.OrdinalIgnoreCase) == true;
}
