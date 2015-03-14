using GitHubConsole.Commands;
using GitHubConsole.Commands.Structure;
using GitHubConsole.Messages;
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

            var matches = System.Text.RegularExpressions.Regex.Matches(input, "[^ \"]+|\"[^\"]+\"");
            string[] inputArr = new string[matches.Count];
            for (int i = 0; i < inputArr.Length; i++)
            {
                inputArr[i] = matches[i].Value;
                if (inputArr[i][0] == '\"' && inputArr[i][inputArr[i].Length - 1] == '\"')
                    inputArr[i] = inputArr[i].Substring(1, inputArr[i].Length - 2);
            }

            ArgumentStack arguments = new ArgumentStack(inputArr);
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

            ErrorMessage message = null;
            Command command = getCommand();

            while (arguments.Count > 0)
            {
                message = command.HandleArgument(arguments.Pop());
                if (message != null)
                    break;
            }


            if (message == null)
                message = command.ValidateState();

            if (message != null)
                ColorConsole.ToConsoleLine(message.GetMessage());
            else
            {
                try { command.Execute(); }
                catch (AggregateException aggex)
                {
                    if (aggex.InnerExceptions.Count == 1 && aggex.InnerException is Octokit.AuthorizationException)
                    {
                        Octokit.AuthorizationException credex = aggex.InnerException as Octokit.AuthorizationException;

                        ColorConsole.ToConsoleLine("GitHub responded to your request with an authentification error:");
                        ColorConsole.ToConsoleLine("[[:Red:[{1}] {0}]]", credex.Message, credex.StatusCode);
                        ColorConsole.ToConsoleLine("Run [[:Yellow:github config --set authtoken <token>]] to set authentification token.");
                    }
                    else
                        throw aggex;
                }
            }
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
                "config", new ConfigCommand(),
                "issues", new SubCommand(new IssuesCommand(),
                    "create", new IssuesCreateCommand(),
                    "take", new IssuesAssigner(true),
                    "drop", new IssuesAssigner(false),
                    "label", new IssuesLabeler()));
        }
    }
}
