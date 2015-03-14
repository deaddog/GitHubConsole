using CredentialManagement;
using GitHubConsole.Commands.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitHubConsole.Commands
{
    public class CredentialCommand : ManagedCommand
    {
        private bool setUser = false, setPass = false;
        private string newUsername = null;

        protected override IEnumerable<ArgumentHandlerPair> LoadArgumentHandlers()
        {
            yield return new ArgumentHandlerPair("-set-user", setUserArgument);
            yield return new ArgumentHandlerPair("-set-pass", setPassArgument);
        }

        private bool setUserArgument(Argument argument)
        {
            if (argument.Count != 0 && argument.Count != 1)
            {
                Console.WriteLine("Incorrect value supplied for -set-user.");
                return false;
            }
            if (argument.Count == 1)
                newUsername = argument[0];
            else if (argument.Count > 1)
                ColorConsole.ToConsoleLine("Only a username can be supplied for the [[:White:-set-user]] argument. You have supplied [[:Cyan:{0}]] values.", argument.Count);

            setUser = true;
            setPass = true;
            return true;
        }
        private bool setPassArgument(Argument argument)
        {
            if (argument.Count != 0)
            {
                Console.WriteLine("A value cannot be supplied for -set-pass.");
                return false;
            }
            setPass = true;
            return true;
        }

        public override void Execute()
        {
            Credential c = new Credential() { Target = GitHub.CredentialsKey };
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

        public static void SetCredentials(string username = null)
        {
            Credential c = new Credential() { Target = GitHub.CredentialsKey };
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
            Credential c = new Credential() { Target = GitHub.CredentialsKey };
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
