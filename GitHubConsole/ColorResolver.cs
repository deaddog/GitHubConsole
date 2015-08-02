using Octokit;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GitHubConsole
{
    public static class ColorResolver
    {
        public static ConsoleColor GetConsoleColor(Label label)
        {
            if (label == null)
                throw new ArgumentNullException(nameof(label));

            return GetConsoleColor(ColorTranslator.FromHtml("#ff" + label.Color));
        }
        public static ConsoleColor GetConsoleColor(Color color)
        {
            double closest = double.PositiveInfinity;
            ConsoleColor res = ConsoleColor.Gray;

            foreach (var c in consoleColors)
            {
                double dist = manhattanDistance(getColor(c), color);
                if (dist < closest)
                {
                    closest = dist;
                    res = c;
                }
            }

            return res;
        }

        private static IEnumerable<ConsoleColor> consoleColors
        {
            get
            {
                foreach (ConsoleColor c in Enum.GetValues(typeof(ConsoleColor)))
                {
                    switch (c)
                    {
                        case ConsoleColor.Black:
                        case ConsoleColor.DarkGray:
                        case ConsoleColor.Gray:
                        case ConsoleColor.White:
                            continue;
                    }
                    yield return c;
                }
            }
        }

        private static Color getColor(ConsoleColor color)
        {
            return Color.FromName(color.ToString());
        }

        private static double manhattanDistance(Color a, Color b)
        {
            return Math.Abs(a.A - b.A) + Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
        }
    }
}
