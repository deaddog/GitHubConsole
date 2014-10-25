using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using CredentialManagement;
using GitHubConsole.Commands;

namespace GitHubConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            ArgumentDictionary arguments = new ArgumentDictionary(args);

            if (gitDirectory == null)
            {
                Console.WriteLine("The current directory is not part of a Git repository.");
                Console.WriteLine("GitHub commands cannot be executed.");
                return;
            }

            string user;
            string project;

            if (!findGitHubRemote(out user, out project))
            {
                Console.WriteLine("Unable to find GitHub project.");
                return;
            }

            Credentials cred = loadCredentials();
            if (cred == null)
            {
                Console.WriteLine("Unable to load GitHub credentials.");
                return;
            }
#if DEBUG
            Console.ReadKey(true);
#endif
        }
    }
}
