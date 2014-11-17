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

                    var update = issue.ToUpdate();
                    update.Assignee = assignUser;
                    client.Issue.Update(username, project, issue.Number, update).Wait();
                }
            }

            if (isDrop)
            {
                if (issues.Count == 0)
                {
                    Console.WriteLine("You must specify which issues # to unassign yourself from.");
                    Console.Write("For instance: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("github issues drop 5 7");
                    Console.ResetColor();
                    Console.WriteLine(" will unassign you from issue #5 and #7.");
                }

                string assignUser = client.User.Current().Result.Login;
                for (int i = 0; i < issues.Count; i++)
                {
                    var issue = client.Issue.Get(username, project, issues[i]).Result;
                    if (issue.Assignee == null)
                    {
                        Console.Write("No one is assigned to issue ");

                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("#{0}", issue.Number);
                        Console.ResetColor();

                        Console.WriteLine(", you cannot be unassigned.");
                        continue;
                    }
                    if (issue.Assignee.Login != assignUser)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.Write(issue.Assignee.Login);
                        Console.ResetColor();

                        Console.Write(" is assigned to issue ");

                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("#{0}", issue.Number);
                        Console.ResetColor();

                        Console.WriteLine(", you cannot be unassigned.");
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
