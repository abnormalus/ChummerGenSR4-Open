#nullable enable
using System;
using System.Reflection;

namespace Chummer;

internal static class BuildIdentityChecks
{
    internal static void Run(Action<bool, string> check)
    {
        string revision = new('a', 40);
        check(BuildIdentity.FindSource("0.2.126-nightly+" + revision) == revision &&
            BuildIdentity.FindSource("0.2.126-development+" + revision + ".dirty") == revision,
            "release identity resolves the full source commit for clean and dirty builds");
        check(BuildIdentity.FindSource("0.2.0-development") == null && BuildIdentity.FindSource("0.2.1-nightly+bad") == null,
            "unversioned development builds do not invent a source revision");
        check(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion == BuildIdentity.Version &&
            typeof(Program).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version == BuildIdentity.FileVersion,
            "reported release and file versions come from the actual binary attributes");
        using var about = new frmAbout();
        check(about.Controls.Count > 0 && !string.IsNullOrWhiteSpace(BuildIdentity.ReleaseVersion),
            "About opens with the shared build identity");
    }
}
