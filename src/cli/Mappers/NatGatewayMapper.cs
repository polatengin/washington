using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class NatGatewayMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/natGateways";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Virtual Network",
                ArmRegionName: region,
                ProductName: "NAT Gateway",
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("NAT Gateway") &&
                p.UnitOfMeasure == "1 Hour")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        var dataPrice = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Data Processed"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "NAT Gateway - no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
        var details = $"NAT Gateway @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs";

        // Estimate 100 GB data processed
        if (dataPrice != null)
        {
            var dataGb = 100m;
            monthlyCost += (decimal)dataPrice.UnitPrice * dataGb;
            details += $" + ~{dataGb} GB data";
        }

        return new MonthlyCost(monthlyCost, details);
    }
}
