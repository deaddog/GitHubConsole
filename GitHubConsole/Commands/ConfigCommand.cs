using CommandLineParsing;
using System;
using System.Collections.Generic;

namespace GitHubConsole.Commands
{
    public class ConfigCommand : Command
    {
        private Dictionary<string, string> setValues = new Dictionary<string, string>();
        private List<string> removeKeys = new List<string>();

        [Description("Sets a key value combination in the configuration file.")]
        private readonly Parameter<string[]> set = null;
        [Description("Removes a key from the configuration file.")]
        private readonly Parameter<string[]> remove = null;

        [Description("Removes all keys from the configuration file.")]
        private readonly FlagParameter clear = null;
        [Description("Lists all key value combinations in the configuration file.")]
        private readonly FlagParameter list = null;

        [Description("Commands will apply to the global configuration file.")]
        private readonly FlagParameter global = null;

        [Description("Commands will apply to both the local and the global configuration file.")]
        private readonly FlagParameter all = null;

        public ConfigCommand()
        {
            set.Validator.Add(x => x.Length == 2,
                "Setting config values requires exactly two arguments:\n" +
                $"  [Example:github config {set.Name} <key> <value>]");
            set.Callback += () => setValues.Add(set.Value[0], set.Value[1]);

            remove.Validator.Add(x => x.Length > 0,
                "Removing config keys requires that you specify at least one key to remove:\n" +
                $"  [Example:github config {remove.Name} <key>]\n" +
                $"  [Example:github config {remove.Name} <key1> <key2> <key3>...]");
            remove.Callback += () => removeKeys.AddRange(remove.Value);

            this.Validator.AddIfFirstNotRest(all, clear, set, remove);
            this.Validator.AddOnlyOne(global, all);
        }

        protected override void Execute()
        {
            Configuration conf = global.IsSet ? Config.Global : Config.Local;
            IConfiguration iconf = all.IsSet ? Config.Default : conf;

            if (clear.IsSet)
                conf.Clear();
            else
                foreach (var key in removeKeys)
                    conf.Remove(key);

            foreach (var set in setValues)
                conf[set.Key] = set.Value;

            if (list.IsSet)
                foreach (var pair in iconf.GetAll())
                    ColorConsole.WriteLine($"{pair.Key}={pair.Value}");
        }
    }
}
