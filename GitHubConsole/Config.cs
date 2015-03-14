using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitHubConsole
{
    public static class Config
    {
        private static string applicationDataPath;
        private static string configFilePath
        {
            get { return Path.Combine(applicationDataPath, "config"); }
        }

        private static Dictionary<string, string> values;

        static Config()
        {
            var roamingPath = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);

            applicationDataPath = Path.Combine(roamingPath, "DeadDog", "GitHubConsole");
            ensurePath(applicationDataPath);

            loadConfig();
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

        private static void loadConfig()
        {
            var lines = File.ReadAllLines(configFilePath);

            values = new Dictionary<string, string>();
            foreach (var l in lines)
            {
                var temp = loadKeyValuePair(l);
                if (temp != null)
                    values.Add(temp.Item1, temp.Item2);
            }
        }
        private static Tuple<string, string> loadKeyValuePair(string line)
        {
            line = line.Trim();

            var pair = Regex.Match(line, "(?<key>[a-zA-Z]) *= *(?<value>.*[^ ])");

            if (!pair.Success)
                return null;
            else
                return Tuple.Create(pair.Groups["key"].Value, pair.Groups["value"].Value);
        }

    }
}
