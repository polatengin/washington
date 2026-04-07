using System.Text.Json;
using Washington.Commands;
using Washington.Services;
using Xunit;

namespace Washington.Tests;

public class EstimateCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenBicepCompilationFails_ReturnsNonZeroAndWritesError()
    {
        using var errorWriter = new StringWriter();
        Func<Task<int>> commandAction = () => throw new BicepCompilationException("Bicep compilation failed for 'showcase.bicep'.");

        var exitCode = await EstimateCommand.ExecuteAsync(commandAction, errorWriter);

        Assert.Equal(1, exitCode);
        Assert.Equal(
            $"Error: Bicep compilation failed for 'showcase.bicep'.{Environment.NewLine}",
            errorWriter.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTemplateJsonIsInvalid_ReturnsNonZeroAndWritesError()
    {
        using var errorWriter = new StringWriter();
        Func<Task<int>> commandAction = () => throw new JsonException("Bad JSON.");

        var exitCode = await EstimateCommand.ExecuteAsync(commandAction, errorWriter);

        Assert.Equal(1, exitCode);
        Assert.Equal(
            $"Error: Failed to parse template JSON. Bad JSON.{Environment.NewLine}",
            errorWriter.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnexpectedErrorOccurs_ReturnsNonZeroAndWritesError()
    {
        using var errorWriter = new StringWriter();
        Func<Task<int>> commandAction = () => throw new IOException("Disk is unavailable.");

        var exitCode = await EstimateCommand.ExecuteAsync(commandAction, errorWriter);

        Assert.Equal(1, exitCode);
        Assert.Equal(
            $"Error: Disk is unavailable.{Environment.NewLine}",
            errorWriter.ToString());
    }
}