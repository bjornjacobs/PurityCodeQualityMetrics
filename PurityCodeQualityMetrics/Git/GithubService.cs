using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using RestSharp;


namespace CodeQualityAnalyzer
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
            var bugIssues = GetBugIssues();

            var bugCommitsHashes = commits
                // Look for patterns that close an issue (see: https://help.github.com/en/articles/closing-issues-using-keywords)
                .Where(x => Regex.IsMatch(x.CommitInfo.Message, @"(clos(e[sd]?|ing)|fix(e[sd]|ing)?|resolv(e[sd]?))", RegexOptions.IgnoreCase))
                // Check if there is a linked issue or pull-request in the meta data
                .Where(x => Regex.IsMatch(x.CommitInfo.Message, @"#\d+"))
                // Check if there is a linked bug issue to the number found in the commit message
                .Where(x => ContainsBugIssue(x.CommitInfo.Message, bugIssues))
                .Select(x => x.Hash)
                ;

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

            for (int page = 1; ; page++){
                request.AddOrUpdateParameter("page", page);

                RestResponse response;
                int tries = 0;
                do
                {
                    response = _client.GetAsync(request).Result;
                    tries++;
                }
                while (!response.IsSuccessful && tries < 10);
                List<T> currentPageResults = JsonConvert.DeserializeObject<List<T>>(response.Content);
                resultList.AddRange(currentPageResults);

                // TODO: Determine "Link-Header" instead of hard coding the index, works for now :)
                if (!response.Headers.ToList()[9].ToString().Contains("next")) break;
//           
                Console.Write($"\r{typeof(T).Name}s count: {resultList.Count}");
            }

            Console.WriteLine($"\nTotal of {resultList.Count} {typeof(T).Name}s found");

            return resultList;
        }

        private List<int> GetBugIssues()
        {
            RestRequest request = new RestRequest($"repos/{_owner}/{_repositoryName}/issues");
            request
                .AddParameter("labels", _bugLabel)
                .AddParameter("state", "closed")
                .AddParameter("per_page", "100")

                .AddHeader("User-Agent", "GitService")
                .AddHeader("Authorization", $"token {_accessToken}")
            ;

            List<Issue> issues = ExhaustApi<Issue>(request);

            return issues.Select(x => x.Number).ToList();
        }

        private bool ContainsBugIssue(string message, List<int> bugIssues)
        {
            var matches = Regex.Matches(message, @"#\d+");
            foreach (Match match in matches)
            {
                int number = int.Parse(match.Value.Substring(1));
                if (bugIssues.Contains(number)) return true;
            }

            return false;
        }

        public static HashSet<string> GetAffectedClasses(SyntaxNode treeRoot, List<int> affectedLines, string absoluteFileLocation)
        {
            HashSet<string> affectedClasses = new HashSet<string>();

            TextLineCollection lines = treeRoot.GetText().Lines;
            string fileName = absoluteFileLocation.Replace(@"\", "/");
            var pathArr = fileName.Split('/');
            string fileAndParent = $@"{pathArr[pathArr.Length - 2]}/{pathArr[pathArr.Length - 1]}";

            foreach (var affectedLine in affectedLines)
            {
                if (affectedLine < 1) continue; // This means the first line of the file has been inserted. This can not be an update to an class. Therefore, skip.
                TextSpan lineTextSpan = lines[affectedLine - 1].Span;
                List<ClassDeclarationSyntax> intersectedClasses = treeRoot.DescendantNodes()
                    .Where(x => x.Span.IntersectsWith(lineTextSpan))
                    .OfType<ClassDeclarationSyntax>()
                    .ToList();

                if (intersectedClasses.Count == 0)
                {
                    Console.WriteLine($"Affected line {affectedLine} doesn't intersect with a class");
                    continue;
                }
                if (intersectedClasses.Count > 1)
                {
                    Console.WriteLine($"Affected line {affectedLine} intersects with multiple classes");
                    continue;
                }

                foreach(ClassDeclarationSyntax classDeclaration in intersectedClasses)
                {
                    string className = classDeclaration.Identifier.ValueText;
                    string classNameWithHash = $"{className}-{fileAndParent.GetHashCode().ToString()}";
                    affectedClasses.Add(classNameWithHash);
                }
            }

            return affectedClasses;
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
