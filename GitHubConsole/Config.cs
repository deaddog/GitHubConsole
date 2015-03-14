using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitHubConsole
{
    public class Config
    {
        #region Static directory ensuring

        private static string applicationDataPath;
        private static string configFilePath
        {
            get { return Path.Combine(applicationDataPath, "config"); }
        }

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

        #endregion

        private static Tuple<string, string> loadKeyValuePair(string line)
        {
            line = line.Trim();

            var pair = Regex.Match(line, "(?<key>[a-zA-Z0-9]+) *= *(?<value>.*[^ ])");

            if (!pair.Success)
                return null;
            else
                return Tuple.Create(pair.Groups["key"].Value, pair.Groups["value"].Value);
        }

        private static Config config;

        public static Config Default
        {
            get
            {
                if (config == null)
                    config = new Config();
                return config;
            }
        }

        private Dictionary<string, string> values;

        private Config()
        {
            values = new Dictionary<string, string>();
            if (!File.Exists(configFilePath))
                return;

            var lines = File.ReadAllLines(configFilePath, Encoding.UTF8);
            foreach (var l in lines)
            {
                var temp = loadKeyValuePair(l);
                if (temp != null)
                    values.Add(temp.Item1, temp.Item2);
            }
        }
    }
}
