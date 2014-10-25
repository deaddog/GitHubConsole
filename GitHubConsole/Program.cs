using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using CredentialManagement;

namespace GitHubConsole
{
    class Program
    {
#if DEBUG
        public static readonly string gitDirectory = findRepo(@"C:\Users\Mikkel\Documents\Git\sw7\report\");
#else
        public static readonly string gitDirectory = findRepo(Directory.GetCurrentDirectory());
#endif

        static void Main(string[] args)
        {
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

        private static string findRepo(string directory)
        {
            return findRepo(new DirectoryInfo(directory));
        }
        private static string findRepo(DirectoryInfo directory)
        {
            var dirs = directory.GetDirectories(".git");
            for (int i = 0; i < dirs.Length; i++)
                if (dirs[i].Name == ".git")
                    return directory.FullName;

            if (directory.Parent == null)
                return null;
            else
                return findRepo(directory.Parent);
        }

        private static bool findGitHubRemote(out string user, out string project)
        {
            var remotes = findRemotes();

            for (int i = 0; i < remotes.Length; i++)
            {
                var m = Regex.Match(remotes[i].Item2, @"https://github.com/(?<user>[^/]+)/(?<proj>.+)\.git");
                if (m.Success)
                {
                    user = m.Groups["user"].Value;
                    project = m.Groups["proj"].Value;
                    return true;
                }
            }

            user = null;
            project = null;
            return false;
        }
        private static Tuple<string, string>[] findRemotes()
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process()
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo("git.exe", "remote -v")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = gitDirectory
                }
            };
            p.Start();
            StreamReader output = p.StandardOutput;
            p.WaitForExit();

            List<Tuple<string, string>> lines = new List<Tuple<string, string>>();
            while (!output.EndOfStream) lines.Add(Tuple.Create(output.ReadLine(), string.Empty));

            p.Dispose();

            for (int i = 0; i < lines.Count; i++)
            {
                var m = Regex.Match(lines[i].Item1, @"(?<name>[^\t]+)\t(?<url>.*) \(fetch\)");
                if (!m.Success)
                {
                    lines.RemoveAt(i--);
                    continue;
                }

                lines[i] = Tuple.Create(m.Groups["name"].Value, m.Groups["url"].Value);
            }

            return lines.ToArray();
        }

        private static Credentials loadCredentials()
        {
            Credential c = new Credential()
            {
                Target = "github"
            };
            if (!c.Load())
            {
                Console.Write("Username: ");
                string username = Console.ReadLine();

                Console.Write("Password: ");
                string password = Console.ReadLine();

                c = new CredentialManagement.Credential(username, password, "github");
                if (!c.Save())
                {
                    Console.WriteLine("Unable to store credentials.");
                    return null;
                }
                else
                    return new Credentials(c.Username, c.Password);
            }
            else
                return new Credentials(c.Username, c.Password);
        }
    }
}
