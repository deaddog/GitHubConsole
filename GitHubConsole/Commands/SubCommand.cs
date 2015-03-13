using System.Collections.Generic;

namespace GitHubConsole.Commands
{
    public class SubCommand : Command
    {
        private Command fallback;
        private Dictionary<string, Command> subcommands;

        private Command active;

        public SubCommand(Command fallback,
            string key1, Command command1)
        {
            this.active = null;
            this.fallback = fallback;

            this.subcommands = new Dictionary<string, Command>();
            this.subcommands.Add(key1, command1);
        }

        public SubCommand(Command fallback,
            string key1, Command command1,
            string key2, Command command2)
            : this(fallback, key1, command1)
        {
            this.subcommands.Add(key2, command2);
        }

        public SubCommand(Command fallback,
            string key1, Command command1,
            string key2, Command command2,
            string key3, Command command3)
            : this(fallback, key1, command1, key2, command2)
        {
            this.subcommands.Add(key3, command3);
        }

        public SubCommand(Command fallback, string key1, Command command1,
            string key2, Command command2,
            string key3, Command command3,
            string key4, Command command4)
            : this(fallback, key1, command1, key2, command2, key3, command3)
        {
            this.subcommands.Add(key4, command4);
        }

        public SubCommand(Command fallback,
            string key1, Command command1,
            string key2, Command command2,
            string key3, Command command3,
            string key4, Command command4,
            string key5, Command command5)
            : this(fallback, key1, command1, key2, command2, key3, command3, key4, command4)
        {
            this.subcommands.Add(key5, command5);
        }

        public SubCommand(Command fallback,
            string key1, Command command1,
            string key2, Command command2,
            string key3, Command command3,
            string key4, Command command4,
            string key5, Command command5,
            string key6, Command command6)
            : this(fallback, key1, command1, key2, command2, key3, command3, key4, command4, key5, command5)
        {
            this.subcommands.Add(key6, command6);
        }

        public override bool HandleArgument(Argument argument)
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

        public override bool ValidateState()
        {
            if (active != null)
                return active.ValidateState();
            else
                return base.ValidateState();
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
