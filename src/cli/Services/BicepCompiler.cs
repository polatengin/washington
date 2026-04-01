using Washington.Models;

namespace Washington.Services;

public class BicepCompiler
{
    public async Task<string> CompileBicepToArm(string bicepFilePath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "bce", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a minimal bicepconfig to suppress analyzer warnings
            File.WriteAllText(Path.Combine(tempDir, "bicepconfig.json"),
                """{ "analyzers": { "core": { "enabled": false } } }""");

            var fileName = Path.GetFileName(bicepFilePath);
            var tempBicepPath = Path.Combine(tempDir, fileName);
            File.Copy(bicepFilePath, tempBicepPath, overwrite: true);

            // Copy any referenced files in the same directory
            var sourceDir = Path.GetDirectoryName(bicepFilePath);
            if (sourceDir != null)
            {
                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    var destFile = Path.Combine(tempDir, Path.GetFileName(file));
                    if (!File.Exists(destFile))
                        File.Copy(file, destFile);
                }
            }

            var outputFile = Path.Combine(tempDir, Path.GetRandomFileName() + ".json");

            await Bicep.Cli.Program.Main(["build", tempBicepPath, "--outfile", outputFile]);

            if (!File.Exists(outputFile))
                throw new InvalidOperationException($"Bicep compilation failed for '{bicepFilePath}'. No output file was produced.");

            return await File.ReadAllTextAsync(outputFile);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    public async Task<string> CompileBicepParamsToArm(string bicepParamFilePath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "bce", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "bicepconfig.json"),
                """{ "analyzers": { "core": { "enabled": false } } }""");

            // Copy the params file and any related files
            var sourceDir = Path.GetDirectoryName(bicepParamFilePath);
            if (sourceDir != null)
            {
                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    var destFile = Path.Combine(tempDir, Path.GetFileName(file));
                    if (!File.Exists(destFile))
                        File.Copy(file, destFile);
                }
            }

            var tempParamPath = Path.Combine(tempDir, Path.GetFileName(bicepParamFilePath));
            var outputFile = Path.Combine(tempDir, Path.GetRandomFileName() + ".json");

            await Bicep.Cli.Program.Main(["build-params", tempParamPath, "--outfile", outputFile]);

            if (!File.Exists(outputFile))
                throw new InvalidOperationException($"Bicep params compilation failed for '{bicepParamFilePath}'.");

            return await File.ReadAllTextAsync(outputFile);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
