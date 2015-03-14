using GitHubConsole.Messages;
using System;

namespace GitHubConsole.Commands.Structure
{
    public abstract class Command
    {
        public abstract void Execute();

        public virtual ErrorMessage ValidateState()
        {
            return ErrorMessage.NoError;
        }

        public virtual ErrorMessage HandleArgument(Argument argument)
        {
            return new ErrorMessage("Unknown parameter \"{0}\".", argument.Key);
        }
    }
}
