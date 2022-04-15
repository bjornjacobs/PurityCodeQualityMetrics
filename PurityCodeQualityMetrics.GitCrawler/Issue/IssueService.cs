using Newtonsoft.Json;

namespace PurityCodeQualityMetrics.GitCrawler.Issue;

public class IssueService
{
    private static string CacheDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dev", "repos", "issues");

    public List<Issue> GetIssues(TargetProject project)
    {
        var cacheFile = Path.Combine(CacheDir, $"{project.RepositoryName}.json");


        if (File.Exists(cacheFile))
        {
            return JsonConvert.DeserializeObject<List<Issue>>(File.ReadAllText((cacheFile)))!;
        }

        var github = new GithubService(project);
        var issues = github.GetBugIssues();

        Directory.CreateDirectory(CacheDir);
        File.WriteAllText(cacheFile, JsonConvert.SerializeObject(issues));

        return issues;
    }
}