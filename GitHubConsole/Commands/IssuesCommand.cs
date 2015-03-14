using GitHubConsole.Commands.Structure;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHubConsole.Commands
{
    public class IssuesCommand : ManagedCommand
    {
        private bool openArgFound = false;
        private RepositoryIssueRequest request = new RepositoryIssueRequest();
        private Predicate<Issue> validator = null;

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
            yield return new ArgumentHandlerPair("--all", x => { request.State = ItemState.All; return true; });
            yield return new ArgumentHandlerPair("--label", handleLabel);
            yield return new ArgumentHandlerPair("--no-assignee", arg => { AndPredicate(x => x.Assignee == null); return true; });
            yield return new ArgumentHandlerPair("--has-assignee", arg => { AndPredicate(x => x.Assignee != null); return true; });
            yield return new ArgumentHandlerPair("--assignee", handleAssignee);
        }

        private bool handleOpen(Argument argument)
        {
            openArgFound = true;
            if (request.State == ItemState.Closed)
                request.State = ItemState.All;
            return true;
        }
        private bool handleClosed(Argument argument)
        {
            if (request.State == ItemState.Open)
                request.State = openArgFound ? ItemState.All : ItemState.Closed;
            return true;
        }

        private bool handleLabel(Argument argument)
        {

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
            return true;
        }

        private bool handleAssignee(Argument argument)
        {
            if (argument.Count == 0)
            {
                Console.WriteLine("A user must be specified for the -assignee argument.");
                return false;
            }
            else if (argument.Count > 1)
            {
                Console.WriteLine("Only one user can be specified for the -assignee argument.");
                return false;
            }
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
            return true;
        }

        public override void Execute()
        {
            if (GitHub.Client == null)
                return;

            var q = GitHub.Client.Issue.GetForRepository(GitHub.Username, GitHub.Project, request).Result;
            if (q.Count == 0)
                return;

            int len = q[0].Number.ToString().Length;
            int namelen = q.Count == 0 ? 0 : (from v in q
                                              let n = v.Assignee == null ? "" : v.Assignee.Login
                                              select n.Length).Max();
            foreach (var v in q)
            {
                if (validator != null && !validator(v))
                    continue;

                string name = v.Assignee == null ? "" : v.Assignee.Login;

                "[[:{1}:{0}]] [[:{3}:{2}]] {4}".ToConsole(
                    v.Number.ToString().PadLeft(len), v.ClosedAt.HasValue ? "DarkRed" : "DarkYellow",
                    name.PadRight(namelen), name == GitHub.Client.Credentials.Login ? "Cyan" : "DarkCyan",
                    v.Title);

                if (v.Labels.Count > 0)
                {
                    " [[:DarkYellow:(]]{0}[[:DarkYellow:)]]".ToConsole(
                        string.Join(", ", v.Labels.Select(l => "[[:" + ColorResolver.GetConsoleColor(l.Color) + ":" + l.Name + "]]")));
                }

                Console.WriteLine();
            }
        }
    }
}
