﻿using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubConsole.Commands
{
    public class IssuesLabeler : ManagedCommand
    {
        private List<int> issues = new List<int>();
        private List<string> set = new List<string>();
        private List<string> remove = new List<string>();

        public override void Execute()
        {
            GitHubClient client = GitHub.Client;
            if (client == null)
                return;

            List<Label> setLabels = new List<Label>();
            List<Label> remLabels = new List<Label>();

            var labels = client.Issue.Labels.GetForRepository(GitHub.Username, GitHub.Project).Result;
            foreach (var s in set)
            {
                var l = labels.FirstOrDefault(x => x.Name == s);
                if (l != null) setLabels.Add(l);
            }
            foreach (var r in remove)
            {
                var l = labels.FirstOrDefault(x => x.Name == r);
                if (l != null) remLabels.Add(l);
            }

            foreach (var number in issues)
            {
                var issue = client.Issue.Get(GitHub.Username, GitHub.Project, number).Result;
                if (issue == null)
                {
                    "Unknown issue [[:DarkRed:#{0}]].".ToConsoleLine(number);
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

        protected override IEnumerable<Tuple<string, Func<Argument, bool>>> LoadArgumentHandlers()
        {
            yield return new Tuple<string, Func<Argument, bool>>("-set", handleSet);
            yield return new Tuple<string, Func<Argument, bool>>("-remove", handleRemove);
        }

        public override bool HandleArgumentFallback(Argument argument)
        {
            int number;
            if (int.TryParse(argument.Key, out number))
            {
                if (number < 0)
                {
                    ColorConsole.ToConsoleLine("Issue [[:Red:#{0}]] is invalid. Issues must be 1, 2, 3...", number);
                    return false;
                }
                else
                {
                    issues.Add(number);
                    return true;
                }
            }
            else
                return base.HandleArgumentFallback(argument);
        }

        private bool handleSet(Argument argument)
        {
            for (int i = 0; i < argument.Count; i++)
            {
                set.Add(argument[i]);
                if (argument[i].Contains('_'))
                    set.Add(argument[i].Replace('_', ' '));
            }
            return true;
        }
        private bool handleRemove(Argument argument)
        {
            for (int i = 0; i < argument.Count; i++)
            {
                remove.Add(argument[i]);
                if (argument[i].Contains('_'))
                    remove.Add(argument[i].Replace('_', ' '));
            }
            return true;
        }
    }
}
