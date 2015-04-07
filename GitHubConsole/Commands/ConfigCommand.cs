using CommandLineParsing;
using System;
using System.Collections.Generic;

namespace GitHubConsole.Commands
{
    public class ConfigCommand : Command
    {
        private Dictionary<string, string> setValues = new Dictionary<string, string>();
        private List<string> removeKeys = new List<string>();

        private readonly Parameter<string[]> set;
        private readonly Parameter<string[]> remove;

        private readonly FlagParameter clear;
        private readonly FlagParameter list;

        public ConfigCommand()
        {
            set.Validate(x => x.Length == 2,
                "Setting config values requires exactly two arguments:\n" +
                "  github config " + set.Name + " <key> <value>");
            set.Callback += () => setValues.Add(set.Value[0], set.Value[1]);

            remove.Validate(x => x.Length > 0,
                "Removing config keys requires that you specify at least one key to remove:\n" +
                "  github config " + remove.Name + " <key>\n" +
                "  github config " + remove.Name + " <key1> <key2> <key3>...");
            remove.Callback += () => removeKeys.AddRange(remove.Value);
        }

        protected override void Execute()
        {
            if (clear.IsSet)
                Config.Default.Clear();
            else
                foreach (var key in removeKeys)
                    Config.Default.Remove(key);

            foreach (var set in setValues)
                Config.Default[set.Key] = set.Value;

            if (list.IsSet)
                foreach (var pair in Config.Default.GetAll())
                    ColorConsole.WriteLine("{0}={1}", pair.Key, pair.Value);
        }
    }
}
