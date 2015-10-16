using CommandLineParsing;
using System.Linq;
using System.Collections.Generic;

namespace GitHubConsole.Commands
{
    [Description("Sets configuration for the application")]
    public class ConfigCommand : Command
    {
        private Dictionary<string, string> setValues = new Dictionary<string, string>();
        private List<string> removeKeys = new List<string>();

        [Description("Sets a key value combination in the configuration file.")]
        private readonly Parameter<string[]> set = null;
        [Description("Removes a key from the configuration file.")]
        private readonly Parameter<string[]> remove = null;

        [Description("Opens the configuration file in an editor.")]
        private readonly FlagParameter edit = null;
        [Description("Removes all keys from the configuration file.")]
        private readonly FlagParameter clear = null;
        [Description("Lists all key value combinations in the configuration file.")]
        private readonly FlagParameter list = null;

        [Name("--format", "-f"), Description("Allows for describing an output format when listing configuration files.")]
        private readonly Parameter<string> format = null;

        [Description("Commands will apply to the global configuration file.")]
        private readonly FlagParameter global = null;

        [Description("Commands will apply to both the local and the global configuration file.")]
        private readonly FlagParameter all = null;

        public ConfigCommand()
        {
            format.SetDefault(Config.Default["config.format"] ?? "$key=$value");

            set.Validator.Add(x => x.Length == 2,
                "Setting config values requires exactly two arguments:\n" +
                $"  [Example:github config {set.Name} <key> <value>]");
            set.Validator.Add(x => Configuration.IsKeyValid(x[0]), "Configuration keys must start with a letter and be letter and numbers only.\n\n" +
                "   <key>.<subkey> = <value>\n" +
                "   [Example:abc.def = <value>]\n" +
                "   <key>.<subkey>.<subkey> = <value>\n" +
                "   [Example:a12.b34.c56 = <value>]");
            set.Callback += () => setValues.Add(set.Value[0], set.Value[1]);

            remove.Validator.Add(x => x.Length > 0,
                "Removing config keys requires that you specify at least one key to remove:\n" +
                $"  [Example:github config {remove.Name} <key>]\n" +
                $"  [Example:github config {remove.Name} <key1> <key2> <key3>...]");
            remove.Callback += () => removeKeys.AddRange(remove.Value);

            this.Validator.AddIfFirstNotRest(all, clear, set, remove, edit);
            this.Validator.AddIfFirstNotRest(edit, clear, set, remove);
            this.Validator.AddOnlyOne(global, all);
        }

        protected override void Execute()
        {
            Configuration conf = global.IsSet ? Config.Global : Config.Local;
            IConfiguration iconf = all.IsSet ? Config.Default : conf;

            bool shouldList = list.IsSet;
            if (!(set.IsSet || remove.IsSet || clear.IsSet || edit.IsSet))
                shouldList = true;

            if (clear.IsSet)
                conf.Clear();
            else
                foreach (var key in removeKeys)
                    conf.Remove(key);

            foreach (var set in setValues)
                conf[set.Key] = set.Value;

            if (edit.IsSet)
            {
                string path = global.IsSet ? Config.GlobalPath : Config.LocalPath;
                if (!System.IO.File.Exists(path))
                    System.IO.File.WriteAllText(path, "");

                FileEditing.OpenAndEdit(path, Config.Default["config.editor"]);

                Config.Reset();
            }

            if (shouldList)
            {
                var confList = iconf.GetAll().ToArray();

                Formatter formatter = new Formatter();
                formatter.Variables.Add<KeyValuePair<string, string>>("key", x => x.Key, null, null);
                formatter.Variables.Add<KeyValuePair<string, string>>("value", x => ColorConsole.EscapeColor(x.Value), null, null);

                formatter.WriteLines(confList, format.Value);

                if (confList.Length == 0 && all.IsSet)
                    ColorConsole.WriteLine("[DarkCyan:Both local and global configuration files are empty.");
                else if (confList.Length == 0)
                    ColorConsole.WriteLine($"[DarkCyan:{(global.IsSet ? "Global" : "Local")} configuration file is empty.]");
            }
        }
    }
}
