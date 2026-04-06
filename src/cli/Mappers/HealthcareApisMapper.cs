using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class HealthcareApisMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.HealthcareApis/services";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure API for FHIR",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Structured Data Storage") &&
                p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "Azure API for FHIR - usage-based (RU + storage)");

        // Estimate 10 GB storage
        var estimatedGb = 10m;
        var monthlyCost = (decimal)price.UnitPrice * estimatedGb;
        return new MonthlyCost(monthlyCost, $"Azure API for FHIR ~{estimatedGb} GB @ ${price.UnitPrice:F4}/GB + RU charges");
    }
}
