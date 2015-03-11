using CredentialManagement;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GitHubConsole.Commands
{
    public abstract class Command
    {
        public abstract void Execute();
        public virtual bool HandleArgument(Argument argument)
        {
            Console.WriteLine("Unknown parameter \"{0}\".", argument.Key);
            return false;
        }
    }
}
