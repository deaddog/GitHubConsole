﻿using System.Text.RegularExpressions;

namespace GitHubConsole
{
    public abstract class FormattedPrinter
    {
        private readonly string format;

        public FormattedPrinter(string format)
        {
            this.format = format;
        }

        protected string Handle()
        {
            return Handle(format);
        }
        protected string Handle(string text)
        {
            int index = 0;

            while (index < text.Length)
                switch (text[index])
                {
                    case '[': // Coloring
                        {
                            int end = findEnd(text, index, '[', ']');
                            var block = text.Substring(index + 1, end - index - 1);
                            string replace = colorBlock(block);
                            text = text.Substring(0, index) + replace + text.Substring(end + 1);
                            index += replace.Length;
                        }
                        break;

                    case '?': // Conditional
                        {
                            var match = Regex.Match(text.Substring(index), @"\?[^\{]*");
                            var end = findEnd(text, index + match.Value.Length, '{', '}');
                            var block = text.Substring(index + match.Value.Length + 1, end - index - match.Value.Length - 1);

                            string replace = "";
                            var condition = conditionBlock(match.Value);
                            if (!condition.HasValue)
                                replace = match.Value + "{" + Handle(block) + "}";
                            else if (condition.Value)
                                replace = Handle(block);

                            text = text.Substring(0, index) + replace + text.Substring(end + 1);
                            index += replace.Length;
                        }
                        break;

                    case '@': // Listing/Function
                        {
                            var match = Regex.Match(text.Substring(index), @"\@[^\{]*");
                            var end = findEnd(text, index + match.Value.Length, '{', '}');
                            var block = text.Substring(index + match.Value.Length + 1, end - index - match.Value.Length - 1);
                            string replace = functionBlock(match.Value, block.Split('@'));
                            text = text.Substring(0, index) + replace + text.Substring(end + 1);
                            index += replace.Length;
                        }
                        break;

                    case '$': // Variable
                        {
                            var match = Regex.Match(text.Substring(index), @"^\$[^ ]*");
                            var end = match.Index + index + match.Length;
                            var block = match.Value;
                            string replace = getVariable(block);
                            text = text.Substring(0, index) + replace + text.Substring(end);
                            index += replace.Length;
                        }
                        break;

                    default: // Skip content
                        index = text.IndexOfAny(new char[] { '[', '?', '@', '$' }, index);
                        if (index < 0) index = text.Length;
                        break;
                }

            return text;
        }

        protected virtual string getVariable(string variable)
        {
            return variable;
        }
        protected virtual string getAutoColor(string content)
        {
            return string.Empty;
        }

        private string colorBlock(string format)
        {
            Match m = Regex.Match(format, "^(?<color>[^:]+):(?<content>.*)$", RegexOptions.Singleline);
            if (!m.Success)
                return null;

            string color_str = m.Groups["color"].Value;
            string content = m.Groups["content"].Value;

            if (color_str.ToLower() == "auto")
            {
                Match autoColor = Regex.Match(content, @"\$[^ ]+");

                if (autoColor.Success)
                    color_str = getAutoColor(autoColor.Value) ?? string.Empty;
                else
                    color_str = string.Empty;
            }

            return $"[{color_str}:{Handle(content)}]";
        }
        protected virtual bool? conditionBlock(string format)
        {
            return null;
        }
        protected virtual string functionBlock(string function, string[] args)
        {
            return function + "{" + string.Join("@", args) + "}";
        }

        private int findEnd(string text, int index, char open, char close)
        {
            int count = 0;
            do
            {
                if (text[index] == open) count++;
                else if (text[index] == close) count--;
                index++;
            } while (count > 0 && index < text.Length);
            if (count == 0) index--;

            return index;
        }
    }
}
