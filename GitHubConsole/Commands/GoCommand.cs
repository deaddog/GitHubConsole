using CommandLineParsing;

namespace GitHubConsole.Commands
{
    [Description("Launches various links to GitHub.com in your default browser")]
    public class GoCommand : Command
    {
        private const string GITHUBCOM = "Github.com";

        [Description("Opens the issues page on " + GITHUBCOM + " for the current project.")]
        private readonly FlagParameter issues = null;
        [Name("--issue", "-i"), Description("Opens a single issue page on " + GITHUBCOM + ".")]
        private readonly Parameter<int> issue = null;

        [Description("Opens the labels page on " + GITHUBCOM + " for the current project.")]
        private readonly FlagParameter labels = null;
        [Description("Opens the wiki page on " + GITHUBCOM + " for the current project.")]
        private readonly FlagParameter wiki = null;

        public GoCommand()
        {
            PreValidator.Add(GitHub.ValidateGitDirectory);
            Validator.Ensure.ZeroOrOne(issues, issue, labels, wiki);
        }

        protected override void Execute()
        {
            string url = string.Format("https://github.com/{0}/{1}", GitHub.Username, GitHub.Project) + getPage();
            System.Diagnostics.Process.Start(url);
        }

        private string getPage()
        {
            if (issues.IsSet)
                return "/issues";
            if (issue.IsSet)
                return "/issues/" + issue.Value;
            if (labels.IsSet)
                return "/labels";
            if (wiki.IsSet)
                return "/wiki";
            return string.Empty;
        }
    }
}
