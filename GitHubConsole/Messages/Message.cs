using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubConsole.Messages
{
    public class Message
    {
        private string message;

        public Message(string message)
        {
            this.message = message;
        }
        public Message(string message, params object[] args)
            : this(string.Format(message, args))
        {
        }

        public virtual string GetMessage()
        {
            return message;
        }
    }
}
