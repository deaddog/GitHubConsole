using GitHubConsole.Commands.Structure;
using Octokit;
using System.Collections.Generic;
using System.Linq;

namespace GitHubConsole.Commands
{
    public class IssuesCreateCommand : ManagedCommand
    {
        private string title = null;
        private List<string> labels = new List<string>();

        protected override IEnumerable<ArgumentHandlerPair> LoadArgumentHandlers()
        {
            yield return new ArgumentHandlerPair("--title", handleTitle);
            yield return new ArgumentHandlerPair("--labels", handleLabel);
        }

        private bool handleTitle(Argument argument)
        {
            if (argument.Count != 1)
            {
                ColorConsole.ToConsoleLine("Only one issue title can be specified.");
                return false;
            }

            title = argument[0].Trim();
            return true;
        }
        private bool handleLabel(Argument argument)
        {
            for (int i = 0; i < argument.Count; i++)
                labels.Add(argument[i]);

            return true;
        }

        public override bool ValidateState()
        {
            if (title == null || title.Length == 0)
                return false;

            var knownLabels = GitHub.Client.Issue.Labels.GetForRepository(GitHub.Username, GitHub.Project).Result.Select(x => x.Name).ToList();
            foreach (var l in labels)
                if (!knownLabels.Contains(l))
                {
                    ColorConsole.ToConsoleLine("Unknown label [[:Red:{0}]].", l);
                    return false;
                }

            return base.ValidateState();
        }

        public override void Execute()
        {
            NewIssue issue = new NewIssue(title);
            foreach (var l in labels)
                issue.Labels.Add(l);

            var iss = GitHub.Client.Issue.Create(GitHub.Username, GitHub.Project, issue).Result;
        }
    }
}
