using Octokit;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GitHubConsole
{
    public static class ColorResolver
    {
        public static string GetConsoleColor(Label label)
        {
            if (label == null)
                throw new ArgumentNullException(nameof(label));
            
            return Config.Default["labels.color." + label.Name] ?? GetConsoleColor(ColorTranslator.FromHtml("#ff" + label.Color));
        }
        public static string GetConsoleColor(Color color)
        {
            double closest = double.PositiveInfinity;
            ConsoleColor res = ConsoleColor.Gray;

            foreach (var c in getSaturation(color) < 0.2 ? grays : colors)
            {
                double dist = manhattanDistance(getColor(c), color);
                if (dist < closest)
                {
                    closest = dist;
                    res = c;
                }
            }

            return res.ToString();
        }

        private static IEnumerable<ConsoleColor> colors
        {
            get
            {
                yield return ConsoleColor.DarkBlue;
                yield return ConsoleColor.DarkGreen;
                yield return ConsoleColor.DarkCyan;
                yield return ConsoleColor.DarkRed;
                yield return ConsoleColor.DarkMagenta;
                yield return ConsoleColor.DarkYellow;
                yield return ConsoleColor.Blue;
                yield return ConsoleColor.Green;
                yield return ConsoleColor.Cyan;
                yield return ConsoleColor.Red;
                yield return ConsoleColor.Magenta;
                yield return ConsoleColor.Yellow;
            }
        }
        private static IEnumerable<ConsoleColor> grays
        {
            get
            {
                yield return ConsoleColor.Black;
                yield return ConsoleColor.DarkGray;
                yield return ConsoleColor.Gray;
                yield return ConsoleColor.White;
            }
        }

        private static double getSaturation(Color color)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            return (max == 0) ? 0 : 1.0 - (1.0 * min / max);
        }
        private static Color getColor(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Black: return Color.FromArgb(0, 0, 0);
                case ConsoleColor.Gray: return Color.FromArgb(192, 192, 192);
                case ConsoleColor.DarkGray: return Color.FromArgb(128, 128, 128);
                case ConsoleColor.White: return Color.FromArgb(255, 255, 255);

                case ConsoleColor.DarkBlue: return Color.FromArgb(0, 0, 128);
                case ConsoleColor.DarkGreen: return Color.FromArgb(0, 128, 0);
                case ConsoleColor.DarkCyan: return Color.FromArgb(0, 128, 128);
                case ConsoleColor.DarkRed: return Color.FromArgb(128, 0, 0);
                case ConsoleColor.DarkMagenta: return Color.FromArgb(128, 0, 128);
                case ConsoleColor.DarkYellow: return Color.FromArgb(128, 128, 0);
                case ConsoleColor.Blue: return Color.FromArgb(0, 0, 255);
                case ConsoleColor.Green: return Color.FromArgb(0, 255, 0);
                case ConsoleColor.Cyan: return Color.FromArgb(0, 255, 255);
                case ConsoleColor.Red: return Color.FromArgb(255, 0, 0);
                case ConsoleColor.Magenta: return Color.FromArgb(255, 0, 255);
                case ConsoleColor.Yellow: return Color.FromArgb(255, 255, 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(color));
            }
        }

        private static double manhattanDistance(Color a, Color b)
        {
            return Math.Abs(a.A - b.A) + Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
        }
    }
}
