﻿using CommandLineParsing;

namespace GitHubConsole.Commands
{
    public class GoCommand : Command
    {
        protected override void Execute()
        {
            string url = string.Format("https://github.com/{0}/{1}", GitHub.Username, GitHub.Project) + getPage();
            System.Diagnostics.Process.Start(url);
        }

        private string getPage()
        {
            return string.Empty;
        }
    }
}
