using CommandLineParsing;
using Octokit;
using System.Linq;

namespace GitHubConsole.Commands
{
    [Description("Lists, modifies, creates and deletes github labels for this repository")]
    public class LabelsCommand : Command
    {
        private Label[] _allLabels = null;
        private Label[] allLabels => _allLabels ?? (_allLabels = GitHub.Client.Issue.Labels.GetAllForRepository(GitHub.Username, GitHub.Project).Result.ToArray());

        public LabelsCommand()
        {
        }

        protected override void Execute()
        {
            foreach (var l in allLabels)
                ColorConsole.WriteLine($"[{ColorResolver.GetConsoleColor(l)}:{l.Name}]");
        }
    }
}
