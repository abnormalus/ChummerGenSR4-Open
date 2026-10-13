using System;
using System.IO;
using System.Linq;


namespace Chummer
{
    internal static class MacPaths
    {
        internal static string TestUserDirectory;
        public static string UserDirectory => TestUserDirectory ?? (System.OperatingSystem.IsMacOS()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "ChummerGenSR4Mac")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChummerGenSR4Mac"));
        public static string CustomDataDirectory => Path.Combine(UserDirectory, "data");
        public static string[] CustomDataFiles(string pattern) => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data"), pattern)
            .Concat(Directory.GetFiles(CustomDataDirectory, pattern)).GroupBy(Path.GetFileName).Select(x => x.Last()).ToArray();
        public static string SettingsDirectory => Path.Combine(UserDirectory, "settings");
        public static string BackupsDirectory => Path.Combine(UserDirectory, "saves", "backup");
        public static string MugshotsDirectory => Path.Combine(UserDirectory, "mugshots");

        public static void Initialize()
        {
            Directory.CreateDirectory(SettingsDirectory);
            Directory.CreateDirectory(CustomDataDirectory);
            Directory.CreateDirectory(MugshotsDirectory);
            Directory.CreateDirectory(BackupsDirectory);
            string strDefaults = Path.Combine(AppContext.BaseDirectory, "settings");
            foreach (string strFile in Directory.GetFiles(strDefaults, "*.xml"))
            {
                string strDestination = Path.Combine(SettingsDirectory, Path.GetFileName(strFile));
                if (!File.Exists(strDestination)) File.Copy(strFile, strDestination);
            }
            // Finder launches applications with an arbitrary working directory.
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        }
    }
}
