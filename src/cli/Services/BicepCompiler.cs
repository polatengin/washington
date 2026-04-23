using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using Bicep.RpcClient;
using Bicep.RpcClient.Models;

namespace Washington.Services;

public class BicepCompiler
{
    private const string DefaultBicepVersion = "0.42.1";
    private static readonly HttpClient HttpClient = new();
    private static readonly BicepClientFactory ClientFactory = new(HttpClient);

    public virtual async Task<string> CompileBicepToArm(string bicepFilePath)
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var tempBicepPath = CopyWorkingFiles(bicepFilePath, tempDir);

            using var client = await CreateClientAsync();
            var response = await client.Compile(new CompileRequest(tempBicepPath));

            return GetCompilationOutput(
                response.Success,
                response.Contents,
                response.Diagnostics,
                bicepFilePath,
                "compilation");
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    public virtual async Task<string> CompileBicepParamsToArm(string bicepParamFilePath)
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var tempParamPath = CopyWorkingFiles(bicepParamFilePath, tempDir);

            using var client = await CreateClientAsync();
            var response = await client.CompileParams(new CompileParamsRequest(tempParamPath, new Dictionary<string, JsonNode>()));

            return GetCompilationOutput(
                response.Success,
                response.Parameters,
                response.Diagnostics,
                bicepParamFilePath,
                "params compilation");
        }
        finally
        {
            DeleteTempDirectory(tempDir);
        }
    }

    protected virtual Task<IBicepClient> CreateClientAsync(CancellationToken cancellationToken = default)
        => ClientFactory.Initialize(CreateClientConfiguration(), cancellationToken);

    private static BicepClientConfiguration CreateClientConfiguration()
    {
        var cliFromPath = FindCliOnPath();
        if (!string.IsNullOrWhiteSpace(cliFromPath))
        {
            return new BicepClientConfiguration
            {
                ExistingCliPath = cliFromPath,
                ConnectionMode = BicepConnectionMode.Stdio
            };
        }

        return new BicepClientConfiguration
        {
            BicepVersion = DefaultBicepVersion,
            ConnectionMode = BicepConnectionMode.Stdio
        };
    }

    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "bce", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);

        File.WriteAllText(
            Path.Combine(tempDir, "bicepconfig.json"),
            """{ "analyzers": { "core": { "enabled": false } } }""");

        return tempDir;
    }

    private static string CopyWorkingFiles(string sourcePath, string tempDir)
    {
        var sourceDir = Path.GetDirectoryName(sourcePath);
        if (!string.IsNullOrWhiteSpace(sourceDir) && Directory.Exists(sourceDir))
        {
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destinationPath = Path.Combine(tempDir, Path.GetFileName(file));
                if (!File.Exists(destinationPath))
                {
                    TryCopySupportingFile(file, destinationPath);
                }
            }
        }

        var tempSourcePath = Path.Combine(tempDir, Path.GetFileName(sourcePath));
        File.Copy(sourcePath, tempSourcePath, overwrite: true);
        return tempSourcePath;
    }

    private static void TryCopySupportingFile(string sourcePath, string destinationPath)
    {
        try
        {
            File.Copy(sourcePath, destinationPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string GetCompilationOutput(
        bool success,
        string? contents,
        IEnumerable<DiagnosticDefinition>? diagnostics,
        string sourcePath,
        string operationName)
    {
        if (success && !string.IsNullOrWhiteSpace(contents))
        {
            return contents;
        }

        throw CreateCompilationException(success, diagnostics, sourcePath, operationName);
    }

    private static BicepCompilationException CreateCompilationException(
        bool success,
        IEnumerable<DiagnosticDefinition>? diagnostics,
        string sourcePath,
        string operationName)
    {
        var diagnosticList = diagnostics?.ToList() ?? [];

        var errorDiagnostics = diagnosticList
            .Where(diagnostic => diagnostic.Level.Equals("Error", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var relevantDiagnostics = errorDiagnostics.Count > 0 ? errorDiagnostics : diagnosticList;

        var message = new StringBuilder();
        message.Append($"Bicep {operationName} failed for '{sourcePath}'.");

        if (relevantDiagnostics.Count == 0)
        {
            message.Append(success
                ? " The compiler did not return any template contents."
                : " No diagnostics were returned.");
            return new BicepCompilationException(message.ToString());
        }

        foreach (var diagnostic in relevantDiagnostics)
        {
            var start = diagnostic.Range.Start;
            message.AppendLine();
            message.Append($"- {diagnostic.Level} {diagnostic.Code} ({start.Line + 1},{start.Char + 1}): {diagnostic.Message}");
        }

        return new BicepCompilationException(message.ToString());
    }

    private static string? FindCliOnPath()
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        var executableNames = OperatingSystem.IsWindows()
            ? new[] { "bicep.exe", "bicep" }
            : new[] { "bicep" };

        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var executableName in executableNames)
            {
                var candidate = Path.Combine(directory, executableName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static void DeleteTempDirectory(string tempDir)
    {
        try
        {
            Directory.Delete(tempDir, recursive: true);
        }
        catch
        {
        }
    }
}
