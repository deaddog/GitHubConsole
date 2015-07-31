using CommandLineParsing;
using System;
using System.IO;

namespace GitHubConsole
{
    public static class Config
    {
        private static Configuration local, global;
        private static IConfiguration group;

        public static Configuration Local => local ?? (local = new Configuration(Path.Combine(GitHub.RepositoryStorage, "config")));
        
        public static Configuration Global => global ?? (global = new Configuration(Path.Combine(GitHub.GlobalStorage, "config")));

        public static IConfiguration Default => group ?? (group = new ConfigurationGroup(Local, Global));
    }
}
