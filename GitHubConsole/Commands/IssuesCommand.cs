﻿using CommandLineParsing;
using CommandLineParsing.Output;
using CommandLineParsing.Output.Formatting;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitHubConsole.Commands
{
    [Description("Lists, modifies and creates github issues for this repository")]
    public class IssuesCommand : Command
    {
        private RepositoryIssueRequest request = new RepositoryIssueRequest();

        [Name("--format", "-f"), Description("Allows for describing an output format when listing issues.")]
        private readonly Parameter<string> outputFormat = null;

        [Name("--opened", "-o"), Description("List issues that are currently open.")]
        private readonly FlagParameter opened = null;
        [Name("--closed", "-c"), Description("List issues that are currently closed.")]
        private readonly FlagParameter closed = null;
        [Name("--all", "-a"), Description("List issues that are currently either open or closed.")]
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
        private readonly Parameter<string[]> setLabels = null;
        [Name("--remove-labels", "-rl"), Description("Removes a set of labels from a selected issue (or range of issues).")]
        private readonly Parameter<string[]> remLabels = null;
        [Name("--edit-labels", "-el"), Description("Allow for editing an issues labels using a menu interface.")]
        private readonly FlagParameter editLabels = null;

        [Name("--create"), Description("Creates a new issue.")]
        private readonly FlagParameter create = null;
        [Name("--open"), Description("Opens an issue.")]
        private readonly FlagParameter open = null;
        [Name("--close"), Description("Closes an issue.")]
        private readonly FlagParameter close = null;

        [Name("--title"), Description("Sets the title of an issue.")]
        private readonly Parameter<string> setTitle = null;
        [Name("--description"), Description("Set the description of an issue.")]
        private readonly Parameter<string> setDescription = null;
        [Name("--edit"), Description("Opens a text-editor for editing issue title and description.")]
        private readonly FlagParameter edit = null;

        [NoName]
        private readonly Parameter<int[]> issuesIn = null;

        private List<Issue> issues;
        private string assignUser;

        public IssuesCommand()
        {
            PreValidator.Add(GitHub.ValidateGitDirectory);

            outputFormat.SetDefault(Config.Default["issues.format"] ?? "[auto:$+number] [auto:$assignee+] $title ?labels{[DarkYellow:(]@labels{[auto:$label], }[DarkYellow:)]}");
            assignee.Validator.Fail.If(x => x.Length == 0, "A user must be specified for the " + assignee.Name + " parameter.");
            notAssignee.Validator.Fail.If(x => x.Length == 0, "A user must be specified for the " + assignee.Name + " parameter.");
            labels.Validator.Fail.If(x => x.Length == 0, "At least one label name must be supplied for the " + labels.Name + " parameter.");

            setLabels.Validator.Fail.If(x => x.Length == 0, "You must specify a set of labels to set:\n"
                + "  gihub issues <issues> " + setLabels.Name + " <label1> <label2>...");
            remLabels.Validator.Fail.If(x => x.Length == 0, "You must specify a set of labels to remove:\n"
                + "  gihub issues <issues> " + remLabels.Name + " <label1> <label2>...");

            setTitle.Validator.Fail.If(x => x.Trim().Length == 0, "An issue cannot have an empty title.");

            Validator.Ensure.IfFirstNotRest(assignee, hasAssignee, noAssignee, notAssignee);
            Validator.Ensure.IfFirstNotRest(notAssignee, hasAssignee, noAssignee);

            Validator.Ensure.ZeroOrOne(close, open, create);
            Validator.Ensure.ZeroOrOne(edit, create);
            Validator.Ensure.IfFirstNotRest(edit, setTitle, setDescription);

            Validator.Ensure.ZeroOrOne(editLabels, setLabels);
            Validator.Ensure.ZeroOrOne(editLabels, remLabels);

            Validator.Ensure.ZeroOrOne(take, drop);

            Validator.Add(Validate);
        }

        protected override Message GetHelpMessage()
        {
            return
                "Provides information about and operations on issues on GitHub.com.\n" +
                "To address specific issues use:\n\n" +
                "  [Example:github issues \\[1 2 3...\\] <arguments>]\n\n" +
                "Below is a complete list of all parameters for this command:" +

            base.GetParametersMessage(2);
        }

        protected Message Validate()
        {
            if (take.IsSet && issuesIn.Value.Length == 0 && !create.IsSet)
                return "You must specify which issues # to assign yourself to.\nFor instance: [White:github issues 5 7 " + take.Name + "] will assign you to issue #5 and #7.";

            if (drop.IsSet && issuesIn.Value.Length == 0)
                return "You must specify which issues # to unassign yourself from.\nFor instance: [White:github issues 5 7 " + drop.Name + "] will unassign you from issue #5 and #7.";

            if (setLabels.IsSet && issuesIn.Value.Length == 0 && !create.IsSet)
                return "You must specify which issues # to add labels to.\nFor instance: [White:github issues 5 7 " + setLabels.Name + " bug] will label issue #5 and #7 with the bug label.";

            if (editLabels.IsSet && issuesIn.Value.Length == 0 && !create.IsSet)
                return "You must specify which issues # to edit labels for.\nFor instance: [White:github issues 5 7 " + editLabels.Name + " bug] will allow you to edit labels for issue #5 and #7.";

            if (remLabels.IsSet && issuesIn.Value.Length == 0)
                return "You must specify which issues # to remove labels from.\nFor instance: [White:github issues 5 7 " + remLabels.Name + " bug] will remove the bug label from issue #5 and #7.";

            if (issuesIn.Value.Length > 0 || create.IsSet)
            {
                if (opened.IsSet || closed.IsSet || all.IsSet || labels.IsSet || hasAssignee.IsSet || noAssignee.IsSet || assignee.IsSet || notAssignee.IsSet)
                    return "Issue filtering cannot be applied when specifying specific issues or creating new ones.";
            }

            if (issuesIn.Value.Length == 0 && close.IsSet)
                return $"You must specify which issue to close:\n  [Example:github issues 2 {close.Name}]";
            if (issuesIn.Value.Length == 0 && open.IsSet)
                return $"You must specify which issue to open:\n  [Example:github issues 2 {open.Name}]";

            if (setTitle.IsSet && !issuesIn.IsSet && !create.IsSet)
                return "You much specify the issue to which the title is assigned.";
            if (setTitle.IsSet && issuesIn.IsSet && issuesIn.Value.Length > 1)
                return "Title cannot be assigned to multiple issues at once.";

            if (setDescription.IsSet && !issuesIn.IsSet && !create.IsSet)
                return "You much specify the issue to which the description is assigned.";
            if (setDescription.IsSet && issuesIn.IsSet && issuesIn.Value.Length > 1)
                return "Description cannot be assigned to multiple issues at once.";

            if (setDescription.IsSet && !setTitle.IsSet)
                return $"When using the {setDescription.Name} parameter you must also use the {setTitle.Name} parameter.";

            if (edit.IsSet && !issuesIn.IsSet)
                return "You much specify which issue you want to edit.";
            if (edit.IsSet && issuesIn.IsSet && issuesIn.Value.Length > 1)
                return "You can only specify one issue for editing.";

            if (create.IsSet && issuesIn.IsSet)
                return "You cannot specify issues # when creating a new issue.";

            if (create.IsSet && drop.IsSet)
                return string.Format("You cannot unassign yourself from an issue you are creating.");

            if (create.IsSet && remLabels.IsSet)
                return string.Format("You cannot remove labels from an issue you are creating.");

            if (take.IsSet || drop.IsSet || remLabels.IsSet || setLabels.IsSet || editLabels.IsSet)
                if (!GitHub.Client.Repository.Get(GitHub.Username, GitHub.Project).Result.Permissions.Admin)
                    return "You do not have admin rights for the [Yellow:" + GitHub.Username + "/" + GitHub.Project + "] repository.\n " + take.Name + " and " + drop.Name + " are not available.";

            if (setLabels.IsSet || remLabels.IsSet)
            {
                var all = setLabels.Value.Concat(remLabels.Value).ToList();
                var labels = GitHub.Client.Issue.Labels.GetAllForRepository(GitHub.Username, GitHub.Project).Result.Select(x => x.Name).ToList();
                foreach (var a in all)
                    if (!labels.Contains(a))
                        return "There is no [Red:" + a + "] label in this repository.";
            }

            if (create.IsSet)
            {
                if (!setTitle.IsSet)
                {
                    var m = titleAndDescriptionFromFile();
                    if (m.IsError)
                        return m;
                }

                assignUser = GitHub.Client.User.Current().Result.Login;
                return Message.NoError;
            }

            issues = GitHub.Client.Issue.GetAllForRepository(GitHub.Username, GitHub.Project).Result.ToList();

            if (issuesIn.Value.Length > 0)
            {
                int max = issues.Select(x => x.Number).Max();

                foreach (var i in issuesIn.Value)
                    if (i > max) return string.Format("The repo does not contain a #{0} issue.", i);
            }

            if (edit.IsSet)
            {
                var iss = issues.Where(x => x.Number == issuesIn.Value[0]).First();
                setTitle.Value = iss.Title;
                setDescription.Value = iss.Body;
                var m = titleAndDescriptionFromFile();
                if (m.IsError)
                    return m;
            }

            for (int i = 0; i < issues.Count; i++)
                if (!validateIssue(issues[i]))
                    issues.RemoveAt(i--);

            if (issuesIn.IsSet)
                issues.Sort((x, y) => Array.IndexOf(issuesIn.Value, x.Number).CompareTo(Array.IndexOf(issuesIn.Value, y.Number)));

            if (take.IsSet)
            {
                assignUser = GitHub.Client.User.Current().Result.Login;
                var msg = issues.ValidateEach(x => x.Assignee == null || x.Assignee.Login == assignUser,
                    x => string.Format("[DarkCyan:{0}] is assigned to issue [DarkYellow:#{1}], you cannot be assigned.", x.Assignee.Login, x.Number));

                if (msg.IsError)
                    return msg;
            }
            if (drop.IsSet)
            {
                assignUser = GitHub.Client.User.Current().Result.Login;
                Message msg;

                msg = issues.ValidateEach(x => x.Assignee != null,
                    x => string.Format("No one is assigned to issue [DarkYellow:#{0}], you cannot be unassigned.", x.Number));
                if (msg.IsError)
                    return msg;

                msg = issues.ValidateEach(x => x.Assignee.Login == assignUser,
                    x => string.Format("[DarkCyan:{0}] is assigned to issue [DarkYellow:#{1}], you cannot be unassigned.", x.Assignee.Login, x.Number));
                if (msg.IsError)
                    return msg;
            }

            return Message.NoError;
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
        private bool validateIssueState(StringEnum<ItemState> state)
        {
            switch (state.Value)
            {
                case ItemState.Closed: return all.IsSet || closed.IsSet;
                case ItemState.Open: return all.IsSet || opened.IsSet || !closed.IsSet;
            }
            return false;
        }
        private bool validateIssueAssignee(User a)
        {
            if (assignee.IsSet)
                return a != null && assignee.Value.Contains(a.Login);
            else if (notAssignee.IsSet)
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
            if (!labels.IsSet)
                return true;

            List<string> lc = lbls.Select(x => x.ToLower()).ToList();

            var par = new Stack<string>(labels.Value.Select(x => x.ToLower()));
            while (par.Count > 0)
            {
                var p = par.Pop();
                if (p.StartsWith("^") && lc.Contains(p.Substring(1)))
                    return false;
                else if (!p.StartsWith("^") && !lc.Contains(p))
                    return false;
            }
            return true;
        }

        #endregion

        protected override void Execute()
        {
            if (create.IsSet)
            {
                NewIssue nIssue = new NewIssue(setTitle.Value.Trim());
                nIssue.Body = setDescription.Value?.Trim();

                if (take.IsSet)
                    nIssue.Assignees.Add(assignUser);

                if (editLabels.IsSet)
                {
                    var allLabels = GitHub.Client.Issue.Labels.GetAllForRepository(GitHub.Username, GitHub.Project).Result.ToArray();
                    string header = string.Format("Set labels for the new issue: {0}", nIssue.Title);
                    var updateLabels = selectLabels(header, allLabels, new string[0]);
                    foreach (var l in updateLabels)
                        nIssue.Labels.Add(l.Name);
                }
                else
                    foreach (var l in setLabels.Value)
                        nIssue.Labels.Add(l);

                var issue = GitHub.Client.Issue.Create(GitHub.Username, GitHub.Project, nIssue).Result;

                issues = new List<Issue>(1) { issue };
            }
            else if (close.IsSet || open.IsSet || take.IsSet || drop.IsSet || setLabels.IsSet || remLabels.IsSet || editLabels.IsSet || setTitle.IsSet || setDescription.IsSet || edit.IsSet)
            {
                var allLabels = editLabels.IsSet ? GitHub.Client.Issue.Labels.GetAllForRepository(GitHub.Username, GitHub.Project).Result.ToArray() : new Label[0];
                for (int i = 0; i < issues.Count; i++)
                {
                    var update = issues[i].ToUpdate();
                    if (take.IsSet) update.AddAssignee(assignUser);
                    else if (drop.IsSet) update.RemoveAssignee(assignUser);

                    if (close.IsSet) update.State = ItemState.Closed;
                    if (open.IsSet) update.State = ItemState.Open;

                    if (setTitle.IsSet || edit.IsSet)
                        update.Title = setTitle.Value.Trim();
                    if (setDescription.IsSet || edit.IsSet)
                        update.Body = setDescription.Value?.Trim();

                    if (editLabels.IsSet)
                    {
                        string header = string.Format("Set labels for issue #{0}: {1}", issues[i].Number, issues[i].Title);
                        var updateLabels = selectLabels(header, allLabels, issues[i].Labels.Select(x => x.Name));

                        update.ClearLabels();
                        foreach (var l in updateLabels)
                            update.AddLabel(l.Name);
                    }
                    else
                    {
                        if (setLabels.Value.Length > 0 || remLabels.Value.Length > 0)
                            foreach (var l in issues[i].Labels)
                                update.AddLabel(l.Name);

                        foreach (var l in setLabels.Value)
                            if (update.Labels == null || !update.Labels.Contains(l))
                                update.AddLabel(l);

                        if (update.Labels != null)
                            foreach (var l in remLabels.Value)
                                update.Labels.Remove(l);
                    }

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

            var formatter = FormatterComposer.Create<Issue>()
                .With("number", x => x.Number)
                    .WithAutoColor(x => x.ClosedAt is null ? "Issue_Open" : "Issue_Closed")
                    .WithPaddedLengthFrom(issues)
                .With("assignee", x => x.Assignee?.Login ?? "")
                    .WithAutoColor(x => x.Assignee?.Login == GitHub.CurrentUser?.Login ? "Issue_User_Self" : "Issue_User")
                    .WithPaddedLengthFrom(issues)
                .With("title", x => x.Title)
                .With("description", x => x.Body)

                .WithListFunction("labels", x => x.Labels, FormatterComposer.Create<Label>()
                    .With("label", x => x.Name)
                        .WithAutoColor(ColorResolver.GetConsoleColor)
                )

                .WithPredicate("labels", x => x.Labels.Count > 0)
                .WithPredicate("assignee", x => x.Assignee != null)
                .WithPredicate("open", x => x.State.Value == ItemState.Open)
                .WithPredicate("closed", x => x.State.Value == ItemState.Closed)
                .WithPredicate("mine", x => x.Assignee?.Login == GitHub.CurrentUser?.Login)
                .WithPredicate("description", x => !string.IsNullOrWhiteSpace(x.Body))

                .GetFormatter();

            var parsedFormat = CommandLineParsing.Output.Formatting.Structure.FormatElement.Parse(outputFormat.Value.Replace("\\n", "\n"));

            foreach (var i in issues)
                ColorConsole.WriteLine(formatter.Format(parsedFormat, i));
        }

        private Label[] selectLabels(string header, Label[] knownLabelNames, IEnumerable<string> preSelected)
        {
            int theader = Console.CursorTop;
            if (header != null)
                ColorConsole.WriteLine(header);

            List<string> pre = new List<string>(preSelected);

            var rr = knownLabelNames.MenuSelectMultiple
            (
                cleanup: MenuCleanup.RemoveMenu,
                onKeySelector: x => "[" + ColorResolver.GetConsoleColor(x) + ":" + x.Name + "]",
                offKeySelector: x => "[DarkGray:" + x.Name + "]",
                selected: x => pre.Contains(x.Name)
            );

            Console.CursorTop = theader;
            Console.WriteLine(new string(' ', header.Length));
            Console.CursorTop = theader;

            return rr;
        }

        private Message titleAndDescriptionFromFile()
        {
            var content = FileEditing.Edit(
$@"# Edit title and description for your issue.
# Lines that start with # are comments - they are disregarded.
# Clearing the file will cancel the current operation.
# The first line is the title of the issue.
{setTitle.Value}
# Remaining lines represent the description for the issue.
{setDescription.Value}",
Config.Default["issues.editor"]);

            setTitle.Value = content.Length == 0 ? null : content[0].Trim();
            setDescription.Value = content.Length <= 1 ? null : string.Join(Environment.NewLine, content, 1, content.Length - 1).Trim();

            Message m = Message.NoError;
            if (setTitle.Value == null)
                m = "An issue cannot have an empty title.";
            else
                m = setTitle.Validator.Validate(setTitle.Value);

            if (!m.IsError && setDescription.Value != null)
                m = setDescription.Validator.Validate(setDescription.Value);

            return m;
        }
    }
}
