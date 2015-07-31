using CommandLineParsing;
using System;
using System.IO;

namespace GitHubConsole
{
    public static class Config
    {
        private static Configuration global;

        public static Configuration Default => global ?? (global = new Configuration(Path.Combine(GitHub.GlobalStorage, "config")));
    }
}
