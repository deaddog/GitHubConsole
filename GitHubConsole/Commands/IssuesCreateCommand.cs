using GitHubConsole.Commands.Structure;
using GitHubConsole.Messages;
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

        private ErrorMessage handleTitle(Argument argument)
        {
            if (argument.Count != 1)
                return new ErrorMessage("Only one issue title can be specified.");

            title = argument[0].Trim();
            return ErrorMessage.NoError;
        }
        private ErrorMessage handleLabel(Argument argument)
        {
            for (int i = 0; i < argument.Count; i++)
                labels.Add(argument[i]);

            return ErrorMessage.NoError;
        }

        public override ErrorMessage ValidateState()
        {
            if (title == null || title.Length == 0)
                return new ErrorMessage("Issue must have a title. You must specify the [[:Yellow:--title <title>]] argument.");

            var knownLabels = GitHub.Client.Issue.Labels.GetForRepository(GitHub.Username, GitHub.Project).Result.ToList();
            var knownLabelNames = knownLabels.Select(x => x.Name).ToList();

            foreach (var l in labels)
                if (!knownLabelNames.Contains(l))
                {
                    string lblString = string.Format(string.Join("", knownLabels.Select(lbl => "\n  [[:" + ColorResolver.GetConsoleColor(lbl.Color) + ":" + lbl.Name + "]]")));
                    return new ErrorMessage("Unknown label [[:Red:{0}]]. Valid label names are:{1}", l, lblString);
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
