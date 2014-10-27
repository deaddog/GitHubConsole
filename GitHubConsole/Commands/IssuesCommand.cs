using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubConsole.Commands
{
    public class IssuesCommand : Command
    {
        private bool openArgFound = false;
        private RepositoryIssueRequest request = new RepositoryIssueRequest();

        public override void Execute()
        {
            string username, project;
            GitHubClient client = CreateClient(out username, out project);
            if (client == null)
                return;

            listIssues(client, username, project, request);
        }

        public override bool HandleArgument(ArgumentStack.Argument argument)
        {
            switch (argument.Key)
            {
                case "-open":
                    openArgFound = true;
                    if (request.State == ItemState.Closed)
                        request.State = ItemState.All;
                    return true;
                case "-closed":
                    if (request.State == ItemState.Open)
                        request.State = openArgFound ? ItemState.All : ItemState.Closed;
                    return true;
                case "-all":
                    request.State = ItemState.All;
                    return true;

                default:
                    return base.HandleArgument(argument);
            }
        }

        private void listIssues(GitHubClient client, string username, string project, RepositoryIssueRequest req)
        {
            var q = client.Issue.GetForRepository(username, project, req).Result;
            int len = q[0].Number.ToString().Length;
            int namelen = q.Count == 0 ? 0 : (from v in q
                                              let n = v.Assignee == null ? "" : v.Assignee.Login
                                              select n.Length).Max();
            foreach (var v in q)
            {
                if (v.ClosedAt.HasValue)
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                else
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write(v.Number.ToString().PadLeft(len));

                string name = v.Assignee == null ? "" : v.Assignee.Login;
                if (name == client.Credentials.Login)
                    Console.ForegroundColor = ConsoleColor.Cyan;
                else
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(" {0}", name.PadRight(namelen));

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(" {0}", v.Title);

                if (v.Labels.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write(" (");

                    bool first = true;
                    foreach (var label in v.Labels)
                    {
                        if (first)
                            first = false;
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write(", ");
                        }

                        Console.ForegroundColor = ColorResolver.GetConsoleColor(label.Color);
                        Console.Write(label.Name);
                    }

                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write(")");
                }

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
            }
        }
    }
}
