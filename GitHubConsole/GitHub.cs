﻿using CommandLineParsing;
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

        private static IGitHubClient client;
        private static User currentUser;
        private static Message validated;
        private static bool? accessPath = null;

        private static Credentials cred;
        private static string username;
        private static string project;

        private static string repoRoot;
        private static string repoGitDir;

        public static string Username => ensureValidated(nameof(Username), username);
        public static string Project => ensureValidated(nameof(Project), project);

        public static string RepositoryRoot => ensurePath(nameof(RepositoryRoot), repoRoot);
        public static string RepositoryGitDirectory => ensurePath(nameof(RepositoryGitDirectory), repoGitDir);

        public static string RepositoryStorage
        {
            get
            {
                if (!accessPath ?? false)
                    throw new InvalidOperationException($"{nameof(RepositoryStorage)} cannot be retrieved before running the {nameof(ValidateGitDirectory)} method.");

                string dir = Path.Combine(RepositoryGitDirectory, "githubconsole");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                return dir;
            }
        }
        public static string GlobalStorage
        {
            get
            {
                var roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);

                string dir = Path.Combine(roamingPath, "DeadDog", "GitHubConsole");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                return dir;
            }
        }

        private static T ensurePath<T>(string name, T value)
        {
            if (!accessPath ?? false)
                throw new InvalidOperationException($"{name} cannot be retrieved before running the {nameof(ValidateGitDirectory)} method.");

            return value;
        }

        private static void ensureValidated(string name)
        {
            if (validated == null)
                throw new InvalidOperationException($"{name} cannot be retrieved before running the {nameof(ValidateGitDirectory)} method.");

            if (validated != Message.NoError)
                throw new InvalidOperationException($"{name} cannot be retrieved when git validation was not successfull.");
        }
        private static T ensureValidated<T>(string name, T value)
        {
            ensureValidated(name);

            return value;
        }
        private static T ensureValidated<T>(string name, ref T value, Func<T> create) where T : class
        {
            ensureValidated(name);

            if (value == null)
                value = create();

            return value;
        }

        public static IGitHubClient Client => ensureValidated(nameof(Client), ref client, () => new GitHubClient(new ProductHeaderValue(clientHeader)) { Credentials = cred });
        public static User CurrentUser => ensureValidated(nameof(CurrentUser), ref currentUser, () => Client?.User?.Current().Result);

        public static bool IsGitRepository()
        {
#if DEBUG
            Directory.SetCurrentDirectory(@"C:\Users\Mikkel\Documents\Git\ghconsole_test");
#endif
            if (accessPath.HasValue)
                return accessPath.Value;

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
                repoGitDir = Path.GetFullPath(repoGitDir.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
                repoRoot = Path.GetFullPath(repoRoot.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
            }

            p.Dispose();
            accessPath = ok;

            return ok;
        }

        private static bool findGitHubRemote()
        {
            string domain = @"https?://github\.com/|git@github\.com:|git://github\.com/";
            string user = @"[^/]+";
            string proj = @"([^.]|\.[^g]|\.g[^i]|\.gi[^t]|\.git.)+";
            var r = new Regex($@"^({domain})(?<user>{user})/(?<proj>{proj})(\.git)?$", RegexOptions.IgnoreCase);

            var remotes = findRemotes();

            if (remotes.Count == 0)
                return false;

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

            if (remotes.Count == 0)
                return false;

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

            if (!IsGitRepository())
                return validated = "The current directory is not part of a Git repository.\n" +
                    "GitHub commands cannot be executed.";

            if (!findGitHubRemote())
                return validated = "The current repository has no GitHub.com remotes.\n" +
                    "GitHub commands cannot be executed.";

            string token = Config.Default["authentication.token"];
            if (string.IsNullOrWhiteSpace(token))
            {
                return validated = "Unable to load GitHub authentication token.\n" +
                    "Run [Yellow:github config --set authentication.token <token>] to set.";
            }

            cred = new Credentials(token);
            return validated = Message.NoError;
        }
    }
}
