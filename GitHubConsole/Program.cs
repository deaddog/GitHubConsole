using CommandLineParsing;
using GitHubConsole.Commands;
using System;

namespace GitHubConsole
{
    class Program
    {
        private const string HELP = "help";

        static void Main(string[] args)
        {
            ColorConsole.Colors["Example"] = ConsoleColor.Yellow;

            ColorConsole.Colors["Issue_Closed"] = ConsoleColor.DarkRed;
            ColorConsole.Colors["Issue_Open"] = ConsoleColor.DarkYellow;
            ColorConsole.Colors["Issue_User_Self"] = ConsoleColor.Cyan;
            ColorConsole.Colors["Issue_User"] = ConsoleColor.DarkCyan;
            ColorConsole.Colors["Issue_Par"] = ConsoleColor.DarkYellow;

            var valid = GitHub.ValidateGitDirectory();
            if (valid.IsError)
            {
                ColorConsole.WriteLine(valid.GetMessage());
                return;
            }

#if DEBUG
            Command.SimulateREPL(() => new MainCommand(), "quit", HELP);
#else
            try { new MainCommand().RunCommand(args, HELP); }
            catch (AggregateException aggex) when (aggex.InnerExceptions.Count == 1 && aggex.InnerException is Octokit.AuthorizationException)
            {
                Octokit.AuthorizationException credex = aggex.InnerException as Octokit.AuthorizationException;

                ColorConsole.WriteLine("GitHub responded to your request with an authentication error:");
                ColorConsole.WriteLine($"[Red:[{credex.Message}] {credex.StatusCode}]");
                ColorConsole.WriteLine("Run [Yellow:github config --set authtoken <token>] to set authentication token.");
            }
#endif
        }

        private class MainCommand : Command
        {
            public MainCommand()
            {
                SubCommands.Add("config", new ConfigCommand());
                SubCommands.Add("issues", new IssuesCommand());
                SubCommands.Add("go", new GoCommand());

                Validator.Add(GetHelpMessage);
            }

            protected override bool HandleAlias(string alias, out string replaceby)
            {
                replaceby = Config.Default["alias." + alias];
                return replaceby != null;
            }
        }
    }
}
