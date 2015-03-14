using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubConsole
{
    public static class Config
    {
        private static string applicationDataPath;

        static Config()
        {
            var roamingPath = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);

            applicationDataPath = Path.Combine(roamingPath, "DeadDog", "GitHubConsole");
            ensurePath(applicationDataPath);
        }

        private static void ensurePath(string path)
        {
            string[] levels = path.Split(Path.DirectorySeparatorChar);
            var drive = new DriveInfo(levels[0]);
            if (drive.DriveType == DriveType.NoRootDirectory ||
                drive.DriveType == DriveType.Unknown)
                throw new ArgumentException("Unable to evaluate path drive; " + levels[0], "path");

            if (!drive.IsReady)
                throw new ArgumentException("Drive '" + levels[0] + "' is not ready.", "path");

            path = levels[0] + "\\";
            for (int i = 1; i < levels.Length; i++)
            {
                path = Path.Combine(path, levels[i]);
                DirectoryInfo dir = new DirectoryInfo(path);
                if (!dir.Exists)
                    dir.Create();
            }
        }
    }
}
