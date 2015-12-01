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

        public static void Reset()
        {
            local = global = null;
            group = null;
        }

        public static Configuration Local => local ?? (local = GitHub.IsGitRepository() ? new Configuration(LocalPath) : null);
        public static Configuration Global => global ?? (global = new Configuration(GlobalPath));

        public static IConfiguration Default
        {
            get
            {
                if (group == null)
                {
                    if (Local == null)
                        group = Global;
                    else
                        group = new ConfigurationGroup(Local, Global);
                }
                return group;
            }
        }
    }
}
