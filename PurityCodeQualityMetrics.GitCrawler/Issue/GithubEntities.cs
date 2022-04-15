using Newtonsoft.Json;

namespace PurityCodeQualityMetrics.GitCrawler.Issue
{
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class User
    {
        public string Login { get; set; }
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string AvatarUrl { get; set; }
        public string GravatarId { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string FollowersUrl { get; set; }
        public string FollowingUrl { get; set; }
        public string GistsUrl { get; set; }
        public string StarredUrl { get; set; }
        public string SubscriptionsUrl { get; set; }
        public string OrganizationsUrl { get; set; }
        public string ReposUrl { get; set; }
        public string EventsUrl { get; set; }
        public string ReceivedEventsUrl { get; set; }
        public string Type { get; set; }
        public bool SiteAdmin { get; set; }
    }

    public class Label
    {
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public bool Default { get; set; }
    }

    public class Assignee
    {
        public string Login { get; set; }
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string AvatarUrl { get; set; }
        public string GravatarId { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string FollowersUrl { get; set; }
        public string FollowingUrl { get; set; }
        public string GistsUrl { get; set; }
        public string StarredUrl { get; set; }
        public string SubscriptionsUrl { get; set; }
        public string OrganizationsUrl { get; set; }
        public string ReposUrl { get; set; }
        public string EventsUrl { get; set; }
        public string ReceivedEventsUrl { get; set; }
        public string Type { get; set; }
        public bool SiteAdmin { get; set; }
    }

    public class Assignee2
    {
        public string Login { get; set; }
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string AvatarUrl { get; set; }
        public string GravatarId { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string FollowersUrl { get; set; }
        public string FollowingUrl { get; set; }
        public string GistsUrl { get; set; }
        public string StarredUrl { get; set; }
        public string SubscriptionsUrl { get; set; }
        public string OrganizationsUrl { get; set; }
        public string ReposUrl { get; set; }
        public string EventsUrl { get; set; }
        public string ReceivedEventsUrl { get; set; }
        public string Type { get; set; }
        public bool SiteAdmin { get; set; }
    }

    public class Creator
    {
        public string Login { get; set; }
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string AvatarUrl { get; set; }
        public string GravatarId { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string FollowersUrl { get; set; }
        public string FollowingUrl { get; set; }
        public string GistsUrl { get; set; }
        public string StarredUrl { get; set; }
        public string SubscriptionsUrl { get; set; }
        public string OrganizationsUrl { get; set; }
        public string ReposUrl { get; set; }
        public string EventsUrl { get; set; }
        public string ReceivedEventsUrl { get; set; }
        public string Type { get; set; }
        public bool SiteAdmin { get; set; }
    }

    public class Milestone
    {
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string LabelsUrl { get; set; }
        public long Id { get; set; }
        public string NodeId { get; set; }
        public long Number { get; set; }
        public string State { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Creator Creator { get; set; }
        public long OpenIssues { get; set; }
        public long ClosedIssues { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime ClosedAt { get; set; }
        public DateTime DueOn { get; set; }
    }

    public class PullRequest
    {
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string DiffUrl { get; set; }
        public string PatchUrl { get; set; }
    }

    public class Owner
    {
        public string Login { get; set; }
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string AvatarUrl { get; set; }
        public string GravatarId { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string FollowersUrl { get; set; }
        public string FollowingUrl { get; set; }
        public string GistsUrl { get; set; }
        public string StarredUrl { get; set; }
        public string SubscriptionsUrl { get; set; }
        public string OrganizationsUrl { get; set; }
        public string ReposUrl { get; set; }
        public string EventsUrl { get; set; }
        public string ReceivedEventsUrl { get; set; }
        public string Type { get; set; }
        public bool SiteAdmin { get; set; }
    }

    public class Permissions
    {
        public bool Admin { get; set; }
        public bool Push { get; set; }
        public bool Pull { get; set; }
    }

    public class License
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string SpdxId { get; set; }
        public string NodeId { get; set; }
        public string HtmlUrl { get; set; }
    }

    public class Repository
    {
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public Owner Owner { get; set; }
        public bool Private { get; set; }
        public string HtmlUrl { get; set; }
        public string Description { get; set; }
        public bool Fork { get; set; }
        public string Url { get; set; }
        public string ArchiveUrl { get; set; }
        public string AssigneesUrl { get; set; }
        public string BlobsUrl { get; set; }
        public string BranchesUrl { get; set; }
        public string CollaboratorsUrl { get; set; }
        public string CommentsUrl { get; set; }
        public string CommitsUrl { get; set; }
        public string CompareUrl { get; set; }
        public string ContentsUrl { get; set; }
        public string ContributorsUrl { get; set; }
        public string DeploymentsUrl { get; set; }
        public string DownloadsUrl { get; set; }
        public string EventsUrl { get; set; }
        public string ForksUrl { get; set; }
        public string GitCommitsUrl { get; set; }
        public string GitRefsUrl { get; set; }
        public string GitTagsUrl { get; set; }
        public string GitUrl { get; set; }
        public string IssueCommentUrl { get; set; }
        public string IssueEventsUrl { get; set; }
        public string IssuesUrl { get; set; }
        public string KeysUrl { get; set; }
        public string LabelsUrl { get; set; }
        public string LanguagesUrl { get; set; }
        public string MergesUrl { get; set; }
        public string MilestonesUrl { get; set; }
        public string NotificationsUrl { get; set; }
        public string PullsUrl { get; set; }
        public string ReleasesUrl { get; set; }
        public string SshUrl { get; set; }
        public string StargazersUrl { get; set; }
        public string StatusesUrl { get; set; }
        public string SubscribersUrl { get; set; }
        public string SubscriptionUrl { get; set; }
        public string TagsUrl { get; set; }
        public string TeamsUrl { get; set; }
        public string TreesUrl { get; set; }
        public string CloneUrl { get; set; }
        public string MirrorUrl { get; set; }
        public string HooksUrl { get; set; }
        public string SvnUrl { get; set; }
        public string Homepage { get; set; }
        public object Language { get; set; }
        public long ForksCount { get; set; }
        public long StargazersCount { get; set; }
        public long WatchersCount { get; set; }
        public long Size { get; set; }
        public string DefaultBranch { get; set; }
        public long OpenIssuesCount { get; set; }
        public bool IsTemplate { get; set; }
        public List<string> Topics { get; set; }
        public bool HasIssues { get; set; }
        public bool HasProjects { get; set; }
        public bool HasWiki { get; set; }
        public bool HasPages { get; set; }
        public bool HasDownloads { get; set; }
        public bool Archived { get; set; }
        public bool Disabled { get; set; }
        public string Visibility { get; set; }
        public DateTime PushedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Permissions Permissions { get; set; }
        public bool AllowRebaseMerge { get; set; }
        public object TemplateRepository { get; set; }
        public string TempCloneToken { get; set; }
        public bool AllowSquashMerge { get; set; }
        public bool AllowAutoMerge { get; set; }
        public bool DeleteBranchOnMerge { get; set; }
        public bool AllowMergeCommit { get; set; }
        public long SubscribersCount { get; set; }
        public long NetworkCount { get; set; }
        public License License { get; set; }
        public long Forks { get; set; }
        public long OpenIssues { get; set; }
        public long Watchers { get; set; }
    }

    public class Issue
    {
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string Url { get; set; }
        public string RepositoryUrl { get; set; }
        public string LabelsUrl { get; set; }
        public string CommentsUrl { get; set; }
        public string EventsUrl { get; set; }
        public string HtmlUrl { get; set; }
        public long Number { get; set; }
        public string State { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public User User { get; set; }
        public List<Label> Labels { get; set; }
        public Assignee Assignee { get; set; }
        public List<Assignee> Assignees { get; set; }
        public Milestone Milestone { get; set; }
        public bool Locked { get; set; }
        public string ActiveLockReason { get; set; }
        public long Comments { get; set; }
        public PullRequest PullRequest { get; set; }
        public object ClosedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Repository Repository { get; set; }
        public string AuthorAssociation { get; set; }
    }


    class FaultyFileData
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("patch")]
        public string Patch { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
    class Commit
    {
        [JsonProperty("sha")]
        public string Hash { get; set; }

        [JsonProperty("commit")]
        public CommitInfo CommitInfo { get; set; }

        [JsonProperty("Parents")]
        public List<Parent> Parents { get; set; }
    }

    class CommitInfo
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    class Parent
    {
        [JsonProperty("sha")]
        public string Hash { get; set; }
    }

    class BugFixCommit
    {
        [JsonProperty("sha")]
        public string Hash { get; set; }

        [JsonProperty("parents")]
        public List<Parent> Parents { get; set; }

        [JsonProperty("commit")]
        public CommitInfo CommitInfo { get; set; }

        [JsonProperty("files")]
        public List<FaultyFileData> FaultyFilesData { get; set; }

        public FaultyVersion GetFaultyVersion()
        {

            List<FaultyFile> faultyFiles = HelperFunctions.GetFaultyFiles(FaultyFilesData);
            if (faultyFiles is null) return null;
            // If there are two parents, the commit is a merge commit. The first parent is the master branch, where the second parent is the actual fix.
            // We want to run our metrics on the master branch, not the version with the fix. Therefore, we take the first parent. 
            return new FaultyVersion(Parents.First().Hash, faultyFiles, Hash);
        }
    }

}
