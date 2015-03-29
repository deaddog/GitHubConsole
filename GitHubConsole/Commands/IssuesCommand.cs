using CommandLineParsing;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHubConsole.Commands
{
    public class IssuesCommand : Command
    {
        private bool openArgFound = false;
        private RepositoryIssueRequest request = new RepositoryIssueRequest();
        private Predicate<Issue> validator = null;

        [Name("--format", "-f")]
        private readonly Parameter<string> outputFormat;

        private readonly FlagParameter open, closed, all;

        private readonly Parameter<string[]> labels;

        [Name("--has-assignee")]
        private readonly FlagParameter hasAssignee;
        [Name("--no-assignee")]
        private readonly FlagParameter noAssignee;

        [Name("--assignee")]
        private readonly Parameter<string[]> assignee;
        [Name("--not-assignee")]
        private readonly Parameter<string[]> notAssignee;

        public IssuesCommand()
        {
            outputFormat.SetDefault(Config.Default["issues.format"] ?? "%#% %user% %title% %labels%");
            assignee.Validate(x => x.Length > 0, "A user must be specified for the " + assignee.Name + " argument.");
            labels.Validate(x => x.Length > 0, "At least one label name must be supplied for the " + labels.Name + " argument.");
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

            return base.Validate();
        }

        private RepositoryIssueRequest getRequest()
        {
            var request = new RepositoryIssueRequest();

            request.State = (all.IsSet || (open.IsSet && closed.IsSet)) ? ItemState.All : (closed.IsSet ? ItemState.Closed : ItemState.Open);

            throw new NotImplementedException();
        }
        private bool validateIssue(Issue issue)
        {
            return validateAssignee(issue.Assignee) && validateLabels(issue.Labels.Select(x => x.Name).ToList());
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

        public override void Execute()
        {
            if (GitHub.Client == null)
                return;

            var q = GitHub.Client.Issue.GetForRepository(GitHub.Username, GitHub.Project, getRequest()).Result.Where(x => validateIssue(x)).ToArray();

            listIssues(q);
        }

        private void listIssues(Issue[] issues)
        {
            if (issues.Length == 0)
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

                format.ToConsoleLine(
                    v.Number.ToString().PadLeft(len), v.ClosedAt.HasValue ? "DarkRed" : "DarkYellow",
                    name.PadRight(namelen), name == GitHub.Client.Credentials.Login ? "Cyan" : "DarkCyan",
                    v.Title.Trim(),
                    labels);
            }
        }
    }
}
