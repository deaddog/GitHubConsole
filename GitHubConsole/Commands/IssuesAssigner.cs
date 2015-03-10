using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubConsole.Commands
{
    public class IssuesAssigner : Command
    {
        private bool isFirst = true;
        private bool isTake = false;
        private bool isDrop = false;

        private List<int> issues = new List<int>();

        public override void Execute()
        {
            GitHubClient client = GitHub.Client;
            if (client == null)
                return;

            if (isTake)
            {
                if (issues.Count == 0)
                {
                    Console.WriteLine("You must specify which issues # to assign yourself to.");
                    Console.Write("For instance: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("github issues take 5 7");
                    Console.ResetColor();
                    Console.WriteLine(" will assign you to issue #5 and #7.");
                }

                string assignUser = client.User.Current().Result.Login;
                for (int i = 0; i < issues.Count; i++)
                {
                    var issue = client.Issue.Get(GitHub.Username, GitHub.Project, issues[i]).Result;
                    if (issue.Assignee != null)
                    {
                        "[[:DarkCyan:{0}]] is assigned to issue [[:DarkYellow:#{1}]], you cannot be assigned.".ToConsoleLine(issue.Assignee.Login, issue.Number);
                        continue;
                    }

                    var update = issue.ToUpdate();
                    update.Assignee = assignUser;
                    client.Issue.Update(GitHub.Username, GitHub.Project, issue.Number, update).Wait();
                }
            }

            if (isDrop)
            {
                if (issues.Count == 0)
                {
                    "You must specify which issues # to unassign yourself from.".ToConsoleLine();
                    "For instance: [[:White:github issues drop 5 7]] will unassign you from issue #5 and #7.".ToConsoleLine();
                }

                string assignUser = client.User.Current().Result.Login;
                for (int i = 0; i < issues.Count; i++)
                {
                    var issue = client.Issue.Get(GitHub.Username, GitHub.Project, issues[i]).Result;
                    if (issue.Assignee == null)
                    {
                        "No one is assigned to issue [[:DarkYellow:#{0}]], you cannot be unassigned.".ToConsoleLine(issue.Number);
                        continue;
                    }
                    if (issue.Assignee.Login != assignUser)
                    {
                        "[[:DarkCyan:{0}]] is assigned to issue [[:DarkYellow:#{1}]], you cannot be unassigned.".ToConsoleLine(issue.Assignee.Login, issue.Number);
                        continue;
                    }

                    var update = issue.ToUpdate();
                    update.Assignee = null;
                    client.Issue.Update(GitHub.Username, GitHub.Project, issue.Number, update).Wait();
                }
            }
        }

        public override bool HandleArgument(ArgumentStack.Argument argument)
        {
            if (isFirst)
            {
                isFirst = false;
                return handleFirst(argument);
            }
            else
                return handleRest(argument);
        }

        private bool handleFirst(ArgumentStack.Argument argument)
        {
            switch (argument.Key)
            {
                case "take":
                    isTake = true;
                    return true;

                case "drop":
                    isDrop = true;
                    return true;

                default:
                    return base.HandleArgument(argument);
            }
        }
        private bool handleRest(ArgumentStack.Argument argument)
        {
            int id;
            if (!int.TryParse(argument.Key, out id))
            {
                Console.WriteLine("GitHub issue # must be an integer. \"{0}\" is not valid.", argument.Key);
                return false;
            }

            if (isTake || isDrop)
                issues.Add(id);
            else
            {
                Console.WriteLine("take or drop must be specified before attempting to handle assignment,.");
                return false;
            }
            return true;
        }
    }
}
