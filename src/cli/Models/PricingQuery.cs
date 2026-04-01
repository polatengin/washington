namespace Washington.Models;

public record PricingQuery(
    string ServiceName,
    string ArmRegionName,
    string? ArmSkuName = null,
    string? SkuName = null,
    string? MeterName = null,
    string? ProductName = null,
    string PriceType = "Consumption"
)
{
    public string ToFilterString()
    {
        var filters = new List<string>
        {
            $"priceType eq '{PriceType}'"
        };

        if (!string.IsNullOrEmpty(ServiceName))
            filters.Add($"serviceName eq '{ServiceName}'");

        if (!string.IsNullOrEmpty(ArmRegionName))
            filters.Add($"armRegionName eq '{ArmRegionName}'");

        if (!string.IsNullOrEmpty(ArmSkuName))
            filters.Add($"armSkuName eq '{ArmSkuName}'");

        if (!string.IsNullOrEmpty(SkuName))
            filters.Add($"skuName eq '{SkuName}'");

        if (!string.IsNullOrEmpty(MeterName))
            filters.Add($"meterName eq '{MeterName}'");

        if (!string.IsNullOrEmpty(ProductName))
            filters.Add($"productName eq '{ProductName}'");

        return string.Join(" and ", filters);
    }

    public string ToCacheKey() => ToFilterString().ToLowerInvariant();
}
