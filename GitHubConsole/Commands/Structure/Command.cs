using GitHubConsole.Messages;
using System;

namespace GitHubConsole.Commands.Structure
{
    public abstract class Command
    {
        public abstract void Execute();

        public virtual Message ValidateState()
        {
            return null;
        }

        public virtual Message HandleArgument(Argument argument)
        {
            return new Message("Unknown parameter \"{0}\".", argument.Key);
        }
    }
}
