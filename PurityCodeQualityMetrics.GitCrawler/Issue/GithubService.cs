using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RestSharp;

namespace PurityCodeQualityMetrics.GitCrawler.Issue
{
    internal class GithubService : IIssueTrackerService
    {
        private const string BaseUrl = "https://api.github.com";

        private readonly string _owner;
        private readonly string _repositoryName;
        private readonly string _bugLabel;
        private readonly RestClient _client;
        private readonly string _accessToken;

        public GithubService(TargetProject targetProject)
        {
            _accessToken = SecretService.GetGithubSecrets().AccessToken;
            _owner = targetProject.OrganizationName;
            _repositoryName = targetProject.RepositoryName;
            _bugLabel = targetProject.BugLabel;
            _client = new RestClient(BaseUrl);
        }

        public List<FaultyVersion> GetFaultyVersions()
        {

            var request = new RestRequest($"repos/{_owner}/{_repositoryName}/commits");
            request
                .AddParameter("per_page", "100")

                .AddHeader("User-Agent", "GitService")
                .AddHeader("Authorization", $"token {_accessToken}")
                ;

            List<Commit> commits = ExhaustApi<Commit>(request);
            var bugIssues = GetBugIssues().Select(x => x.Number).ToList();

            var bugCommitsHashes = commits
                    // Look for patterns that close an issue (see: https://help.github.com/en/articles/closing-issues-using-keywords)
                    .Where(x => Regex.IsMatch(x.CommitInfo.Message,
                        @"(clos(e[sd]?|ing)|fix(e[sd]|ing)?|resolv(e[sd]?))", RegexOptions.IgnoreCase))
                    // Check if there is a linked issue or pull-request in the meta data
                    .Where(x => Regex.IsMatch(x.CommitInfo.Message, @"#\d+"))
                    // Check if there is a linked bug issue to the number found in the commit message
                    .Where(x => ContainsBugIssue(x.CommitInfo.Message, bugIssues))
                    .Select(x => x.Hash).ToList()
                ;


            var all = commits
                // Look for patterns that close an issue (see: https://help.github.com/en/articles/closing-issues-using-keywords)
                .Where(x => Regex.IsMatch(x.CommitInfo.Message, @"(clos(e[sd]?|ing)|fix(e[sd]|ing)?|resolv(e[sd]?))",
                    RegexOptions.IgnoreCase)).ToList();


            Console.WriteLine($"{bugCommitsHashes.Count()} identified as bug fixing commits");

            ConcurrentBag<BugFixCommit> bugCommits = new ConcurrentBag<BugFixCommit>();

            Parallel.ForEach(bugCommitsHashes, (bugCommitSha) =>
            {
                var commitRequest = new RestRequest($"repos/{_owner}/{_repositoryName}/commits/{bugCommitSha}")
                        .AddHeader("User-Agent", "GitService")
                        .AddHeader("Authorization", $"token {_accessToken}")
                    ;

                var response = _client.GetAsync(commitRequest).Result;

                var bugCommit = JsonConvert.DeserializeObject<BugFixCommit>(response.Content);

                bugCommits.Add(bugCommit);
                Console.Write($"\rData received for commit: {bugCommit.Hash}");

            });

            List<FaultyVersion> faultyVersions = new List<FaultyVersion>();
            foreach (var bugCommit in bugCommits)
            {
                FaultyVersion faultyVersion = bugCommit.GetFaultyVersion();
                if (faultyVersion != null)
                {
                    faultyVersions.Add(faultyVersion);
                }
            }

            return faultyVersions;
        }

        private List<T> ExhaustApi<T>(RestRequest request)
        {
            List<T> resultList = new List<T>();

            for (int page = 1;; page++)
            {
                request.AddOrUpdateParameter("page", page);

                RestResponse response;
                int tries = 0;
                do
                {
                    response = _client.GetAsync(request).Result;
                    tries++;
                } while (!response.IsSuccessful && tries < 10);

                List<T> currentPageResults = JsonConvert.DeserializeObject<List<T>>(response.Content);
                resultList.AddRange(currentPageResults);

                if (!currentPageResults.Any()) break;

                Console.Write($"\r{typeof(T).Name}s count: {resultList.Count}");
            }

            Console.WriteLine($"\nTotal of {resultList.Count} {typeof(T).Name}s found");

            return resultList;
        }

        public List<Issue> GetBugIssues()
        {
            RestRequest request = new RestRequest($"repos/{_owner}/{_repositoryName}/issues");
            request
                .AddParameter("labels", _bugLabel)
                .AddParameter("state", "closed")
                .AddParameter("per_page", "500")

                .AddHeader("User-Agent", "GitService")
                .AddHeader("Authorization", $"token {_accessToken}")
                ;

            List<Issue> issues = ExhaustApi<Issue>(request);

            return issues;
        }

        private bool ContainsBugIssue(string message, List<long> bugIssues)
        {
            var matches = Regex.Matches(message, @"#\d+");
            foreach (Match match in matches)
            {
                long number = long.Parse(match.Value.Substring(1));
                if (bugIssues.Contains(number)) return true;
            }

            return false;
        }

    }

    public class FaultyFile
    {
        public string Filename { get; set; }
        public List<int> AffectedLines { get; set; }

        public FaultyFile(string filename, List<int> affectedLines)
        {
            Filename = filename;
            AffectedLines = affectedLines;
        }
    }

    public class FaultyVersion
    {
        public string Hash { get; set; }
        public string FixHash { get; set; }
        public List<FaultyFile> FaultyFiles { get; set; }

        public FaultyVersion(string hash, List<FaultyFile> faultyFiles, string fixHash)
        {
            Hash = hash;
            FaultyFiles = faultyFiles;
            FixHash = fixHash;
        }
    }
}
