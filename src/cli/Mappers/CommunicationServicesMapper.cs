using Washington.Models;

namespace Washington.Mappers;

public class CommunicationServicesMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Communication/communicationServices";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Communication Services",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var price = prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, "Communication Services - usage-based (chat, SMS, voice)");

        return new MonthlyCost(0, "Communication Services - usage-based (chat, SMS, voice)");
    }
}
