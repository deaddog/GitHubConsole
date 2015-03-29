using CommandLineParsing;
using Octokit;
using System;
using System.Collections.Generic;

namespace GitHubConsole.Commands
{
    public class IssuesAssigner : Command
    {
        private bool isTake = false;

        [NoName]
        private Parameter<int[]> issues;

        public IssuesAssigner(bool istake)
        {
            this.isTake = istake;

            issues.TypeErrorMessage = x => "GitHub issue # must be an integer. \"" + x + "\" is not valid.";
        }

        protected override Message Validate()
        {
            if (isTake)
            {
                if (issues.Value.Length == 0)
                    return "You must specify which issues # to assign yourself to.\nFor instance: [[:White:github issues take 5 7]] will assign you to issue #5 and #7.";
            }
            else
            {
                if (issues.Value.Length == 0)
                    return "You must specify which issues # to unassign yourself from.\nFor instance: [[:White:github issues drop 5 7]] will unassign you from issue #5 and #7.";
            }

            return base.Validate();
        }

        public override void Execute()
        {
            GitHubClient client = GitHub.Client;
            if (client == null)
                return;

            if (isTake)
            {
                string assignUser = client.User.Current().Result.Login;
                for (int i = 0; i < issues.Value.Length; i++)
                {
                    var issue = client.Issue.Get(GitHub.Username, GitHub.Project, issues.Value[i]).Result;
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

            else // drop
            {
                string assignUser = client.User.Current().Result.Login;
                for (int i = 0; i < issues.Value.Length; i++)
                {
                    var issue = client.Issue.Get(GitHub.Username, GitHub.Project, issues.Value[i]).Result;
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
    }
}
