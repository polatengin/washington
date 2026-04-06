using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class CognitiveServicesMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.CognitiveServices/accounts";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var kind = GetKind(resource);

        var serviceName = kind switch
        {
            "OpenAI" => "Azure OpenAI Service",
            "TextAnalytics" => "Cognitive Services - Text Analytics",
            "ComputerVision" => "Cognitive Services - Computer Vision",
            "SpeechServices" => "Cognitive Services - Speech",
            "FormRecognizer" => "Cognitive Services - Form Recognizer",
            _ => "Cognitive Services"
        };

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: serviceName,
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var kind = GetKind(resource);
        var sku = GetSkuName(resource);

        // For OpenAI, look for token-based pricing
        if (kind.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            var tokenPrice = prices
                .Where(p => p.MeterName != null &&
                    (p.MeterName.Contains("Token") || p.MeterName.Contains("Unit")))
                .OrderBy(p => p.UnitPrice)
                .FirstOrDefault();

            if (tokenPrice != null)
            {
                // Estimate 1M tokens/month
                var estimatedUnits = 1_000m;
                var monthlyCost = (decimal)tokenPrice.UnitPrice * estimatedUnits;
                return new MonthlyCost(monthlyCost, $"Azure OpenAI ({sku}) ~1M tokens @ ${tokenPrice.UnitPrice:F4}/1K tokens");
            }
        }

        // Generic cognitive services pricing
        var price = prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Cognitive Services ({kind}/{sku}) - usage-based");

        // Estimate 1000 transactions/month
        var txnCount = 1_000m;
        var cost = (decimal)price.UnitPrice * txnCount;
        return new MonthlyCost(cost, $"Cognitive Services ({kind}/{sku}) ~{txnCount:N0} txns @ ${price.UnitPrice:F4}/txn");
    }

    private static string GetKind(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("_kind", out var kind) && kind.ValueKind == JsonValueKind.String)
            return kind.GetString() ?? "CognitiveServices";
        return "CognitiveServices";
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "S0";
        return "S0";
    }
}
