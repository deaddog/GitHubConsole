using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitHubConsole.CachedGitHub
{
    public class CachedIssuesClient : Fallback<IssuesClient>, IIssuesClient
    {
        private string path
        {
            get
            {
                string dir = Path.Combine(GitHub.RepositoryRoot, ".git", "gh_caching");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                return Path.Combine(dir, "issues.xml");
            }
        }

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
        public Task<IReadOnlyList<Issue>> GetAllForRepository(string owner, string name) => GetAllForRepository(owner, name, new RepositoryIssueRequest());
        public Task<IReadOnlyList<Issue>> GetAllForRepository(string owner, string name, RepositoryIssueRequest request)
        {
            if (useCache())
                return Task.FromResult(new ReadOnlyCollection<Issue>(loadIssues().ToList()) as IReadOnlyList<Issue>);
            else
            {
                var r = fallback.GetAllForRepository(owner, name).Result;

                saveIssues(r);

                return Task.FromResult(r);
            }
        }
        public Task<Issue> Update(string owner, string name, int number, IssueUpdate issueUpdate) => fallback.Update(owner, name, number, issueUpdate);

        private bool useCache()
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Issue> loadIssues()
        {
            throw new NotImplementedException();
        }

        private void saveIssues(IEnumerable<Issue> issues)
        {
            throw new NotImplementedException();
        }
    }
}
