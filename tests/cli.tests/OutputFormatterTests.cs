using Washington.Models;
using Washington.Commands;
using Xunit;

namespace Washington.Tests;

public class OutputFormatterTests
{
    [Fact]
    public void Format_Json_ValidJson()
    {
        var report = CreateSampleReport();
        var output = OutputFormatter.Format(report, "json");

        Assert.Contains("\"grandTotal\"", output);
        Assert.Contains("\"lines\"", output);
    }

    [Fact]
    public void Format_Csv_HasHeader()
    {
        var report = CreateSampleReport();
        var output = OutputFormatter.Format(report, "csv");

        Assert.StartsWith("ResourceName,ResourceType,PricingDetails,MonthlyCost", output);
        Assert.Contains("test-vm", output);
    }

    [Fact]
    public void Format_Table_HasTotal()
    {
        var report = CreateSampleReport();
        var output = OutputFormatter.Format(report, "table");

        Assert.Contains("ESTIMATED MONTHLY TOTAL", output);
        Assert.Contains("$70.08", output);
    }

    [Fact]
    public void Format_Markdown_HasTable()
    {
        var report = CreateSampleReport();
        var output = OutputFormatter.Format(report, "markdown");

        Assert.Contains("| Resource |", output);
        Assert.Contains("**ESTIMATED MONTHLY TOTAL**", output);
    }

    private static CostReport CreateSampleReport()
    {
        return new CostReport(
            Lines: new List<ResourceCostLine>
            {
                new("Microsoft.Compute/virtualMachines", "test-vm",
                    "Standard_D2s_v3 @ $0.0960/hr × 730 hrs", 70.08m)
            },
            GrandTotal: 70.08m,
            Currency: "USD",
            Warnings: new List<string> { "⚠ No pricing mapper for Microsoft.Network/networkInterfaces — skipped" }
        );
    }
}
