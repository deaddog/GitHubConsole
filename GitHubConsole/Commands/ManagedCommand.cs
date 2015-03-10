using System;
using System.Collections.Generic;

namespace GitHubConsole.Commands
{
    public abstract class ManagedCommand : Command
    {
        private Dictionary<string, Func<ArgumentStack.Argument, bool>> argumentHandlers;

        public ManagedCommand()
        {
            this.argumentHandlers = new Dictionary<string, Func<ArgumentStack.Argument, bool>>();

            foreach (var a in LoadArgumentHandlers())
                this.argumentHandlers.Add(a.Item1, a.Item2);
        }

        public virtual bool HandleArgumentFallback(ArgumentStack.Argument argument)
        {
            return base.HandleArgument(argument);
        }

        protected abstract IEnumerable<Tuple<string, Func<ArgumentStack.Argument, bool>>> LoadArgumentHandlers();

        public sealed override bool HandleArgument(ArgumentStack.Argument argument)
        {
            if (argumentHandlers.ContainsKey(argument.Key))
                return argumentHandlers[argument.Key](argument);
            else
                return HandleArgumentFallback(argument);
        }
    }
}
