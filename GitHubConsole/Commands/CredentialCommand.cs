using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHubConsole.Commands
{
    public class CredentialCommand : Command
    {
        public override void Run(ArgumentDictionary args)
        {
            if (args.Contains("-set-user") && args.Contains("-set-pass"))
            {
                if (args["-set-user"].Count != 1)
                    Console.WriteLine("Incorrect value supplied for -set-user.");
                else if (args["-set-pass"].Count != 1)
                    Console.WriteLine("Incorrect value supplied for -set-pass.");
                else
                {
                    Credential c = new CredentialManagement.Credential(args["-set-user"][0], args["-set-pass"][0], credentialsKey);
                    if (!c.Save())
                        Console.WriteLine("Unable to store credentials.");
                    else
                        Console.WriteLine("Credentials updated for {0}.", c.Username);
                }
            }

            else if (args.Contains("-set-user"))
            {
                if (args["-set-user"].Count != 1)
                    Console.WriteLine("Incorrect value supplied for -set-user.");
                else
                {
                    Credential c = new Credential() { Target = credentialsKey };
                    if (!c.Load())
                        Console.WriteLine("No existing user defined. You must supply both username and password.");
                    else
                    {
                        c.Username = args["-set-user"][0];
                        if (!c.Save())
                            Console.WriteLine("Unable to store credentials.");
                        else
                            Console.WriteLine("Credentials updated.");
                    }
                }
            }


            else if (args.Contains("-set-pass"))
            {
                if (args["-set-pass"].Count != 0)
                    Console.WriteLine("Password cannot be supplied as command argument.");
                else
                {
                    Credential c = new Credential() { Target = credentialsKey };
                    if (!c.Load())
                        Console.WriteLine("No existing user defined. You must supply both username and password.");
                    else
                    {
                        Console.Write("Password: ");
                        string pass = Console.ReadLine();

                        c.Password = pass;
                        if (!c.Save())
                            Console.WriteLine("Unable to store credentials.");
                        else
                            Console.WriteLine("Credentials updated.");
                    }
                }
            }
        }
    }
}
