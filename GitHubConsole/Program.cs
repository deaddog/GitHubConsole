using GitHubConsole.Commands;
using System;

namespace GitHubConsole
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
        start:
            Console.Write("Input command (or \"exit\" to quit):");

            string input = Console.ReadLine();
            if (input.StartsWith("github"))
                input = input.Substring(6);
            input = input.Trim();

            if (input == "exit")
                return;

            ArgumentStack arguments = new ArgumentStack(input.Split(' '));
#else
            ArgumentStack arguments = new ArgumentStack(args);
#endif

            if (arguments.Count == 0)
            {
                Console.WriteLine("GitHubConsole");
                Console.WriteLine();
                Console.WriteLine("Available commands:");
                Console.WriteLine("  cred      Manages the stored GitHub credentials");
                Console.WriteLine("  issues    Lists the GitHub issues associated with the current repo");
                return;
            }

            Command command = getCommand();

            bool argumentsValid = true;

            while (arguments.Count > 0)
                if (!command.HandleArgument(arguments.Pop()))
                {
                    argumentsValid = false;
                    break;
                }

            if (argumentsValid && command.ValidateState())
                command.Execute();
#if DEBUG
            goto start;
#endif
        }

        private class emptyCommand : Command
        {
            public override void Execute()
            {
            }
        }

        private static Command getCommand()
        {
            return new SubCommand(new emptyCommand(),
                "cred", new CredentialCommand(),
                "issues", new SubCommand(new IssuesCommand(),
                    "take", new IssuesAssigner(true),
                    "drop", new IssuesAssigner(false),
                    "label", new IssuesLabeler()));
        }
    }
}
