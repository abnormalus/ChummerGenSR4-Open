#nullable enable
using System.Reflection;
using System.Text.RegularExpressions;

namespace Chummer;

// Embedded in the actual binary, rather than inferred from a moving download tag
// or a sidecar that could be copied from another build. Assembly identity stays
// stable for legacy compatibility; file/informational versions identify releases.
internal static class BuildIdentity
{
    private static readonly Assembly assembly = typeof(Program).Assembly;
    internal static string Version { get; } = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.2.0-development";
    internal static string ReleaseVersion => Version.Split('+')[0];
    internal static string FileVersion => assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "unknown";
    internal static string? SourceCommit { get; } = FindSource(Version);
    internal static bool? Dirty => SourceCommit == null ? null : Version.EndsWith(".dirty", System.StringComparison.Ordinal);
    internal static string Channel => ReleaseVersion.Contains("-nightly") ? "Nightly" : ReleaseVersion.Contains("-stable") ? "Stable" : "Development";
    internal static string? FindSource(string version)
    {
        var match = Regex.Match(version, @"\+([0-9a-f]{40})(?:\.dirty)?$");
        return match.Success ? match.Groups[1].Value : null;
    }
    internal static object Summary => new { version = Version, releaseVersion = ReleaseVersion, fileVersion = FileVersion,
        sourceCommit = SourceCommit, channel = Channel, dirty = Dirty, assemblyVersion = assembly.GetName().Version?.ToString(),
        build = typeof(Program).Module.ModuleVersionId.ToString() };
}
