﻿using System.Text.Json;
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
        Console.WriteLine($"Starting crawling {project.RepositoryName}. Resetting to branch {project.MainBranch}");
        Commands.Checkout(repo, project.MainBranch);
        var commitsWithIssues = GetCommitsWIthIssues(repo, project);
        
        foreach (var commit in commitsWithIssues)
        {
            Console.WriteLine($"Starting analysis for {commit.MessageShort} has {commit.Parents.Count()} parents");
            foreach (var parentCommit in commit.Parents)
            {
                Console.WriteLine($"Checking out parent {commit.MessageShort}");
                var codeChanges = repo.Diff.Compare<Patch>(commit.Tree, parentCommit.Tree) // Get all changes compared to the parent
                    .SelectMany(x => x.GetFileChangesFromPatch()).ToList();
                if (codeChanges.All(x => Path.GetExtension(x.Path) != ".cs"))
                {
                    _logger.LogInformation("Commit didn't change any csharp files");
                    continue;
                }
                Console.WriteLine("Checking out before");
                var before = await MetricsForCommit(project, repo, parentCommit, codeChanges.Select(x => x.Removed));
                Console.WriteLine("Checking out after");
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

    private List<FunctionOutput> GetScorePerFunction(List<MethodWithMetrics> before, List<MethodWithMetrics> after)
    {
        var allFunctions = before.Concat(after)
            .Select(x => new {x.PurityScore.Report.FullName, Class = x.PurityScore.Report.Namespace + "." + x.PurityScore.Report.Name.Split(".").First()}).ToList();
                
        var data = allFunctions.Select(func =>
        {
            var b = before.FirstOrDefault(x => x.PurityScore.Report.FullName == func.FullName);
            var a = after.FirstOrDefault(x => x.PurityScore.Report.FullName == func.FullName);
            var rating = new FunctionOutput(func.FullName, b, a);
            return rating;
        }).ToList();

        return data;
    }

    private async Task<List<MethodWithMetrics>> MetricsForCommit(TargetProject project, Repository repository, Commit commit, IEnumerable<LinesChange> codeChanges)
    {
        CheckoutCommit(repository, commit); // Get all changes since last state on disk
        var solutionFile = project.SolutionFile.Select(x => Path.Combine(project.RepoPath, x))
            .First(File.Exists); //FInd the right solution file.
                
        var metrics = await AnalyseCode(solutionFile, codeChanges.ToList());

        return metrics;
    }

    private async Task<List<MethodWithMetrics>> AnalyseCode(string solution, List<LinesChange> changes)
    {
       
        var runner = new OptimizedMetricRunner(_purityAnalyser, _purityCalculator);
        var metrics = await runner.Run(solution, changes);
        Console.WriteLine($"Found metrics for {metrics.Count} methods");
        return metrics;
    }

    private void CheckoutCommit(Repository repository, Commit commit)
    {
        Commands.Checkout(repository, commit);
        currentCommitState = commit;
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