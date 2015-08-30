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

            public static string GetUnusedOrRandom()
            {
                return defaultColors.FirstOrDefault(x => !ExistingLabels.Exists(x)) ?? defaultColors[rnd.Next(defaultColors.Length)];
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

        [Description("Creates a new label.")]
        private readonly FlagParameter create;

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

        protected override void Execute()
        {
            if (create.IsSet)
                CreateLabel();
            else if (name.IsSet || color.IsSet)
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

        private void CreateLabel()
        {
            if (!name.IsSet)
                name.Value = ColorConsole.ReadLine("Label name: ", validator: name.Validator);
            name.Value = name.Value?.Trim();

            if (name.Value == null || name.Value.Length == 0)
            {
                ColorConsole.WriteLine("No name specified. Aborting.");
                return;
            }

            if (!name.IsSet && !color.IsSet)
            {
                ColorConsole.WriteLine("\nSpecify a label color (in hex-form) or the name of another label from which color should be copied.");
                ColorConsole.WriteLine("Leave this empty to select a random color.");
                color.Value = ColorConsole.ReadLine("Label color: ", validator: color.Validator);
            }
            if (color.Value == null || color.Value == string.Empty)
                color.Value = LabelColors.GetUnusedOrRandom();
            else if (!Regex.IsMatch(color.Value, "#?[0-9a-f]{6}"))
                color.Value = allLabels.First(x => x.Name == color.Value).Color;

            var l = GitHub.Client.Issue.Labels.Create(GitHub.Username, GitHub.Project, new NewLabel(name.Value, color.Value)).Result;
            ColorConsole.WriteLine($"Created label [{ColorResolver.GetConsoleColor(l)}:{l.Name}].");
        }
    }
}
