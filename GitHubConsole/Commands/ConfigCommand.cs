﻿using CommandLineParsing;
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
                    ColorConsole.WriteLine($"{pair.Key}={pair.Value}");
        }
    }
}
