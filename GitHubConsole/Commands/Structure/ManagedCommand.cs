using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitHubConsole.Commands.Structure
{
    public abstract class ManagedCommand : Command
    {
        private Dictionary<string, ArgumentHandler> argumentHandlers;

        public ManagedCommand()
        {
            this.argumentHandlers = new Dictionary<string, ArgumentHandler>();

            foreach (var a in LoadArgumentHandlers())
            {
                this.argumentHandlers.Add(a.Key, a.Handler);
                foreach (var alias in a.Aliases)
                    this.argumentHandlers.Add(alias, a.Handler);
            }
        }

        public virtual bool HandleArgumentFallback(Argument argument)
        {
            return base.HandleArgument(argument);
        }

        protected abstract IEnumerable<ArgumentHandlerPair> LoadArgumentHandlers();

        private string[] keysFromAbbreviation(string abbreviation)
        {
            return argumentHandlers.Keys.Where(key => key.StartsWith("--" + abbreviation)).ToArray();
        }

        public sealed override bool HandleArgument(Argument argument)
        {
            if (argumentHandlers.ContainsKey(argument.Key))
                return argumentHandlers[argument.Key](argument);

            if (System.Text.RegularExpressions.Regex.IsMatch(argument.Key, "^-[^-]"))
            {
                var temp = keysFromAbbreviation(argument.Key.Substring(1));
                if (temp.Length == 1)
                    return argumentHandlers[temp[0]](argument);
            }

            return HandleArgumentFallback(argument);
        }
    }
}
