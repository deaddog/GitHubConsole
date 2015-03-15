using GitHubConsole.Commands.Structure;
using GitHubConsole.Messages;
using System;
using System.Collections.Generic;

namespace GitHubConsole.Commands
{
    public class ConfigCommand : ManagedCommand
    {
        private Dictionary<string, string> setValues = new Dictionary<string, string>();
        private List<string> removeKeys = new List<string>();
        private bool clear = false;

        protected override IEnumerable<ArgumentHandlerPair> LoadArgumentHandlers()
        {
            yield return new ArgumentHandlerPair("--set", handleSet);
            yield return new ArgumentHandlerPair("--remove", handleRemove);
            yield return new ArgumentHandlerPair("--clear", handleClear);
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
        private ErrorMessage handleClear(Argument argument)
        {
            if (argument.Count > 0)
                return new ErrorMessage("Values cannot be supplied for the {0} argument.", argument.Key);

            clear = true;
            return ErrorMessage.NoError;
        }
    }
}
