using CredentialManagement;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitHubConsole
{
    public static class GitHub
    {
        private static readonly string credentialsKey = "githubconsole_managedkeyw";
        private static readonly string clientHeader = "GitHubC#Console";

        private static GitHubClient client;
        private static string username;
        private static string project;

        private static string FindRepo()
        {
#if DEBUG
            return findRepo(@"C:\Users\Mikkel\Documents\Git\ghconsole_test\");
#else
            return findRepo(Directory.GetCurrentDirectory());
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

        private static bool FindGitHubRemote(string gitDirectory, out string user, out string project)
        {
            var remotes = findRemotes(gitDirectory);

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
        private static Tuple<string, string>[] findRemotes(string gitDirectory)
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

        private static Credentials LoadCredentials()
        {
            Credential c = new Credential() { Target = credentialsKey };
            if (!c.Load() || (c.Username == null || c.Username.Length == 0 || c.Password == null || c.Password.Length == 0))
                return null;
            else
                return new Credentials(c.Username, c.Password);
        }
        private static GitHubClient CreateClient(out string username, out string project)
        {
            Credentials cred;
            if (validateGitDirectory(out cred, out username, out project))
                return new GitHubClient(new ProductHeaderValue(clientHeader)) { Credentials = cred };
            else
                return null;
        }

        private static bool validateGitDirectory(out Credentials cred, out string username, out string project)
        {
            string gitDirectory = FindRepo();

            cred = null;
            username = null;
            project = null;

            if (gitDirectory == null)
            {
                Console.WriteLine("The current directory is not part of a Git repository.");
                Console.WriteLine("GitHub commands cannot be executed.");
                return false;
            }

            if (!FindGitHubRemote(gitDirectory, out username, out project))
            {
                Console.WriteLine("Unable to find GitHub project.");
                return false;
            }

            cred = LoadCredentials();
            if (cred == null)
            {
                Console.WriteLine("Unable to load GitHub credentials.");
                Console.Write("Run ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("github cred -set-user");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(" to set.");
                return false;
            }

            return true;
        }
    }
}
