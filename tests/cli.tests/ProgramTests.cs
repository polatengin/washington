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

public class ProgramVersionTests
{
    [Theory]
    [InlineData("0.1.2", "v0.1.2")]
    [InlineData("0.1.2+1d11c834b5112fb3626e008a7d9fb88051f7e54b", "v0.1.2 (1d11c83)")]
    [InlineData("0.1.2-beta.1+1d11c834b5112fb3626e008a7d9fb88051f7e54b", "v0.1.2-beta.1 (1d11c83)")]
    public void Format_WhenInformationalVersionIsProvided_ReturnsDisplayVersion(string rawVersion, string expected)
    {
        var displayVersion = CliVersion.Format(rawVersion);

        Assert.Equal(expected, displayVersion);
    }

    [Fact]
    public void TryHandleVersionRequest_WhenVersionIsRequested_WritesDisplayVersion()
    {
        using var outputWriter = new StringWriter();

        var handled = Program.TryHandleVersionRequest(new[] { "--version" }, outputWriter);

        Assert.True(handled);
        Assert.Equal(
            $"{CliVersion.GetDisplayVersion(typeof(Program).Assembly)}{Environment.NewLine}",
            outputWriter.ToString());
    }

    [Fact]
    public void TryHandleVersionRequest_WhenVersionIsNotRequested_DoesNotWriteOutput()
    {
        using var outputWriter = new StringWriter();

        var handled = Program.TryHandleVersionRequest(new[] { "estimate" }, outputWriter);

        Assert.False(handled);
        Assert.Empty(outputWriter.ToString());
    }
}