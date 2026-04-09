using System.Reflection;

namespace Washington.Services;

internal static class CliVersion
{
    private const int ShortRevisionLength = 7;

    public static string GetDisplayVersion(Assembly assembly)
    {
        var rawVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(rawVersion))
        {
            rawVersion = assembly.GetName().Version?.ToString();
        }

        return Format(rawVersion);
    }

    internal static string Format(string? rawVersion)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
        {
            return "unknown";
        }

        var trimmedVersion = rawVersion.Trim();
        var plusIndex = trimmedVersion.IndexOf('+');
        var versionPart = plusIndex >= 0 ? trimmedVersion[..plusIndex] : trimmedVersion;
        var metadataPart = plusIndex >= 0 ? trimmedVersion[(plusIndex + 1)..] : string.Empty;

        if (!versionPart.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            versionPart = $"v{versionPart}";
        }

        var shortRevision = GetShortRevision(metadataPart);
        return string.IsNullOrEmpty(shortRevision)
            ? versionPart
            : $"{versionPart} ({shortRevision})";
    }

    private static string? GetShortRevision(string metadataPart)
    {
        if (string.IsNullOrWhiteSpace(metadataPart))
        {
            return null;
        }

        var candidate = metadataPart.Split('.', 2, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        if (candidate.Length < ShortRevisionLength)
        {
            return null;
        }

        foreach (var character in candidate)
        {
            if (!Uri.IsHexDigit(character))
            {
                return null;
            }
        }

        return candidate[..ShortRevisionLength];
    }
}