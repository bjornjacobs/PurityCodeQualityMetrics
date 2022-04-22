using System.Text.Json;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics.GitCrawler.Issue;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Purity.Storage;
using Commit = LibGit2Sharp.Commit;
using Repository = LibGit2Sharp.Repository;

namespace PurityCodeQualityMetrics.GitCrawler;

public class LandkroonInterface
{
    private ILogger _logger;
    private PurityAnalyser _purityAnalyser;
    private PurityCalculator _purityCalculator;
    private Commit? currentCommitState;
    private IPurityReportRepo _purityReportRepo;
    private IssueService _issueService = new IssueService();

    private IPurityReportRepo _nonVolitilePurityRepo = new EfPurityRepo("standard_lib");
    
    public static string OutputDir  =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dev", "repos", "results");

    public LandkroonInterface(ILogger logger, PurityAnalyser purityAnalyser, PurityCalculator purityCalculator)
    {
        _logger = logger;
        _purityAnalyser = purityAnalyser;
        _purityCalculator = purityCalculator;
        _purityReportRepo = new InMemoryReportRepo();
    }

    public async Task Run(TargetProject project)
    {
        var repo = new Repository(project.RepoPath);
        Commands.Checkout(repo, project.MainBranch);
        var commitsWithIssues = GetCommitsWIthIssues(repo, project);
        
        foreach (var commit in commitsWithIssues)
        {
            foreach (var parentCommit in commit.Parents)
            {
                var codeChanges = repo.Diff.Compare<Patch>(commit.Tree, parentCommit.Tree) // Get all changes compared to the parent
                    .SelectMany(x => x.GetFileChangesFromPatch()).ToList();
                if (codeChanges.All(x => Path.GetExtension(x.Path) != ".cs"))
                {
                    _logger.LogInformation("Commit didn't change any csharp files");
                    continue;
                }

                var before = await MetricsForCommit(project, repo, parentCommit, codeChanges.Select(x => x.Removed));
                var after = await MetricsForCommit(project, repo, commit, codeChanges.Select(x => x.Added));

                var r = GetScorePerFunction(before, after);
                r.ForEach(x => x.CommitHash = parentCommit.Sha);
                AppendResults(Path.Combine(OutputDir, project.RepositoryName + ".json"), r);
            }
        }
    }

    private static void AppendResults(string path, List<FunctionOutput> newResults)
    {
        Directory.CreateDirectory(OutputDir);
        
        var existing = File.Exists(path) ? JsonSerializer.Deserialize<List<FunctionOutput>>(File.ReadAllText(path)) : new List<FunctionOutput>();
        existing.AddRange(newResults);
        File.WriteAllText(path, JsonSerializer.Serialize(existing));
    }

    private List<FunctionOutput> GetScorePerFunction(SolutionVersionWithMetrics before, SolutionVersionWithMetrics after)
    {
        var allFunctions = before.Scores.Concat(after.Scores)
            .Select(x => new {x.Report.FullName, Class = x.Report.Namespace + "." + x.Report.Name.Split(".").First()}).ToList();
                
        var data = allFunctions.Select(func =>
        {
            var rating = new FunctionOutput(func.FullName);
            var b = before.Scores.FirstOrDefault(x => x.Report.FullName == func.FullName);
            if (b != null)
            {
                rating.Before.Violations = b.Violations;
                rating.Before.TotalLinesOfSourceCOde = b.LinesOfSourceCode;
                rating.Before.DependencyCount = b.DependencyCount;
                
                if(before.ClassesWithMetrics.TryGetValue(func.Class, out var m))
                    rating.Before.SetMetrics(m.MetricResult);        
            }
            
            var a = after.Scores.FirstOrDefault(x => x.Report.FullName == func.FullName);
            if (a != null)
            {
                rating.After.Violations = a.Violations;
                rating.After.TotalLinesOfSourceCOde = a.LinesOfSourceCode;
                rating.After.DependencyCount = a.DependencyCount;
                if(after.ClassesWithMetrics.TryGetValue(func.Class, out var m))
                    rating.After.SetMetrics(m.MetricResult);
            }
            return rating;
        }).ToList();

        return data;
    }

    private async Task<SolutionVersionWithMetrics> MetricsForCommit(TargetProject project, Repository repository, Commit commit, IEnumerable<LinesChange> codeChanges)
    {
        var stateChange = CheckoutCommit(repository, commit); // Get all changes since last state on disk
        var solutionFile = project.SolutionFile.Select(x => Path.Combine(project.RepoPath, x))
            .First(File.Exists); //FInd the right solution file.
                
        var metrics = await AnalyseCode(solutionFile, stateChange);
        metrics.Scores = codeChanges.ToList().FilterChangedScores(metrics.Scores); //Only get scores that are changed
        
        return metrics;
    }

    private async Task<SolutionVersionWithMetrics> AnalyseCode(string solution, List<string> changedFiles)
    {
        var runner = new MetricRunner(solution, _purityReportRepo, _purityAnalyser, _purityCalculator);
        var metrics = await runner.GetSolutionVersionWithMetrics(changedFiles);
        return metrics;
    }

    private List<string> CheckoutCommit(Repository repository, Commit commit)
    {
        var changes = currentCommitState == null
            ? new List<string>()
            : repository.Diff
                .Compare<Patch>(currentCommitState.Tree, commit.Tree).Select(x => x.Path)
                .Select(x => x.Replace('/', '\\')).ToList();

        Commands.Checkout(repository, commit);
        currentCommitState = commit;
        return changes;
    }

    private List<Commit> GetCommitsWIthIssues(Repository repo, TargetProject project)
    {
        var issues = _issueService.GetIssues(project).Select(x => x.Number).ToList();

        var bugCommits = repo.Commits
            .Where(x => Regex.IsMatch(x.Message,
                @"(clos(e[sd]?|ing)|fix(e[sd]|ing)?|resolv(e[sd]?))", RegexOptions.IgnoreCase))
            // Check if there is a linked issue or pull-request in the meta data
            .Where(x => Regex.IsMatch(x.Message, @"#\d+"))
            // Check if there is a linked bug issue to the number found in the commit message
            .Where(x => ContainsBugIssue(x.Message, issues))
            .ToList();

        bool ContainsBugIssue(string message, List<long> bugIssues)
        {
            var matches = Regex.Matches(message, @"#\d+");
            foreach (Match match in matches)
            {
                long number = long.Parse(match.Value.Substring(1));
                if (bugIssues.Contains(number)) return true;
            }

            return false;
        }

        return bugCommits;
    }


    private PurityReport? TryFindPurityReport(List<LinesChange> changes, MethodDependency dep, PurityReport context)
    {
        //Check if context actually needed.eg is in change list
        if (!context.ReportInChanges(changes))
            return null;

        var existing = _nonVolitilePurityRepo.GetByFullName(dep.FullName);
        if (existing != null)
            return existing;


        var userGenerated = MissingMethodInput.FromConsole(dep);
        if (userGenerated == null) return null;
        _purityReportRepo.AddRange(new[] {userGenerated});
        _nonVolitilePurityRepo.AddRange(new[] {userGenerated});
        return userGenerated;
    }
}