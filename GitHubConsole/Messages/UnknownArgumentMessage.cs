using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubConsole.Messages
{
    public class UnknownArgumentMessage : ErrorMessage
    {
        private string argument;
        private List<string> alternativeArguments;

        public UnknownArgumentMessage(string argument)
        {
            this.argument = argument;
            this.alternativeArguments = new List<string>();
        }

        public void AddAlternative(string alternative)
        {
            if (!this.alternativeArguments.Contains(alternative))
                this.alternativeArguments.Add(alternative);
        }
        public void AddAlternatives(IEnumerable<string> alternatives)
        {
            foreach (var a in alternatives)
                AddAlternative(a);
        }

        public override string GetMessage()
        {
            string message = string.Format("Argument [[:Yellow:{0}]] was not recognized. Did you mean any of the following:", argument);
            foreach (var a in alternativeArguments.OrderByDistance(argument).TakeWhile((arg, i) => i == 0 || arg.Item2 < 5))
                message += "\n  " + a.Item1;
            return message;
        }
        public void PrintMessage()
        {
            throw new NotImplementedException();
        }
    }
}
