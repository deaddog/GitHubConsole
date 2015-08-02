﻿using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GitHubConsole.CachedGitHub
{
    public class CachedIssuesClient : Fallback<IssuesClient>, IIssuesClient
    {
        private string path => Path.Combine(GitHub.RepositoryStorage, "issues.xml");

        public CachedIssuesClient(GitHubClient client)
            : base(() => client.Issue as IssuesClient)
        {
        }

        public IAssigneesClient Assignee => fallback.Assignee;
        public IIssueCommentsClient Comment => fallback.Comment;
        public IIssuesEventsClient Events => fallback.Events;
        public IIssuesLabelsClient Labels => fallback.Labels;
        public IMilestonesClient Milestone => fallback.Milestone;

        public Task<Issue> Create(string owner, string name, NewIssue newIssue) => fallback.Create(owner, name, newIssue);
        public Task<Issue> Get(string owner, string name, int number) => fallback.Get(owner, name, number);
        public Task<IReadOnlyList<Issue>> GetAllForCurrent() => fallback.GetAllForCurrent();
        public Task<IReadOnlyList<Issue>> GetAllForCurrent(IssueRequest request) => fallback.GetAllForCurrent(request);
        public Task<IReadOnlyList<Issue>> GetAllForOrganization(string organization) => fallback.GetAllForOrganization(organization);
        public Task<IReadOnlyList<Issue>> GetAllForOrganization(string organization, IssueRequest request) => fallback.GetAllForOrganization(organization, request);
        public Task<IReadOnlyList<Issue>> GetAllForOwnedAndMemberRepositories() => fallback.GetAllForOwnedAndMemberRepositories();
        public Task<IReadOnlyList<Issue>> GetAllForOwnedAndMemberRepositories(IssueRequest request) => fallback.GetAllForOwnedAndMemberRepositories(request);
        public Task<IReadOnlyList<Issue>> GetAllForRepository(string owner, string name)
        {
            if (useCache())
                return Task.FromResult(new ReadOnlyCollection<Issue>(loadIssues().ToList()) as IReadOnlyList<Issue>);
            else
            {
                var r = fallback.GetAllForRepository(owner, name, new RepositoryIssueRequest() { State = ItemState.All }).Result;

                saveIssues(r);

                return Task.FromResult(r);
            }
        }
        public Task<IReadOnlyList<Issue>> GetAllForRepository(string owner, string name, RepositoryIssueRequest request)
        {
            throw new NotSupportedException($"{nameof(RepositoryIssueRequest)} is not supported in the {nameof(CachedIssuesClient)}.");
        }
        public Task<Issue> Update(string owner, string name, int number, IssueUpdate issueUpdate) => fallback.Update(owner, name, number, issueUpdate);

        private bool useCache()
        {
            int timeout = int.Parse(Config.Default["issues.timeout"] ?? "0");

            if (timeout <= 0)
                return false;

            if (!File.Exists(path))
                return false;

            XDocument doc = XDocument.Load(path);

            DateTime dt = DateTime.Parse(doc.Element("cache").Element("timestamp").Value);

            return (DateTime.Now - dt).TotalSeconds <= timeout;
        }

        private IEnumerable<Issue> loadIssues()
        {
            XDocument doc = XDocument.Load(path);
            var xIssues = doc.Element("cache").Element("issues");

            var issues = xIssues.Elements("issue").Select(x => XDeserializer.Deserialize<Issue>(x)).ToArray();
            Array.Sort(issues, (x, y) => -x.Number.CompareTo(y.Number));

            return issues;
        }

        private void saveIssues(IEnumerable<Issue> issues)
        {
            var arr = issues.ToArray();
            Array.Sort<Issue>(arr, (x, y) => x.Number.CompareTo(y.Number));

            XDocument doc;
            doc = !File.Exists(path) ? new XDocument(new XElement("cache", new XElement("timestamp", ""), new XElement("issues"))) : XDocument.Load(path);
            var xIssues = doc.Element("cache").Element("issues");

            foreach (var i in arr)
            {
                var xI = XSerializer.Serialize("issue", i);

                var old = xIssues.Elements("issue").Where(x => x.Element("number")?.Value == i.Number.ToString()).FirstOrDefault();
                if (old != null)
                    old.ReplaceWith(xI);
                else
                    xIssues.Add(xI);

            }

            doc.Element("cache").Element("timestamp").SetValue(DateTime.Now);

            doc.Save(path);
        }
    }
}