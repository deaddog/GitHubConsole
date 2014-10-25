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

#if DEBUG
            Console.ReadKey(true);
#endif
        }
    }
}
