using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubConsole
{
    public class ArgumentHandlerPair
    {
        private string key;
        private string[] aliases;
        private ArgumentHandler handler;

        public ArgumentHandlerPair(string key, ArgumentHandler handler)
        {
            this.key = key;
            this.handler = handler;

            this.aliases = new string[0];
        }
        public ArgumentHandlerPair(string key, string alias1, ArgumentHandler handler)
            : this(key, handler)
        {
            this.aliases = new string[] { alias1 };
        }
        public ArgumentHandlerPair(string key, string alias1, string alias2, ArgumentHandler handler)
            : this(key, handler)
        {
            this.aliases = new string[] { alias1, alias2 };
        }
        public ArgumentHandlerPair(string key, string alias1, string alias2, string alias3, ArgumentHandler handler)
            : this(key, handler)
        {
            this.aliases = new string[] { alias1, alias2, alias3 };
        }

        public string Key
        {
            get { return key; }
        }
        public IEnumerable<string> Aliases
        {
            get { foreach (var a in aliases)yield return a; }
        }
        public ArgumentHandler Handler
        {
            get { return handler; }
        }
    }
}
