using System;

namespace GitHubConsole.Commands.Structure
{
    public abstract class Command
    {
        public abstract void Execute();

        public virtual bool ValidateState()
        {
            return true;
        }

        public virtual bool HandleArgument(Argument argument)
        {
            Console.WriteLine("Unknown parameter \"{0}\".", argument.Key);
            return false;
        }
    }
}
