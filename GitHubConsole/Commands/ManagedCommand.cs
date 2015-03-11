using System;
using System.Collections.Generic;

namespace GitHubConsole.Commands
{
    public abstract class ManagedCommand : Command
    {
        private Dictionary<string, ArgumentHandler> argumentHandlers;

        public ManagedCommand()
        {
            this.argumentHandlers = new Dictionary<string, ArgumentHandler>();

            foreach (var a in LoadArgumentHandlers())
                this.argumentHandlers.Add(a.Key, a.Handler);
        }

        public virtual bool HandleArgumentFallback(Argument argument)
        {
            return base.HandleArgument(argument);
        }

        protected abstract IEnumerable<ArgumentHandlerPair> LoadArgumentHandlers();

        public sealed override bool HandleArgument(Argument argument)
        {
            if (argumentHandlers.ContainsKey(argument.Key))
                return argumentHandlers[argument.Key](argument);
            else
                return HandleArgumentFallback(argument);
        }
    }
}
