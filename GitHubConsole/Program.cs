using CommandLineParsing;
using GitHubConsole.Commands;
using System;

namespace GitHubConsole
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            Command.SimulateREPL(() => new MainCommand(), "quit");
#else
            try { Command.RunCommand(new MainCommand(), args); }
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
#endif
        }

        private class MainCommand : Command
        {
            public MainCommand()
            {
                SubCommands.Add("config", new ConfigCommand());
                SubCommands.Add("issues", new IssuesCommand());
            }
        }
    }
}
