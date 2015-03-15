﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubConsole.Messages
{
    public class ErrorMessage
    {
        private string message;

        public static ErrorMessage NoError
        {
            get { return null; }
        }

        protected ErrorMessage()
        {
            this.message = null;
        }
        public ErrorMessage(string message)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            if (message.Trim().Length == 0)
                throw new ArgumentException("Attempted to display an empty message.", message);

            this.message = message;
        }
        public ErrorMessage(string message, params object[] args)
            : this(string.Format(message, args))
        {
        }

        public virtual string GetMessage()
        {
            return message;
        }
    }
}
