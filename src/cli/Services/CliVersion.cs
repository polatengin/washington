using System.Reflection;

namespace Washington.Services;

internal static class CliVersion
{
    private const int ShortRevisionLength = 7;

    public static string GetDisplayVersion(Assembly assembly)
    {
        return Format(GetRawVersion(assembly));
    }

    internal static string Format(string? rawVersion)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
        {
            return "unknown";
        }

        var versionPart = NormalizeTag(rawVersion);
        var metadataPart = GetMetadataPart(rawVersion);

        var shortRevision = GetShortRevision(metadataPart);
        return string.IsNullOrEmpty(shortRevision)
            ? versionPart
            : $"{versionPart} ({shortRevision})";
    }

    internal static string? GetComparableVersion(Assembly assembly)
    {
        return GetVersionPart(GetRawVersion(assembly));
    }

    internal static string NormalizeTag(string? rawVersion)
    {
        var versionPart = GetVersionPart(rawVersion);
        if (string.IsNullOrWhiteSpace(versionPart))
        {
            return "unknown";
        }

        return versionPart.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? versionPart
            : $"v{versionPart}";
    }

    internal static bool TryParseComparableVersion(string? rawVersion, out Version? version)
    {
        version = null;

        var versionPart = GetVersionPart(rawVersion);
        if (string.IsNullOrWhiteSpace(versionPart))
        {
            return false;
        }

        versionPart = versionPart.TrimStart('v', 'V');

        var prereleaseIndex = versionPart.IndexOf('-');
        if (prereleaseIndex >= 0)
        {
            versionPart = versionPart[..prereleaseIndex];
        }

        return Version.TryParse(versionPart, out version);
    }

    private static string? GetRawVersion(Assembly assembly)
    {
        var rawVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(rawVersion))
        {
            rawVersion = assembly.GetName().Version?.ToString();
        }

        return rawVersion;
    }

    private static string? GetVersionPart(string? rawVersion)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
        {
            return null;
        }

        var trimmedVersion = rawVersion.Trim();
        var plusIndex = trimmedVersion.IndexOf('+');
        return plusIndex >= 0 ? trimmedVersion[..plusIndex] : trimmedVersion;
    }

    private static string GetMetadataPart(string rawVersion)
    {
        var plusIndex = rawVersion.IndexOf('+');
        return plusIndex >= 0 ? rawVersion[(plusIndex + 1)..] : string.Empty;
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