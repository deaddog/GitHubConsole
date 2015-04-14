﻿using CommandLineParsing;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHubConsole.Commands
{
    public class IssuesCommand : Command
    {
        private RepositoryIssueRequest request = new RepositoryIssueRequest();

        [Name("--format", "-f"), Description("Allows for describing an output format when listing issues.")]
        private readonly Parameter<string> outputFormat = null;

        [Description("List issues that are currently open.")]
        private readonly FlagParameter open = null;
        [Description("List issues that are currently closed.")]
        private readonly FlagParameter closed = null;
        [Description("List issues that are currently either open or closed.")]
        private readonly FlagParameter all = null;

        [Description("A set of labels that issues must have in order to be listed.")]
        private readonly Parameter<string[]> labels = null;

        [Name("--has-assignee"), Description("List issues that have an assignee.")]
        private readonly FlagParameter hasAssignee = null;
        [Name("--no-assignee"), Description("List issues that does not have an assignee.")]
        private readonly FlagParameter noAssignee = null;

        [Name("--assignee"), Description("List issues where the assignee is one in a range of users.")]
        private readonly Parameter<string[]> assignee = null;
        [Name("--not-assignee"), Description("List issues where the assignee is not one in a range of users.")]
        private readonly Parameter<string[]> notAssignee = null;

        [Description("Assign yourself to a selected issue (or range of issues).")]
        private readonly FlagParameter take = null;
        [Description("Unassign yourself from a selected issue (or range of issues).")]
        private readonly FlagParameter drop = null;

        [Name("--set-labels", "-sl"), Description("Adds a set of labels to a selected issue (or range of issues).")]
        private readonly Parameter<string[]> setLabel = null;
        [Name("--remove-labels", "-rl"), Description("Removes a set of labels from a selected issue (or range of issues).")]
        private readonly Parameter<string[]> remLabel = null;

        [Name("--create"), Description("Creates a new issue.")]
        private readonly Parameter<string> create = null;

        [NoName]
        private readonly Parameter<int[]> issuesIn = null;

        private List<Issue> issues;
        private string assignUser;

        public IssuesCommand()
        {
            SubCommands.Add("create", new IssuesCreateCommand());

            outputFormat.SetDefault(Config.Default["issues.format"] ?? "%#% %user% %title% %labels%");
            assignee.Validator.Add(x => x.Length > 0, "A user must be specified for the " + assignee.Name + " parameter.");
            notAssignee.Validator.Add(x => x.Length > 0, "A user must be specified for the " + assignee.Name + " parameter.");
            labels.Validator.Add(x => x.Length > 0, "At least one label name must be supplied for the " + labels.Name + " parameter.");

            setLabel.Validator.Add(x => x.Length > 0, "You must specify a set of labels to set:\n"
                + "  gihub issues <issues> " + setLabel.Name + " <label1> <label2>...");
            remLabel.Validator.Add(x => x.Length > 0, "You must specify a set of labels to remove:\n"
                + "  gihub issues <issues> " + remLabel.Name + " <label1> <label2>...");

            create.Validator.Add(x => x.Trim().Length > 0, "An issue cannot be created with an empty title.");
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
                return "You must specify which issues # to assign yourself to.\nFor instance: [White:github issues 5 7 " + take.Name + "] will assign you to issue #5 and #7.";

            if (drop.IsSet && issuesIn.Value.Length == 0)
                return "You must specify which issues # to unassign yourself from.\nFor instance: [White:github issues 5 7 " + drop.Name + "] will unassign you from issue #5 and #7.";

            if (!setLabel.IsDefault && issuesIn.Value.Length == 0)
                return "You must specify which issues # to add labels to.\nFor instance: [White:github issues 5 7 " + setLabel.Name + " bug] will label issue #5 and #7 with the bug label.";

            if (!remLabel.IsDefault && issuesIn.Value.Length == 0)
                return "You must specify which issues # to remove labels from.\nFor instance: [White:github issues 5 7 " + remLabel.Name + " bug] will remove the bug label from issue #5 and #7.";

            if (issuesIn.Value.Length > 0 || !create.IsDefault)
            {
                if (open.IsSet || closed.IsSet || all.IsSet || !labels.IsDefault || hasAssignee.IsSet || noAssignee.IsSet || !assignee.IsDefault || !notAssignee.IsDefault)
                    return "Issue filtering cannot be applied when specifying specific issues or creating new ones.";
            }

            if (!create.IsDefault && !issuesIn.IsDefault)
                return "You cannot specify issues # when creating a new issue.";

            if (take.IsSet || drop.IsSet || !remLabel.IsDefault || !setLabel.IsDefault)
                if (!GitHub.Client.Repository.Get(GitHub.Username, GitHub.Project).Result.Permissions.Admin)
                    return "You do not have admin rights for the [Yellow:" + GitHub.Username + "/" + GitHub.Project + "] repository.\n " + take.Name + " and " + drop.Name + " are not available.";

            if (!setLabel.IsDefault || !remLabel.IsDefault)
            {
                var all = setLabel.Value.Concat(remLabel.Value).ToList();
                var labels = GitHub.Client.Issue.Labels.GetForRepository(GitHub.Username, GitHub.Project).Result.Select(x => x.Name).ToList();
                foreach (var a in all)
                    if (!labels.Contains(a))
                        return "There is no [Red:" + a + "] label in this repository.";
            }

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
                    x => string.Format("[DarkCyan:{0}] is assigned to issue [DarkYellow:#{1}], you cannot be assigned.", x.Assignee.Login, x.Number));

                if (msg.IsError)
                    return msg;
            }
            if (drop.IsSet)
            {
                assignUser = GitHub.Client.User.Current().Result.Login;
                Message msg;

                msg = ValidateEach(issues, x => x.Assignee != null,
                    x => string.Format("No one is assigned to issue [DarkYellow:#{0}], you cannot be unassigned.", x.Number));
                if (msg.IsError)
                    return msg;

                msg = ValidateEach(issues, x => x.Assignee.Login == assignUser,
                    x => string.Format("[DarkCyan:{0}] is assigned to issue [DarkYellow:#{1}], you cannot be unassigned.", x.Assignee.Login, x.Number));
                if (msg.IsError)
                    return msg;
            }

            return base.Validate();
        }

        #region Filtering

        private bool validateIssue(Issue issue)
        {
            if (issuesIn.Value.Length > 0)
                return issuesIn.Value.Contains(issue.Number);

            return
                validateIssueState(issue.State) &&
                validateIssueAssignee(issue.Assignee) &&
                validateIssueLabels(issue.Labels.Select(x => x.Name).ToList());
        }
        private bool validateIssueState(ItemState state)
        {
            switch (state)
            {
                case ItemState.Closed: return all.IsSet || closed.IsSet;
                case ItemState.Open: return all.IsSet || open.IsSet || !closed.IsSet;
            }
            return false;
        }
        private bool validateIssueAssignee(User a)
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
        private bool validateIssueLabels(List<string> lbls)
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

        #endregion

        protected override void Execute()
        {
            if (take.IsSet || drop.IsSet || !setLabel.IsDefault || !remLabel.IsDefault)
            {
                for (int i = 0; i < issues.Count; i++)
                {
                    var update = issues[i].ToUpdate();
                    if (take.IsSet) update.Assignee = assignUser;
                    else if (drop.IsSet) update.Assignee = null;
                    else update.Assignee = issues[i].Assignee == null ? null : issues[i].Assignee.Login;

                    foreach (var l in setLabel.Value)
                        if (update.Labels == null || !update.Labels.Contains(l))
                            update.AddLabel(l);

                    if (update.Labels != null)
                        foreach (var l in remLabel.Value)
                            update.Labels.Remove(l);

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

            format = format.Replace("%#%", "[{1}:{0}]");
            format = format.Replace("%user%", "[{3}:{2}]");
            format = format.Replace("%title%", "{4}");
            format = format.Replace("%labels%", "{5}");

            foreach (var v in issues)
            {
                string name = v.Assignee == null ? "" : v.Assignee.Login;

                string labels = "";
                if (v.Labels.Count > 0 && format.Contains("{5}"))
                {
                    labels = string.Format("[DarkYellow:(]{0}[DarkYellow:)]",
                        string.Join(", ", v.Labels.Select(l => "[" + ColorResolver.GetConsoleColor(l.Color) + ":" + l.Name + "]")));
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
