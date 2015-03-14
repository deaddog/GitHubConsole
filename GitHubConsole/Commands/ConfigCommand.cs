using GitHubConsole.Commands.Structure;
using System;
using System.Collections.Generic;

namespace GitHubConsole.Commands
{
    public class ConfigCommand : ManagedCommand
    {
        private Dictionary<string, string> setValues = new Dictionary<string, string>();

        protected override IEnumerable<ArgumentHandlerPair> LoadArgumentHandlers()
        {
            yield return new ArgumentHandlerPair("--set", handleSet);
        }

        public override void Execute()
        {
            foreach (var set in setValues)
                Config.Default[set.Key] = set.Value;
        }

        private bool handleSet(Argument argument)
        {
            if(argument.Count != 2)
            {
                ColorConsole.ToConsoleLine("Setting config values requires exactly two arguments:");
                ColorConsole.ToConsoleLine("  github config --set <key> <value>");
                return false;
            }

            setValues[argument[0]] = argument[1];
            return true;
        }
    }
}
