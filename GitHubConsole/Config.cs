using CommandLineParsing;
using System;
using System.IO;

namespace GitHubConsole
{
    public static class Config
    {
        private static Configuration local, global;
        private static IConfiguration group;

        public static string LocalPath
        {
            get { return Path.Combine(GitHub.RepositoryStorage, "config"); }
        }
        public static string GlobalPath
        {
            get { return Path.Combine(GitHub.GlobalStorage, "config"); }
        }

        public static Configuration Local => local ?? (local = new Configuration(LocalPath));
        public static Configuration Global => global ?? (global = new Configuration(GlobalPath));

        public static IConfiguration Default => group ?? (group = new ConfigurationGroup(Local, Global));
    }
}
