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
        private ArgumentHandler handler;

        public ArgumentHandlerPair(string key, ArgumentHandler handler)
        {
            this.key = key;
            this.handler = handler;
        }

        public string Key
        {
            get { return key; }
        }
        public ArgumentHandler Handler
        {
            get { return handler; }
        }
    }
}
