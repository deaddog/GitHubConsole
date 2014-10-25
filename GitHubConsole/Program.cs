using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using CredentialManagement;
using GitHubConsole.Commands;

namespace GitHubConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            ArgumentDictionary arguments = new ArgumentDictionary(args);

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
            Console.ReadKey(true);
#endif
        }

        private static Command getCommand(string key)
        {
            switch (key)
            {
                case "cred":
                    return new CredentialCommand();
                default:
                    return null;
            }
        }
    }
}
