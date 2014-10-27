using GitHubConsole.Commands;
using System;

namespace GitHubConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            ArgumentStack arguments = new ArgumentStack(args);

            if (arguments.Count == 0)
            {
                Console.WriteLine("GitHubConsole");
                Console.WriteLine();
                Console.WriteLine("Available commands:");
                Console.WriteLine("  cred      Manages the stored GitHub credentials");
                Console.WriteLine("  issues    Lists the GitHub issues associated with the current repo");
                return;
            }

            Command command = getCommand(arguments.Pop());
            if (command == null)
            {
                Console.WriteLine("Unknown command: {0}.", command);
                Console.WriteLine("Run with no command to see a list of available commands.");
            }
            else
            {
                while (arguments.Count > 0)
                    if (!command.HandleArgument(arguments.Pop()))
                        return;

                command.Execute();
                command.Run(arguments);
            }
#if DEBUG
            Console.WriteLine("Done.");
            Console.ReadKey(true);
#endif
        }

        private static Command getCommand(ArgumentStack.Argument arg)
        {
            switch (arg.Key)
            {
                case "cred":
                    return new CredentialCommand();
                case "issues":
                    return new IssuesCommand();
                default:
                    return null;
            }
        }
    }
}
