using CommandLineParsing;
using System;
using System.IO;

namespace GitHubConsole
{
    public static class Config
    {
        private static Configuration global;

        public static Configuration Default
        {
            get
            {
                if (global == null)
                {
                    var roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);

                    global = new CommandLineParsing.Configuration(Path.Combine(roamingPath, "DeadDog", "GitHubConsole", "config"));
                }
                return global;
            }
        }
    }
}
