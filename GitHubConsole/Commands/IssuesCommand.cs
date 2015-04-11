using CommandLineParsing;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHubConsole.Commands
{
    public class IssuesCommand : Command
    {
        private RepositoryIssueRequest request = new RepositoryIssueRequest();
        private Predicate<Issue> validator = null;

        [Name("--format", "-f")]
        private readonly Parameter<string> outputFormat = null;

        private readonly FlagParameter open = null, closed = null, all = null;

        private readonly Parameter<string[]> labels = null;

        [Name("--has-assignee")]
        private readonly FlagParameter hasAssignee = null;
        [Name("--no-assignee")]
        private readonly FlagParameter noAssignee = null;

        [Name("--assignee")]
        private readonly Parameter<string[]> assignee = null;
        [Name("--not-assignee")]
        private readonly Parameter<string[]> notAssignee = null;

        private readonly FlagParameter take = null, drop = null;

        [NoName]
        private readonly Parameter<int[]> issuesIn = null;

        private List<Issue> issues;
        private string assignUser;

        public IssuesCommand()
        {
            SubCommands.Add("create", new IssuesCreateCommand());
            SubCommands.Add("label", new IssuesLabeler());

            outputFormat.SetDefault(Config.Default["issues.format"] ?? "%#% %user% %title% %labels%");
            assignee.Validate(x => x.Length > 0, "A user must be specified for the " + assignee.Name + " parameter.");
            notAssignee.Validate(x => x.Length > 0, "A user must be specified for the " + assignee.Name + " parameter.");
            labels.Validate(x => x.Length > 0, "At least one label name must be supplied for the " + labels.Name + " parameter.");
        }

        protected override Message Validate()
        {
            if (hasAssignee.IsSet || noAssignee.IsSet)
            {
                if (!assignee.IsDefault)
                    return string.Format("The {0} parameter cannot be used with the {1} or the {2} flag.", assignee.Name, hasAssignee.Name, noAssignee.Name);

                if (!notAssignee.IsDefault)
                    return string.Format("The {0} parameter cannot be used with the {1} or the {2} flag.", notAssignee.Name, hasAssignee.Name, noAssignee.Name);
            }

            if (!assignee.IsDefault && !notAssignee.IsDefault)
                return string.Format("The {0} parameter cannot be used with the {1} parameter.", assignee.Name, notAssignee.Name);

            if (take.IsSet && drop.IsSet)
                return string.Format("The {0} and {1} parameters cannot be used simultaneously.", take.Name, drop.Name);

            if (take.IsSet && issuesIn.Value.Length == 0)
                return "You must specify which issues # to assign yourself to.\nFor instance: [[:White:github issues 5 7 --take]] will assign you to issue #5 and #7.";

            if (drop.IsSet && issuesIn.Value.Length == 0)
                return "You must specify which issues # to unassign yourself from.\nFor instance: [[:White:github issues 5 7 --drop]] will unassign you from issue #5 and #7.";

            if (issuesIn.Value.Length > 0)
            {
                if (open.IsSet || closed.IsSet || all.IsSet || !labels.IsDefault || hasAssignee.IsSet || noAssignee.IsSet || !assignee.IsDefault || !notAssignee.IsDefault)
                    return "Issue filtering cannot be applied when specifying specific issues.";
            }

            if (take.IsSet || drop.IsSet)
                if (!GitHub.Client.Repository.Get(GitHub.Username, GitHub.Project).Result.Permissions.Admin)
                    return "You do not have admin rights for the [[:Yellow:" + GitHub.Username + "/" + GitHub.Project + "]] repository.\n " + take.Name + " and " + drop.Name + " are not available.";

            issues = GitHub.Client.Issue.GetForRepository(GitHub.Username, GitHub.Project, new RepositoryIssueRequest() { State = ItemState.All }).Result.ToList();

            if (issuesIn.Value.Length > 0)
            {
                int max = issues.Select(x => x.Number).Max();

                foreach (var i in issuesIn.Value)
                    if (i > max) return string.Format("The repo does not contain a #{0} issue.", i);
            }

            for (int i = 0; i < issues.Count; i++)
                if (!validateIssue(issues[i]))
                    issues.RemoveAt(i--);

            if (take.IsSet)
            {
                assignUser = GitHub.Client.User.Current().Result.Login;
                var msg = ValidateEach(issues, x => x.Assignee == null,
                    x => string.Format("[[:DarkCyan:{0}]] is assigned to issue [[:DarkYellow:#{1}]], you cannot be assigned.", x.Assignee.Login, x.Number));

                if (msg.IsError)
                    return msg;
            }
            if (drop.IsSet)
            {
                assignUser = GitHub.Client.User.Current().Result.Login;
                Message msg;

                msg = ValidateEach(issues, x => x.Assignee != null,
                    x => string.Format("No one is assigned to issue [[:DarkYellow:#{0}]], you cannot be unassigned.", x.Number));
                if (msg.IsError)
                    return msg;

                msg = ValidateEach(issues, x => x.Assignee.Login == assignUser,
                    x => string.Format("[[:DarkCyan:{0}]] is assigned to issue [[:DarkYellow:#{1}]], you cannot be unassigned.", x.Assignee.Login, x.Number));
                if (msg.IsError)
                    return msg;
            }

            return base.Validate();
        }

        private bool validateIssue(Issue issue)
        {
            if (issuesIn.Value.Length > 0)
                return issuesIn.Value.Contains(issue.Number);

            return
                validateState(issue.State) &&
                validateAssignee(issue.Assignee) &&
                validateLabels(issue.Labels.Select(x => x.Name).ToList());
        }
        private bool validateState(ItemState state)
        {
            switch (state)
            {
                case ItemState.Closed: return all.IsSet || closed.IsSet;
                case ItemState.Open: return all.IsSet || open.IsSet || !closed.IsSet;
            }
            return false;
        }
        private bool validateAssignee(User a)
        {
            if (!assignee.IsDefault)
                return a != null && assignee.Value.Contains(a.Login);
            else if (!notAssignee.IsDefault)
                return a == null || !notAssignee.Value.Contains(a.Login);
            else if (hasAssignee.IsSet)
                return a != null;
            else if (noAssignee.IsSet)
                return a == null;
            else
                return true;
        }
        private bool validateLabels(List<string> lbls)
        {
            if (labels.IsDefault)
                return true;

            var par = new Stack<string>(labels.Value);
            while (par.Count > 0)
            {
                var p = par.Pop();
                if (p.StartsWith("^") && lbls.Contains(p.Substring(1)))
                    return false;
                else if (!p.StartsWith("^") && !lbls.Contains(p))
                    return false;
            }
            return true;
        }

        protected override void Execute()
        {
            if (take.IsSet)
            {
                ColorConsole.WriteLine("Assigning [[:Cyan:{0}]] to issue{2} {1}.", assignUser, issuesIn.Value.ToString(", ", " and "), issuesIn.Value.Length > 1 ? "s" : "");

                for (int i = 0; i < issues.Count; i++)
                {
                    var update = issues[i].ToUpdate();
                    update.Assignee = assignUser;
                    issues[i] = GitHub.Client.Issue.Update(GitHub.Username, GitHub.Project, issues[i].Number, update).Result;
                }
            }
            else if (drop.IsSet)
            {
                ColorConsole.WriteLine("Removing [[:Cyan:{0}]] as assignee for issue{2} {1}.", assignUser, issuesIn.Value.ToString(", ", " and "), issuesIn.Value.Length > 1 ? "s" : "");

                for (int i = 0; i < issues.Count; i++)
                {
                    var update = issues[i].ToUpdate();
                    update.Assignee = null;
                    issues[i] = GitHub.Client.Issue.Update(GitHub.Username, GitHub.Project, issues[i].Number, update).Result;
                }
            }

            listIssues();
        }

        private void listIssues()
        {
            if (issues.Count == 0)
                return;

            int len = issues[0].Number.ToString().Length;
            int namelen = (from v in issues
                           let n = v.Assignee == null ? "" : v.Assignee.Login
                           select n.Length).Max();

            string format = outputFormat.Value;

            format = format.Replace("%#%", "[[:{1}:{0}]]");
            format = format.Replace("%user%", "[[:{3}:{2}]]");
            format = format.Replace("%title%", "{4}");
            format = format.Replace("%labels%", "{5}");

            foreach (var v in issues)
            {
                string name = v.Assignee == null ? "" : v.Assignee.Login;

                string labels = "";
                if (v.Labels.Count > 0 && format.Contains("{5}"))
                {
                    labels = string.Format("[[:DarkYellow:(]]{0}[[:DarkYellow:)]]",
                        string.Join(", ", v.Labels.Select(l => "[[:" + ColorResolver.GetConsoleColor(l.Color) + ":" + l.Name + "]]")));
                }

                ColorConsole.WriteLine(format,
                    v.Number.ToString().PadLeft(len), v.ClosedAt.HasValue ? "DarkRed" : "DarkYellow",
                    name.PadRight(namelen), name == GitHub.Client.Credentials.Login ? "Cyan" : "DarkCyan",
                    v.Title.Trim(),
                    labels);
            }
        }
    }
}
