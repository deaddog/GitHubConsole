using System;
using System.Collections.Generic;

namespace GitHubConsole.Commands
{
    public abstract class ManagedCommand : Command
    {
        private Dictionary<string, Func<Argument, bool>> argumentHandlers;

        public ManagedCommand()
        {
            this.argumentHandlers = new Dictionary<string, Func<Argument, bool>>();

            foreach (var a in LoadArgumentHandlers())
                this.argumentHandlers.Add(a.Item1, a.Item2);
        }

        public virtual bool HandleArgumentFallback(Argument argument)
        {
            return base.HandleArgument(argument);
        }

        protected abstract IEnumerable<Tuple<string, Func<Argument, bool>>> LoadArgumentHandlers();

        public sealed override bool HandleArgument(Argument argument)
        {
            if (argumentHandlers.ContainsKey(argument.Key))
                return argumentHandlers[argument.Key](argument);
            else
                return HandleArgumentFallback(argument);
        }
    }
}
