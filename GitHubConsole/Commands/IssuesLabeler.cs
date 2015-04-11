﻿using CommandLineParsing;
using Octokit;
using System.Collections.Generic;
using System.Linq;

namespace GitHubConsole.Commands
{
    public class IssuesLabeler : Command
    {
        [NoName]
        private readonly Parameter<int[]> issues = null;

        [Description("Adds a set of labels selected issues.")]
        private readonly Parameter<string[]> add = null;
        [Description("Removes a set of labels from selected issues.")]
        private readonly Parameter<string[]> remove = null;

        public IssuesLabeler()
        {
            issues.Validator.AddForeach(x => x > 0, x => "Issue [Red:#" + x + "] is invalid.");
        }

        protected override Message Validate()
        {
            if (add.Value.Length == 0 && remove.Value.Length == 0)
                return "You must specify whether to add or remove labels:\n" +
                    "  gihub issues labels <issues> --add <label1> <label2>..." +
                    "  gihub issues labels <issues> --remove <label1> <label2>...";

            return base.Validate();
        }

        protected override void Execute()
        {
            GitHubClient client = GitHub.Client;
            if (client == null)
                return;

            List<Label> setLabels = new List<Label>();
            List<Label> remLabels = new List<Label>();

            var labels = client.Issue.Labels.GetForRepository(GitHub.Username, GitHub.Project).Result;
            foreach (var s in add.Value)
            {
                var l = labels.FirstOrDefault(x => x.Name == s);
                if (l != null) setLabels.Add(l);
            }
            foreach (var r in remove.Value)
            {
                var l = labels.FirstOrDefault(x => x.Name == r);
                if (l != null) remLabels.Add(l);
            }

            foreach (var number in issues.Value)
            {
                var issue = client.Issue.Get(GitHub.Username, GitHub.Project, number).Result;
                if (issue == null)
                {
                    ColorConsole.WriteLine("Unknown issue [DarkRed:#{0}].", number);
                    continue;
                }

                var update = issue.ToUpdate();
                if (update.Assignee != null)
                    update.Assignee = issue.Assignee.Login;

                foreach (var l in setLabels)
                    if (update.Labels == null || !update.Labels.Contains(l.Name))
                        update.AddLabel(l.Name);

                if (update.Labels != null)
                    foreach (var l in remLabels)
                        update.Labels.Remove(l.Name);

                client.Issue.Update(GitHub.Username, GitHub.Project, number, update).Wait();
            }
        }
    }
}
