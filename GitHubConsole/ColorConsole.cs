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

            colorRegex = new Regex(@"\[\[:(?<color>" + namesRegex + @"):([^\]]|\][^\]])+\]\]");
        }

        public static void ToConsole(this string format, params object[] args)
        {
            handle(format, false, args);
        }

        public static void ToConsoleLine(this string format, params object[] args)
        {
            handle(format, true, args);
        }

        private static void handle(string format, bool newline, params object[] args)
        {
            var m = colorRegex.Match(format);
            if (m.Success)
            {
                string pre = format.Substring(0, m.Index);
                string post = format.Substring(m.Index + m.Length);

                string content = m.Value.Remove(0, 4 + m.Groups["color"].Length);
                content = content.Remove(content.Length - 2);

                Console.Write(pre, args);
                var color = getColor(m.Groups["color"].Value);
                Console.ForegroundColor = color;
                Console.Write(content, args);
                Console.ResetColor();

                ToConsoleLine(post, args);
            }
            else if (newline)
                Console.WriteLine(format, args);
            else
                Console.Write(format, args);
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
