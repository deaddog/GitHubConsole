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
        private static Message validated;

        private static Credentials cred;
        private static string username;
        private static string project;

        private static string repoRoot;
        private static string repoGitDir;

        public static string Username
        {
            get
            {
                if (validated == null)
                    throw new InvalidOperationException($"{nameof(Username)} cannot be retrieved before running the {nameof(ValidateGitDirectory)} method.");

                if (validated != Message.NoError)
                    throw new InvalidOperationException($"{nameof(Username)} cannot be retrieved when git validation was not successfull.");

                return username;
            }
        }
        public static string Project
        {
            get
            {
                if (validated == null)
                    throw new InvalidOperationException($"{nameof(Project)} cannot be retrieved before running the {nameof(ValidateGitDirectory)} method.");

                if (validated != Message.NoError)
                    throw new InvalidOperationException($"{nameof(Project)} cannot be retrieved when git validation was not successfull.");

                return project;
            }
        }

        public static string RepositoryRoot
        {
            get
            {
                if (validated == null)
                    throw new InvalidOperationException($"{nameof(RepositoryRoot)} cannot be retrieved before running the {nameof(ValidateGitDirectory)} method.");

                if (validated != Message.NoError)
                    throw new InvalidOperationException($"{nameof(RepositoryRoot)} cannot be retrieved when git validation was not successfull.");

                return repoRoot;
            }
        }
        public static string RepositoryGirDirectory
        {
            get
            {
                if (validated == null)
                    throw new InvalidOperationException($"{nameof(RepositoryGirDirectory)} cannot be retrieved before running the {nameof(ValidateGitDirectory)} method.");

                if (validated != Message.NoError)
                    throw new InvalidOperationException($"{nameof(RepositoryGirDirectory)} cannot be retrieved when git validation was not successfull.");

                return repoGitDir;
            }
        }

        public static CachedGitHubClient Client
        {
            get
            {
                if (validated == null)
                    throw new InvalidOperationException($"{nameof(Client)} cannot be retrieved before running the {nameof(ValidateGitDirectory)} method.");

                if (validated != Message.NoError)
                    throw new InvalidOperationException($"{nameof(Client)} cannot be retrieved when git validation was not successfull.");

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
                StartInfo = new System.Diagnostics.ProcessStartInfo("git.exe", "rev-parse --git-dir --show-toplevel")
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
            if (ok)
            {
                using (StreamReader path = p.StandardOutput)
                {
                    repoGitDir = path.ReadLine();
                    repoRoot = path.ReadLine();
                }
                repoGitDir = repoGitDir.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                repoRoot = repoRoot.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            p.Dispose();

            return ok;
        }

        private static bool findGitHubRemote()
        {
            string domain = @"https://github\.com/|git@github\.com:|git://github\.com/";
            string user = @"[^/]+";
            string proj = @"([^.]|\.[^g]|\.g[^i]|\.gi[^t]|\.git.)+";
            var r = new Regex($@"^({domain})(?<user>{user})/(?<proj>{proj})(\.git)?$", RegexOptions.IgnoreCase);

            var remotes = findRemotes();

            for (int i = 0; i < remotes.Count; i++)
                if (!r.Match(remotes[i].Item2).Success)
                    remotes.RemoveAt(i--);

            int index = 0;
            for (int i = 0; i < remotes.Count; i++)
                if (remotes[i].Item1 == "origin")
                {
                    index = i;
                    break;
                }

            var m = r.Match(remotes[index].Item2);
            if (m.Success)
            {
                username = m.Groups["user"].Value;
                project = m.Groups["proj"].Value;
                return true;
            }
            else
            {
                username = null;
                project = null;
                return false;
            }
        }
        private static List<Tuple<string, string>> findRemotes()
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

            return lines;
        }

        public static Message ValidateGitDirectory()
        {
            if (validated != null)
                return validated;

            if (!isGitRepo())
                return validated = "The current directory is not part of a Git repository.\n" +
                    "GitHub commands cannot be executed.";

            if (!findGitHubRemote())
                return validated = "The current repository has no GitHub.com remotes.\n" +
                    "GitHub commands cannot be executed.";

            string token = Config.Default["authentification.token"];
            if (token == null || token == "")
                return validated = "Unable to load GitHub authentification token.\n" +
                    "Run [Yellow:github config --set authentification.token <token>] to set.";

            cred = new Credentials(token);
            validated = Message.NoError;

            return Message.NoError;
        }
    }
}
