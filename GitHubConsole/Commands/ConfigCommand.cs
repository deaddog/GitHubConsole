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

        protected override IEnumerable<ArgumentHandlerPair> LoadArgumentHandlers()
        {
            yield return new ArgumentHandlerPair("--set", handleSet);
            yield return new ArgumentHandlerPair("--remove", handleRemove);
            yield return new ArgumentHandlerPair("--clear", NoValuesHandler(() => clear = true));
            yield return new ArgumentHandlerPair("--list", NoValuesHandler(() => list = true));
        }

        public override void Execute()
        {
            if (clear)
                Config.Default.Clear();
            else
                foreach (var key in removeKeys)
                    Config.Default.Remove(key);

            foreach (var set in setValues)
                Config.Default[set.Key] = set.Value;

            if (list)
                foreach (var pair in Config.Default.GetAll())
                    ColorConsole.ToConsoleLine("{0}={1}", pair.Key, pair.Value);
        }

        private ErrorMessage handleSet(Argument argument)
        {
            if (argument.Count != 2)
                return new ErrorMessage(
                    "Setting config values requires exactly two arguments:\n" +
                    "  github config " + argument.Key + " <key> <value>");

            setValues[argument[0]] = argument[1];
            return ErrorMessage.NoError;
        }
        private ErrorMessage handleRemove(Argument argument)
        {
            if (argument.Count == 0)
                return new ErrorMessage(
                    "Removing config keys requires that you specify at least one key to remove:\n" +
                    "  github config --remove <key>\n" +
                    "  github config --remove <key1> <key2> <key3>...");

            for (int i = 0; i < argument.Count; i++)
                removeKeys.Add(argument[i]);

            return ErrorMessage.NoError;
        }
    }
}
