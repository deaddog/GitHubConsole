using CommandLineParsing;

namespace GitHubConsole.Commands
{
    public class GoCommand : Command
    {
        private readonly FlagParameter issues = null;
        [Name("--issue", "-i")]
        private readonly Parameter<int> issue = null;

        private readonly FlagParameter labels = null;
        private readonly FlagParameter wiki = null;

        public GoCommand()
        {
            Validator.AddOnlyOne(issues, issue, labels, wiki);
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
