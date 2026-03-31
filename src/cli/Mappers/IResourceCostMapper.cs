using Washington.Models;

namespace Washington.Mappers;

public interface IResourceCostMapper
{
    string ResourceType { get; }
    bool CanMap(ResourceDescriptor resource);
    List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD");
    MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices);
}
