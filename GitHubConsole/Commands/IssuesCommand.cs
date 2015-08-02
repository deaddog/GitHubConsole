using CommandLineParsing;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitHubConsole.Commands
{
    public class IssuesCommand : Command
    {
        private RepositoryIssueRequest request = new RepositoryIssueRequest();

        [Name("--format", "-f"), Description("Allows for describing an output format when listing issues.")]
        private readonly Parameter<string> outputFormat = null;

        [Name("--open", "-o"), Description("List issues that are currently open.")]
        private readonly FlagParameter open = null;
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
            outputFormat.SetDefault(Config.Default["issues.format"] ?? "[auto:$+number] [auto:$assignee+] $title ?labels{[DarkYellow:(]@labels{[auto:$label]@,@,}[DarkYellow:)]}");
            assignee.Validator.Add(x => x.Length > 0, "A user must be specified for the " + assignee.Name + " parameter.");
            notAssignee.Validator.Add(x => x.Length > 0, "A user must be specified for the " + assignee.Name + " parameter.");
            labels.Validator.Add(x => x.Length > 0, "At least one label name must be supplied for the " + labels.Name + " parameter.");

            setLabels.Validator.Add(x => x.Length > 0, "You must specify a set of labels to set:\n"
                + "  gihub issues <issues> " + setLabels.Name + " <label1> <label2>...");
            remLabels.Validator.Add(x => x.Length > 0, "You must specify a set of labels to remove:\n"
                + "  gihub issues <issues> " + remLabels.Name + " <label1> <label2>...");

            setTitle.Validator.Add(x => x.Trim().Length > 0, "An issue cannot have an empty title.");

            Validator.AddIfFirstNotRest(assignee, hasAssignee, noAssignee, notAssignee);
            Validator.AddIfFirstNotRest(notAssignee, hasAssignee, noAssignee);

            Validator.AddOnlyOne(edit, create);
            Validator.AddIfFirstNotRest(edit, setTitle, setDescription);

            Validator.AddOnlyOne(editLabels, setLabels);
            Validator.AddOnlyOne(editLabels, remLabels);

            Validator.AddOnlyOne(take, drop);

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
                if (open.IsSet || closed.IsSet || all.IsSet || labels.IsSet || hasAssignee.IsSet || noAssignee.IsSet || assignee.IsSet || notAssignee.IsSet)
                    return "Issue filtering cannot be applied when specifying specific issues or creating new ones.";
            }

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
            if (create.IsSet)
            {
                NewIssue nIssue = new NewIssue(setTitle.Value.Trim());
                nIssue.Body = setDescription.Value.Trim();

                if (take.IsSet)
                    nIssue.Assignee = assignUser;

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
            else if (take.IsSet || drop.IsSet || setLabels.IsSet || remLabels.IsSet || editLabels.IsSet || setTitle.IsSet || setDescription.IsSet || edit.IsSet)
            {
                var allLabels = editLabels.IsSet ? GitHub.Client.Issue.Labels.GetAllForRepository(GitHub.Username, GitHub.Project).Result.ToArray() : new Label[0];
                for (int i = 0; i < issues.Count; i++)
                {
                    var update = issues[i].ToUpdate();
                    if (take.IsSet) update.Assignee = assignUser;
                    else if (drop.IsSet) update.Assignee = null;
                    else update.Assignee = issues[i].Assignee?.Login;

                    if (setTitle.IsSet || edit.IsSet)
                        update.Title = setTitle.Value;
                    if (setDescription.IsSet || edit.IsSet)
                        update.Body = setDescription.Value;

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

            string format = outputFormat.Value.Replace("\\n", "\n");

            IssuePrinter printer = new IssuePrinter(len, namelen, format);
            foreach (var v in issues)
                printer.Print(v);
        }

        private class IssuePrinter : FormattedPrinter
        {
            private Issue issue;
            private Label label;

            private readonly int maxNumberWidth;
            private readonly int maxAssigneeWidth;

            public IssuePrinter(int maxNumberWidth, int maxAssigneeWidth, string format)
                : base(format)
            {
                this.issue = null;
                this.label = null;

                this.maxNumberWidth = maxNumberWidth;
                this.maxAssigneeWidth = maxAssigneeWidth;
            }

            public void Print(Issue issue)
            {
                this.issue = issue;

                string text = Handle();
                ColorConsole.WriteLine(text);
            }

            protected override string getVariable(string variable)
            {
                switch (variable.Substring(1))
                {
                    case "number": return issue.Number.ToString();
                    case "+number": return issue.Number.ToString().PadLeft(maxNumberWidth);
                    case "number+": return issue.Number.ToString().PadRight(maxNumberWidth);

                    case "assignee": return issue.Assignee?.Login ?? "";
                    case "+assignee": return (issue.Assignee?.Login ?? "").PadLeft(maxAssigneeWidth);
                    case "assignee+": return (issue.Assignee?.Login ?? "").PadRight(maxAssigneeWidth);

                    case "title": return issue.Title;
                    case "description": return issue.Body;

                    case "label": return label.Name;

                    default:
                        return variable;
                }
            }
            protected override string getAutoColor(string content)
            {
                switch (content.Substring(1))
                {
                    case "number":
                    case "+number":
                    case "number+":
                        if (issue == null)
                            return string.Empty;
                        else
                            return issue.ClosedAt.HasValue ? "Issue_Closed" : "Issue_Open";

                    case "assignee":
                    case "+assignee":
                    case "assignee+":
                        if (issue == null)
                            return string.Empty;
                        else
                            return (issue?.Assignee?.Login == GitHub.CurrentUser?.Login) ? "Issue_User_Self" : "Issue_User";

                    case "label":
                        if (label == null)
                            return string.Empty;
                        else
                            return ColorResolver.GetConsoleColor(label.Color).ToString();

                    default: return string.Empty;
                }
            }
            
            protected override bool? conditionBlock(string format)
            {
                switch (format.Substring(1))
                {
                    case "labels": return issue.Labels.Count > 0;
                    case "assignee": return issue.Assignee != null;
                    case "description": return issue.Body != null && issue.Body.Length > 0;
                    default: return null;
                }
            }
            protected override string functionBlock(string function, string[] args)
            {
                string def = function + "{" + string.Join("@", args) + "}";
                switch (function.Substring(1))
                {
                    case "labels":
                        if (args.Length == 1)
                            return labelsFunction(args[0], " ", " ");
                        else if (args.Length == 2)
                            return labelsFunction(args[0], args[1], args[1]);
                        else if (args.Length >= 3)
                            return labelsFunction(args[0], args[1], args[2]);
                        else
                            return def;

                    default: return def;
                }
            }

            private string labelsFunction(string format, string separator1, string separator2)
            {
                if (issue.Labels.Count == 0)
                    return string.Empty;

                label = issue.Labels[0];
                string res = Handle(format);

                if (issue.Labels.Count == 1)
                    return res;

                label = issue.Labels[1];
                for (int i = 2; i < issue.Labels.Count; i++)
                {
                    res += separator1 + Handle(format);
                    label = issue.Labels[i];
                }
                return res + separator2 + Handle(format);
            }
        }

        private Label[] selectLabels(string header, Label[] knownLabelNames, IEnumerable<string> preSelected)
        {
            int theader = Console.CursorTop;
            if (header != null)
                ColorConsole.WriteLine(header);

            List<string> pre = new List<string>(preSelected);

            var rr = knownLabelNames.MenuSelectMultiple(new MenuSettings() { Cleanup = MenuCleanup.RemoveMenu },
                x => "[" + ColorResolver.GetConsoleColor(x.Color) + ":" + x.Name + "]",
                x => "[DarkGray:" + x.Name + "]",
                x => pre.Contains(x.Name));

            Console.CursorTop = theader;
            Console.WriteLine(new string(' ', header.Length));
            Console.CursorTop = theader;

            return rr;
        }

        private Message titleAndDescriptionFromFile()
        {
            string filepath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            File.WriteAllText(filepath,
$@"# Edit title and description for your issue.
# Lines that start with # are comments - they are disregarded.
# Clearing the file will cancel the current operation.
# The first line is the title of the issue.
{setTitle.Value}
# Remaining lines represent the description for the issue.
{setDescription.Value}");

            string application = Config.Default["issues.editor"] ?? "%f";

            if (application.Contains("%f"))
                using (var p = System.Diagnostics.Process.Start(application.Replace("%f", filepath)))
                    p.WaitForExit();
            else
                using (var p = System.Diagnostics.Process.Start(application, filepath))
                    p.WaitForExit();

            string[] content = File.ReadAllLines(filepath)
                .Where(x => !x.StartsWith("#"))
                .SkipWhile(x => x.Trim().Length == 0)
                .Reverse()
                .SkipWhile(x => x.Trim().Length == 0)
                .Reverse()
                .ToArray();
            File.Delete(filepath);

            setTitle.Value = content.Length == 0 ? null : content[0];
            setDescription.Value = content.Length <= 1 ? null : string.Join(Environment.NewLine, content, 1, content.Length - 1);

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
