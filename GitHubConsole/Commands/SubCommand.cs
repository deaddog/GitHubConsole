using System.Collections.Generic;

namespace GitHubConsole.Commands
{
    public class SubCommand : Command
    {
        private Command fallback;
        private Dictionary<string, Command> subcommands;

        private Command active;

        public SubCommand(Command fallback, string key1, Command command1)
        {
            this.active = null;
            this.fallback = fallback;

            this.subcommands = new Dictionary<string, Command>();
            this.subcommands.Add(key1, command1);
        }
        public SubCommand(Command fallback, string key1, Command command1, string key2, Command command2)
            : this(fallback, key1, command1)
        {
            this.subcommands.Add(key2, command2);
        }

        public override bool HandleArgument(ArgumentStack.Argument argument)
        {
            if (active == null)
            {
                if (!subcommands.TryGetValue(argument.Key, out active))
                {
                    active = fallback;
                    return active.HandleArgument(argument);
                }
                else
                    return true;
            }
            else
                return active.HandleArgument(argument);
        }

        public override void Execute()
        {
            if (active == null)
                fallback.Execute();
            else
                active.Execute();
        }
    }
}
