using System.Text;
using System.Text.Json;
using Washington.Models;

namespace Washington.Commands;

public static class OutputFormatter
{
    public static string Format(CostReport report, string format, string? filePath = null)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => FormatJson(report),
            "csv" => FormatCsv(report),
            "markdown" => FormatMarkdown(report, filePath),
            _ => FormatTable(report, filePath),
        };
    }

    private static string FormatJson(CostReport report)
    {
        return JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string FormatTable(CostReport report, string? filePath)
    {
        var sb = new StringBuilder();

        sb.AppendLine();
        sb.AppendLine($"Bicep Cost Estimate{(filePath != null ? $": {Path.GetFileName(filePath)}" : "")}");
        sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd}");
        sb.AppendLine();

        if (report.Lines.Count == 0 && report.Warnings.Count == 0)
        {
            sb.AppendLine("No resources found.");
            return sb.ToString();
        }

        // Calculate column widths
        var nameWidth = Math.Max(16, report.Lines.Max(l => l.ResourceName.Length) + 2);
        var typeWidth = Math.Max(20, report.Lines.Max(l => l.ResourceType.Length) + 2);
        var detailsWidth = Math.Max(20, report.Lines.Max(l => l.PricingDetails.Length) + 2);
        var costWidth = 14;

        var totalWidth = nameWidth + typeWidth + detailsWidth + costWidth;
        var separator = new string('─', totalWidth);

        sb.AppendLine($"{"Resource".PadRight(nameWidth)}{"Type".PadRight(typeWidth)}{"Details".PadRight(detailsWidth)}{"Monthly Cost".PadLeft(costWidth)}");
        sb.AppendLine(separator);

        foreach (var line in report.Lines)
        {
            var cost = line.MonthlyCost > 0 ? $"${line.MonthlyCost:N2}" : "—";
            sb.AppendLine($"{line.ResourceName.PadRight(nameWidth)}{line.ResourceType.PadRight(typeWidth)}{line.PricingDetails.PadRight(detailsWidth)}{cost.PadLeft(costWidth)}");
        }

        sb.AppendLine(separator);
        sb.AppendLine($"{"".PadRight(nameWidth + typeWidth)}{"ESTIMATED MONTHLY TOTAL".PadRight(detailsWidth)}{$"${report.GrandTotal:N2}".PadLeft(costWidth)}");
        sb.AppendLine();

        foreach (var warning in report.Warnings)
        {
            sb.AppendLine(warning);
        }

        return sb.ToString();
    }

    private static string FormatCsv(CostReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ResourceName,ResourceType,PricingDetails,MonthlyCost");

        foreach (var line in report.Lines)
        {
            sb.AppendLine($"\"{line.ResourceName}\",\"{line.ResourceType}\",\"{line.PricingDetails}\",{line.MonthlyCost:F2}");
        }

        sb.AppendLine($"\"TOTAL\",\"\",\"\",{report.GrandTotal:F2}");
        return sb.ToString();
    }

    private static string FormatMarkdown(CostReport report, string? filePath)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"## Bicep Cost Estimate{(filePath != null ? $": {Path.GetFileName(filePath)}" : "")}");
        sb.AppendLine();
        sb.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("| Resource | Type | Details | Monthly Cost |");
        sb.AppendLine("|----------|------|---------|-------------:|");

        foreach (var line in report.Lines)
        {
            var cost = line.MonthlyCost > 0 ? $"${line.MonthlyCost:N2}" : "—";
            sb.AppendLine($"| {line.ResourceName} | {line.ResourceType} | {line.PricingDetails} | {cost} |");
        }

        sb.AppendLine($"| | | **ESTIMATED MONTHLY TOTAL** | **${report.GrandTotal:N2}** |");
        sb.AppendLine();

        if (report.Warnings.Count > 0)
        {
            sb.AppendLine("### Warnings");
            foreach (var warning in report.Warnings)
            {
                sb.AppendLine($"- {warning}");
            }
        }

        return sb.ToString();
    }
}
