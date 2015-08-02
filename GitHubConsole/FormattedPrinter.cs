using System.Text.RegularExpressions;

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
                            var condition = ValidateCondition(match.Value.Substring(1));
                            if (!condition.HasValue)
                                replace = "?" + match.Value + "{" + Handle(block) + "}";
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
                            string replace = EvaluateFunction(match.Value.Substring(1), block.Split('@'));
                            text = text.Substring(0, index) + replace + text.Substring(end + 1);
                            index += replace.Length;
                        }
                        break;

                    case '$': // Variable
                        {
                            var match = Regex.Match(text.Substring(index), @"^\$[^ ]*");
                            var end = match.Index + index + match.Length;
                            string replace = GetVariable(match.Value.Substring(1));
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

        protected virtual string GetVariable(string variable)
        {
            return "$" + variable;
        }
        protected virtual string GetAutoColor(string variable)
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
                    color_str = GetAutoColor(autoColor.Value.Substring(1)) ?? string.Empty;
                else
                    color_str = string.Empty;
            }

            return $"[{color_str}:{Handle(content)}]";
        }
        protected virtual bool? ValidateCondition(string condition)
        {
            return null;
        }
        protected virtual string EvaluateFunction(string function, string[] args)
        {
            return "@" + function + "{" + string.Join("@", args) + "}";
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
