using CommandLineParsing;
using GitHubConsole.CachedGitHub;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GitHubConsole
{
    public static class GitHub
    {
        private static readonly string clientHeader = "GitHubC#Console";

        private static CachedGitHubClient client;
        private static Octokit.Credentials cred;
        private static string username;
        private static string project;

        public static string Username
        {
            get
            {
                if (username == null)
                    validateGitDirectory(out cred, out username, out project);

                return username;
            }
        }
        public static string Project
        {
            get
            {
                if (project == null)
                    validateGitDirectory(out cred, out username, out project);

                return project;
            }
        }

        public static CachedGitHubClient Client
        {
            get
            {
                string user = Username;

                if (cred == null)
                    return null;

                if (client == null)
                    client = new CachedGitHub.CachedGitHubClient(new GitHubClient(new ProductHeaderValue(clientHeader)) { Credentials = cred });

                return client;
            }
        }

        private static bool isGitRepo()
        {
#if DEBUG
            Directory.SetCurrentDirectory(@"C:\Users\Mikkel\Documents\Git\ghconsole_test");
#endif
            System.Diagnostics.Process p = new System.Diagnostics.Process()
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo("git.exe", "status")
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };
            bool ok;

            p.Start();
            using (StreamReader output = p.StandardError)
            {
                p.WaitForExit();
                ok = output.EndOfStream;
            }
            p.Dispose();

            return ok;
        }

        private static bool FindGitHubRemote(out string user, out string project)
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
                    UseShellExecute = false
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

        private static bool validateGitDirectory(out Credentials cred, out string username, out string project)
        {
            cred = null;
            username = null;
            project = null;

            if (!isGitRepo())
            {
                Console.WriteLine("The current directory is not part of a Git repository.");
                Console.WriteLine("GitHub commands cannot be executed.");
                return false;
            }

            if (!FindGitHubRemote(out username, out project))
            {
                Console.WriteLine("Unable to find GitHub project.");
                return false;
            }

            string token = Config.Default["authentification.token"];
            if (token == null || token == "")
            {
                Console.WriteLine("Unable to load GitHub authentification token.");
                ColorConsole.WriteLine("Run [Yellow:github config --set authentification.token <token>] to set.");
                return false;
            }
            cred = new Credentials(token);

            return true;
        }
    }
}
