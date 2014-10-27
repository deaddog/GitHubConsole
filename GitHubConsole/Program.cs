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

            Command command = getCommand(arguments[0].Key);
            if (command == null)
            {
                Console.WriteLine("Unknown command: {0}.", arguments[0].Key);
                Console.WriteLine("Run with no command to see a list of available commands.");
            }
            else
                command.Run(arguments);
#if DEBUG
            Console.WriteLine("Done.");
            Console.ReadKey(true);
#endif
        }

        private static Command getCommand(string key)
        {
            switch (key)
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
