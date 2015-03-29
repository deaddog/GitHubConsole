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

        public IssuesCommand()
        {
            outputFormat.SetDefault(Config.Default["issues.format"] ?? "%#% %user% %title% %labels%");
        }

        private void AndPredicate(Func<Issue, bool> func)
        {
            var old = validator;
            validator = null;

            if (old == null)
                validator = x => func(x);
            else
                validator = x => (old(x) && func(x));
        }
        private void OrPredicate(Func<Issue, bool> func)
        {
            var old = validator;
            validator = null;

            if (old == null)
                validator = x => func(x);
            else
                validator = x => (old(x) || func(x));
        }

        protected override IEnumerable<ArgumentHandlerPair> LoadArgumentHandlers()
        {
            yield return new ArgumentHandlerPair("--open", handleOpen);
            yield return new ArgumentHandlerPair("--closed", handleClosed);
            yield return new ArgumentHandlerPair("--all", "-a", x => { request.State = ItemState.All; return ErrorMessage.NoError; });
            yield return new ArgumentHandlerPair("--label", handleLabel);
            yield return new ArgumentHandlerPair("--no-assignee", arg => { AndPredicate(x => x.Assignee == null); return ErrorMessage.NoError; });
            yield return new ArgumentHandlerPair("--has-assignee", arg => { AndPredicate(x => x.Assignee != null); return ErrorMessage.NoError; });
            yield return new ArgumentHandlerPair("--assignee", "-u", handleAssignee);
        }

        private ErrorMessage handleOpen(Argument argument)
        {
            openArgFound = true;
            if (request.State == ItemState.Closed)
                request.State = ItemState.All;
            return ErrorMessage.NoError;
        }
        private ErrorMessage handleClosed(Argument argument)
        {
            if (request.State == ItemState.Open)
                request.State = openArgFound ? ItemState.All : ItemState.Closed;
            return ErrorMessage.NoError;
        }

        private ErrorMessage handleLabel(Argument argument)
        {
            if (argument.Count == 0)
                return new ErrorMessage("At least one label name must be supplied for the {0} argument.", argument.Key);

            for (int i = 0; i < argument.Count; i++)
            {
                var arg = argument[i];
                var argReplace = argument[i].Replace('_', ' ');

                if (arg.StartsWith("^"))
                {
                    arg = arg.Substring(1);
                    argReplace = argReplace.Substring(1);
                    AndPredicate(x => !x.Labels.Any(l => l.Name == arg || l.Name == argReplace));
                }
                else
                {
                    if (arg != argReplace)
                        AndPredicate(x => x.Labels.Any(l => l.Name == arg || l.Name == argReplace));
                    else
                        request.Labels.Add(arg);
                }
            }
            return ErrorMessage.NoError;
        }
        private ErrorMessage handleAssignee(Argument argument)
        {
            if (argument.Count == 0)
                return new ErrorMessage("A user must be specified for the -assignee argument.");
            else if (argument.Count > 1)
                return new ErrorMessage("Only one user can be specified for the -assignee argument.");
            else
            {
                var arg = argument[0];
                var argReplace = argument[0].Replace('_', ' ');

                if (arg.StartsWith("^"))
                {
                    arg = arg.Substring(1);
                    argReplace = argReplace.Substring(1);
                    AndPredicate(x => x.Assignee == null || x.Assignee.Login != arg || x.Assignee.Login != argReplace);
                }
                else
                    AndPredicate(x => x.Assignee != null && (x.Assignee.Login == arg || x.Assignee.Login == argReplace));
            }
            return ErrorMessage.NoError;
        }

        private RepositoryIssueRequest getRequest()
        {
            throw new NotImplementedException();
        }
        private bool validateIssue(Issue issue)
        {
            throw new NotImplementedException();
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
