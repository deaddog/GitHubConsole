using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitHubConsole
{
    public static class ColorConsole
    {
        private static readonly Regex colorRegex;

        static ColorConsole()
        {
            var names = Enum.GetNames(typeof(ConsoleColor));
            string namesRegex = string.Join("|", names);

            colorRegex = new Regex(@"\[\[:(?<color>" + namesRegex + @"):([^\]]|\][^\]])*\]\]");
        }

        public static void ToConsole(this string format, params object[] args)
        {
            handle(string.Format(format, args), false);
        }

        public static void ToConsoleLine(this string format, params object[] args)
        {
            handle(string.Format(format, args), true);
        }

        private static void handle(string input, bool newline)
        {
            var m = colorRegex.Match(input);
            if (m.Success)
            {
                string pre = input.Substring(0, m.Index);
                string post = input.Substring(m.Index + m.Length);

                string content = m.Value.Remove(0, 4 + m.Groups["color"].Length);
                content = content.Remove(content.Length - 2);

                Console.Write(pre);
                var color = getColor(m.Groups["color"].Value);
                Console.ForegroundColor = color;
                Console.Write(content);
                Console.ResetColor();

                handle(post, newline);
            }
            else if (newline)
                Console.WriteLine(input);
            else
                Console.Write(input);
        }

        private static ConsoleColor getColor(string color)
        {
            ConsoleColor c;
            if (!Enum.TryParse(color, out c))
                throw new ArgumentException("Unknown console color: " + color);
            else
                return c;
        }
    }
}
