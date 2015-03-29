using CommandLineParsing;
using Octokit;
using System.Collections.Generic;
using System.Linq;

namespace GitHubConsole.Commands
{
    public class IssuesCreateCommand : Command
    {
        [Name("--title", "-t"), Required("A title is required for an issue.")]
        private readonly Parameter<string> title;
        [Name("--labels", "-l")]
        private readonly Parameter<string[]> labels;

        public IssuesCreateCommand()
        {
            title.Validate(x => x.Trim().Length > 0, "An issue cannot be created with an empty title.");
        }

        protected override Message Validate()
        {
            var knownLabels = GitHub.Client.Issue.Labels.GetForRepository(GitHub.Username, GitHub.Project).Result.ToList();
            var knownLabelNames = knownLabels.Select(x => x.Name).ToList();

            foreach (var l in labels.Value)
                if (!knownLabelNames.Contains(l))
                {
                    string lblString = string.Format(string.Join("", knownLabels.Select(lbl => "\n  [[:" + ColorResolver.GetConsoleColor(lbl.Color) + ":" + lbl.Name + "]]")));
                    return string.Format("Unknown label [[:Red:{0}]]. Valid label names are:{1}", l, lblString);
                }

            return base.Validate();
        }

        public override void Execute()
        {
            NewIssue issue = new NewIssue(title.Value.Trim());
            foreach (var l in labels.Value)
                issue.Labels.Add(l);

            var iss = GitHub.Client.Issue.Create(GitHub.Username, GitHub.Project, issue).Result;
        }
    }
}
