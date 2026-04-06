using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class VirtualMachineMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Compute/virtualMachines";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var vmSize = GetVmSize(resource);
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Virtual Machines",
                ArmRegionName: region,
                ArmSkuName: vmSize,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var vmSize = GetVmSize(resource);

        // Filter for the best matching compute price
        var price = prices
            .Where(p => !string.IsNullOrEmpty(p.ArmSkuName)
                && p.ArmSkuName.Equals(vmSize, StringComparison.OrdinalIgnoreCase)
                && p.UnitOfMeasure == "1 Hour"
                && p.MeterName != null && !p.MeterName.Contains("Low Priority")
                && p.MeterName != null && !p.MeterName.Contains("Spot"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"{vmSize} - no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
        return new MonthlyCost(monthlyCost, $"{vmSize} @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetVmSize(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("hardwareProfile", out var hwProfile) &&
            hwProfile.ValueKind == JsonValueKind.Object &&
            hwProfile.TryGetProperty("vmSize", out var vmSizeProp) &&
            vmSizeProp.ValueKind == JsonValueKind.String)
        {
            return vmSizeProp.GetString() ?? "Standard_D2s_v3";
        }
        return "Standard_D2s_v3";
    }
}
