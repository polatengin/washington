using System.CommandLine;
using System.Text.Json;
using Washington.Services;
using Xunit;

namespace Washington.Tests;

public class ProgramTests
{
    [Fact]
    public async Task InvokeAsync_WhenBicepCompilationFails_ReturnsNonZeroAndWritesError()
    {
        var rootCommand = new RootCommand();
        rootCommand.SetAction((Func<ParseResult, int>)(_ => throw new BicepCompilationException("Bicep compilation failed for 'showcase.bicep'.")));

        using var errorWriter = new StringWriter();

        var exitCode = await global::Program.InvokeAsync(rootCommand, [], errorWriter);

        Assert.Equal(1, exitCode);
        Assert.Equal(
            $"Error: Bicep compilation failed for 'showcase.bicep'.{Environment.NewLine}",
            errorWriter.ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenTemplateJsonIsInvalid_ReturnsNonZeroAndWritesError()
    {
        var rootCommand = new RootCommand();
        rootCommand.SetAction((Func<ParseResult, int>)(_ => throw new JsonException("Bad JSON.")));

        using var errorWriter = new StringWriter();

        var exitCode = await global::Program.InvokeAsync(rootCommand, [], errorWriter);

        Assert.Equal(1, exitCode);
        Assert.Equal(
            $"Error: Failed to parse template JSON. Bad JSON.{Environment.NewLine}",
            errorWriter.ToString());
    }
}