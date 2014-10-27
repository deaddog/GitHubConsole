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
        private bool setUser = false, setPass = false;
        private string newUsername = null;

        public override void Execute()
        {
            Credential c = new Credential() { Target = credentialsKey };
            if (setPass && !setUser)
            {
                if (!c.Load())
                {
                    Console.WriteLine("A password can not be set for user - no GitHub user defined.");
                    return;
                }
            }

            if (setUser)
                SetCredentials(newUsername);
            else if (setPass)
                SetPassword();

            if (!setUser && !setPass)
            {
                Console.Write("The current GitHub user is ");
                if (c.Load())
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(c.Username);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("N/A");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }

        public override bool HandleArgument(ArgumentStack.Argument argument)
        {
            switch (argument.Key)
            {
                case "-set-user":
                    if (argument.Count != 0 && argument.Count != 1)
                    {
                        Console.WriteLine("Incorrect value supplied for -set-user.");
                        return false;
                    }
                    if (argument.Count == 1)
                        newUsername = argument[0];

                    setUser = true;
                    setPass = true;
                    return true;

                case "-set-pass":
                    if (argument.Count != 0)
                    {
                        Console.WriteLine("A value cannot be supplied for -set-pass.");
                        return false;
                    }
                    setPass = true;
                    return true;

                default:
                    return base.HandleArgument(argument);
            }
        }

        public static void SetCredentials(string username = null)
        {
            Credential c = new Credential() { Target = credentialsKey };
            c.Load();

            if (username == null)
            {
                Console.Write("GitHub username: ");
                username = Console.ReadLine();
            }

            c.Username = username;
            c.Password = ".";

            c.PersistanceType = PersistanceType.LocalComputer;
            c.Save();

            SetPassword();
        }
        public static void SetPassword()
        {
            Credential c = new Credential() { Target = credentialsKey };
            c.Load();

            Console.Write("GitHub password: ");
            StringBuilder sb = new StringBuilder();

            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                        sb.Remove(sb.Length - 1, 1);
                }
                else if (key.Key == ConsoleKey.Enter)
                    break;
                else
                    sb.Append(key.KeyChar);
            } while (true);
            Console.WriteLine();

            c.Password = sb.ToString();

            c.PersistanceType = PersistanceType.LocalComputer;
            c.Save();
            Console.WriteLine("Credentials updated for {0}.", c.Username);
        }
    }
}
