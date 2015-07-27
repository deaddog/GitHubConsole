using Octokit;
using System;

namespace GitHubConsole.CachedGitHub
{
    public class CachedGitHubClient : Fallback<GitHubClient>, IGitHubClient
    {
        private CachedIssuesClient issues;
        public CachedGitHubClient(GitHubClient client)
            :base(client)
        {
            this.issues = new CachedIssuesClient(client);
        }

        public Credentials Credentials => fallback.Credentials;

        public IConnection Connection => fallback.Connection;
        public IAuthorizationsClient Authorization => fallback.Authorization;
        public IActivitiesClient Activity => fallback.Activity;
        public IIssuesClient Issue => issues;
        public IMiscellaneousClient Miscellaneous => fallback.Miscellaneous;
        public IOauthClient Oauth => fallback.Oauth;
        public IOrganizationsClient Organization => fallback.Organization;
        public IPullRequestsClient PullRequest => fallback.PullRequest;
        public IRepositoriesClient Repository => fallback.Repository;
        public IGistsClient Gist => fallback.Gist;
        public IReleasesClient Release => fallback.Release;
        public ISshKeysClient SshKey => fallback.SshKey;
        public IUsersClient User => fallback.User;
        public INotificationsClient Notification => fallback.Notification;
        public IGitDatabaseClient GitDatabase => fallback.GitDatabase;
        public ISearchClient Search => fallback.Search;
    }
}
