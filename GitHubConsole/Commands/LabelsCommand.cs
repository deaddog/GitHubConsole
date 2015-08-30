using CommandLineParsing;
using Octokit;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitHubConsole.Commands
{
    [Description("Lists, modifies, creates and deletes github labels for this repository")]
    public class LabelsCommand : Command
    {
        #region Colors

        private static class LabelColors
        {
            private static Random rnd = new Random();
            private static string[] defaultColors = new string[]
            {
                "e11d21",
                "eb6420",
                "fbca04",
                "009800",
                "006b75",
                "207de5",
                "0052cc",
                "5319e7",
                "f7c6c7",
                "fad8c7",
                "fef2c0",
                "bfe5bf",
                "bfdadc",
                "c7def8",
                "bfd4f2",
                "d4c5f9"
            };

            public static string GetUnusedOrRandom(Func<string, bool> inUse)
            {
                return defaultColors.FirstOrDefault(x => !inUse(x)) ?? defaultColors[rnd.Next(defaultColors.Length)];
            }
        }

        #endregion

        #region Labels

        private static class ExistingLabels
        {
            private static Label[] labels;

            static ExistingLabels()
            {
                labels = GitHub.Client.Issue.Labels.GetAllForRepository(GitHub.Username, GitHub.Project).Result.ToArray();
            }

            public static bool Exists(string labelname)
            {
                return labels.Any(x => x.Name.Equals(labelname, StringComparison.InvariantCultureIgnoreCase));
            }
            public static Label Find(string labelname)
            {
                return labels.FirstOrDefault(x => x.Name.Equals(labelname, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        #endregion

        [Description("Sets the name of a label.")]
        private readonly Parameter<string> name;
        [Description("Sets the color of a label.")]
        private readonly Parameter<string> color;

        [NoName]
        private readonly Parameter<string[]> labels;

        private Label[] _allLabels = null;
        private Label[] allLabels => _allLabels ?? (_allLabels = GitHub.Client.Issue.Labels.GetAllForRepository(GitHub.Username, GitHub.Project).Result.ToArray());

        public LabelsCommand()
        {
            name.Validator.Add(x => !ExistingLabels.Exists(x), x => $"A label called {x} already exists.");
            color.Validator.Add(x => x.Length == 0 || Regex.IsMatch(x, "#?[0-9a-f]{6}") || ExistingLabels.Exists(x), "You must specify a valid hex color value (or the name of an existing label).");
            color.Callback += () => color.Value = color.Value.TrimStart('#');

            labels.Validator.AddForeach(x => ExistingLabels.Exists(x), x => $"The label {x} does not exist.");

            Validator.Add(() => name.IsSet && labels.Value.Length > 1 ? "Label name can only be set for a single label." : Message.NoError);
        }

        protected override Message GetHelpMessage()
        {
            return
                "Provides information about and operations on labels on GitHub.com.\n" +
                "To modify a specific label use:\n\n" +
                $"  [Example:github labels bug {name.Name} <newname> {color.Name} <newcolor>]\n\n" +
                "Below is a complete list of all parameters for this command:" +

            base.GetParametersMessage(2);
        }

        private bool ColorInUse(string color)
        {
            return allLabels.Any(x => x.Color.Equals(color, StringComparison.InvariantCultureIgnoreCase));
        }

        protected override void Execute()
        {
            if (name.IsSet || color.IsSet)
            {
                foreach (var n in labels.Value)
                {
                    var l = ExistingLabels.Find(n);
                    var l2 = GitHub.Client.Issue.Labels.Update(GitHub.Username, GitHub.Project, l.Name,
                        new LabelUpdate(name.IsSet ? name.Value : l.Name, color.IsSet ? color.Value : l.Color)).Result;
                    ColorConsole.WriteLine($"Updated [{ColorResolver.GetConsoleColor(l)}:{l.Name}] -> [{ColorResolver.GetConsoleColor(l2)}:{l2.Name}].");
                }
            }
            else
                foreach (var l in allLabels)
                    ColorConsole.WriteLine($"[{ColorResolver.GetConsoleColor(l)}:{l.Name}]");
        }
    }
}
