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
    public async Task TryHandleVersionRequest_WhenVersionIsRequested_WritesDisplayVersion()
    {
        using var outputWriter = new StringWriter();

        var handled = await Program.TryHandleVersionRequestAsync(
            new[] { "--version" },
            outputWriter,
            (_, _) => Task.FromResult<string?>(null));

        Assert.True(handled);
        Assert.Equal(
            $"{CliVersion.GetDisplayVersion(typeof(Program).Assembly)}{Environment.NewLine}",
            outputWriter.ToString());
    }

    [Fact]
    public async Task TryHandleVersionRequest_WhenUpdateIsAvailable_WritesVersionAndNote()
    {
        using var outputWriter = new StringWriter();

        var handled = await Program.TryHandleVersionRequestAsync(
            new[] { "--version" },
            outputWriter,
            (_, _) => Task.FromResult<string?>("Update available: v9.9.9"));

        Assert.True(handled);
        Assert.Equal(
            $"{CliVersion.GetDisplayVersion(typeof(Program).Assembly)}{Environment.NewLine}Update available: v9.9.9{Environment.NewLine}",
            outputWriter.ToString());
    }

    [Theory]
    [InlineData("0.1.2", false)]
    [InlineData("0.1.3", true)]
    [InlineData("v0.1.3", true)]
    [InlineData("0.1.3-beta.1", true)]
    public void TryParseComparableVersion_CanCompareReleaseTags(string rawVersion, bool isNewer)
    {
        Assert.True(CliVersion.TryParseComparableVersion("0.1.2+1d11c834b5112fb3626e008a7d9fb88051f7e54b", out var currentVersion));
        Assert.True(CliVersion.TryParseComparableVersion(rawVersion, out var candidateVersion));

        Assert.Equal(isNewer, candidateVersion > currentVersion);
    }

    [Fact]
    public async Task TryHandleVersionRequest_WhenVersionIsNotRequested_DoesNotWriteOutput()
    {
        using var outputWriter = new StringWriter();

        var handled = await Program.TryHandleVersionRequestAsync(new[] { "estimate" }, outputWriter);

        Assert.False(handled);
        Assert.Empty(outputWriter.ToString());
    }
}