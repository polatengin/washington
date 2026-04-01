using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class SignalRMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.SignalRService/signalR";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;
        var skuName = GetSkuName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "SignalR Service",
                ArmRegionName: region,
                SkuName: skuName,
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var units = GetUnitCount(resource);

        if (skuName.Equals("Free_F1", StringComparison.OrdinalIgnoreCase))
            return new MonthlyCost(0, "SignalR Free — no charge");

        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Unit") &&
                p.UnitOfMeasure == "1/Day")
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"SignalR {skuName} {units} unit(s) — no pricing found");

        decimal monthlyCost;
        if (price.UnitOfMeasure == "1/Day")
            monthlyCost = (decimal)price.UnitPrice * 30m * units;
        else if (price.UnitOfMeasure == "1 Hour")
            monthlyCost = (decimal)price.UnitPrice * 730m * units;
        else
            monthlyCost = (decimal)price.UnitPrice * units;

        return new MonthlyCost(monthlyCost, $"SignalR {skuName} {units} unit(s) @ ${price.UnitPrice:F4}/{price.UnitOfMeasure}");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Standard_S1";
        return "Standard_S1";
    }

    private static int GetUnitCount(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var cap) && cap.ValueKind == JsonValueKind.Number)
            return cap.GetInt32();
        return 1;
    }
}
