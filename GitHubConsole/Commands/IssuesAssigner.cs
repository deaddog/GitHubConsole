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
        private bool isTake = false;

        private List<int> issues = new List<int>();

        public IssuesAssigner(bool istake)
        {
            this.isTake = istake;
        }

        public override void Execute()
        {
            string username, project;
            GitHubClient client = CreateClient(out username, out project);
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
                    var issue = client.Issue.Get(username, project, issues[i]).Result;
                    if (issue.Assignee != null)
                    {
                        "[[:DarkCyan:{0}]] is assigned to issue [[:DarkYellow:#{1}]], you cannot be assigned.".ToConsoleLine(issue.Assignee.Login, issue.Number);
                        continue;
                    }

                    var update = issue.ToUpdate();
                    update.Assignee = assignUser;
                    client.Issue.Update(username, project, issue.Number, update).Wait();
                }
            }

            else // drop
            {
                if (issues.Count == 0)
                {
                    "You must specify which issues # to unassign yourself from.".ToConsoleLine();
                    "For instance: [[:White:github issues drop 5 7]] will unassign you from issue #5 and #7.".ToConsoleLine();
                }

                string assignUser = client.User.Current().Result.Login;
                for (int i = 0; i < issues.Count; i++)
                {
                    var issue = client.Issue.Get(username, project, issues[i]).Result;
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
                    client.Issue.Update(username, project, issue.Number, update).Wait();
                }
            }
        }

        public override bool HandleArgument(ArgumentStack.Argument argument)
        {
            int id;
            if (!int.TryParse(argument.Key, out id))
            {
                Console.WriteLine("GitHub issue # must be an integer. \"{0}\" is not valid.", argument.Key);
                return false;
            }

            issues.Add(id);
            return true;
        }
    }
}
