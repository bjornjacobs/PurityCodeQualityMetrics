using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Purity.Storage;
using Commit = LibGit2Sharp.Commit;

namespace PurityCodeQualityMetrics.GitCrawler;

public class LandkroonInterface
{
    private ILogger _logger;
    private PurityAnalyser _purityAnalyser;
    private PurityCalculator _purityCalculator;
    private Commit? lastState;
    private IPurityReportRepo _purityReportRepo;
    
    public LandkroonInterface(ILogger logger, PurityAnalyser purityAnalyser, PurityCalculator purityCalculator)
    {
        _logger = logger;
        _purityAnalyser = purityAnalyser;
        _purityCalculator = purityCalculator;
        _purityReportRepo = new InMemoryReportRepo();
    }

    public async Task Run(string repoPath, List<string> solutionFile, string mainBranch)
    {
        //Get commits/ with issues
        var repo = new Repository(repoPath);
        repo.Reset(ResetMode.Hard, mainBranch);
        
        var bugCommits = repo.Commits
         //   .Where(x => x.Message.Contains("bug", StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        
        


        foreach (var b in bugCommits)
        {
            Console.WriteLine($"{b.MessageShort}");
            var parents = b.Parents.ToList();

            _logger.LogInformation($"Checking out commit {b.MessageShort} this has {parents.Count} parents");
      
            if (parents.Count == 1)
            {
                var p = parents.First();
                var stateDff = lastState == null ? new List<string>() : repo.Diff
                    .Compare<Patch>(lastState.Tree, p.Tree).Select(x => x.Path)
                    .Select(x => x.Replace('/', '\\')).ToList();
                
                var diffs = repo.Diff.Compare<Patch>(b.Tree, p.Tree).ToList();
                var changes =  diffs.SelectMany(x => x.GetFileChangesFromPatch()).ToList();
                _purityReportRepo.RemoveClassesInFiles(changes.Select(x => x.Path).ToList());
                _logger.LogInformation($"Checking out parent {p.MessageShort} - {diffs.Count}");
                
                Commands.Checkout(repo, b);
                lastState = b;

                Console.WriteLine("\tAfter");
                await CheckMethods(solutionFile.Select(x => Path.Combine(repoPath, x)).ToList(), changes.Select(x => x.Added).ToList(), stateDff);
                
                
                Commands.Checkout(repo, p);
                lastState = p;
          
                Console.WriteLine("\tBefore");
                await CheckMethods(solutionFile.Select(x => Path.Combine(repoPath, x)).ToList(), changes.Select(x => x.Removed).ToList(), stateDff);

                
                
                Console.WriteLine();
            }
            else
            {
                _logger.LogInformation($"More then one parent skipping");
            }
        }
    }

    public async Task CheckMethods(List<string> solutionPaths, List<LinesChange> changesList, List<string> filesToCheck)
    {
        try
        {
            var solutionPath = solutionPaths.FirstOrDefault(File.Exists);
            
            if (!changesList.Any(x => x.Path.EndsWith(".cs", StringComparison.CurrentCultureIgnoreCase)))
            {
                _logger.LogInformation("No c# file changed");
                return;
            }

            var reports = await _purityAnalyser.GeneratePurityReports(solutionPath, filesToCheck, false);
            _purityReportRepo.AddRange(reports);
            var scores = _purityCalculator.CalculateScores(_purityReportRepo.GetAllReports(),(dep, context) =>
            {
                //Check if context actually needed.eg is in change list
                if (!context.ReportInChanges(changesList))
                    return null;
                
                
                var userGenerated = MissingMethodInput.FromConsole(dep);
                if (userGenerated == null) return null;
                _purityReportRepo.AddRange(new []{userGenerated});
                return userGenerated;   
            });

            var changedScores = changesList.GetChangedReports(scores);
            changedScores.ForEach(x => Console.WriteLine($"\t\t{x.Report.Name} - {x.Violations.Count} - {x.Puritylevel}"));
          //  WriteData(changedScores);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Error while analysing commit", e);
        }
    }

    public void WriteData(List<PurityScore> changed)
    {
        foreach (var purityScore in changed)
        {
            var s = new[]
            {
                purityScore.DependencyCount,
                purityScore.Violations.Count(x => x.Violation == PurityViolation.ThrowsException),
                purityScore.Violations.Count(x => x.Violation == PurityViolation.ModifiesParameter),
                purityScore.Violations.Count(x => x.Violation == PurityViolation.UnknownMethod),
                purityScore.Violations.Count(x => x.Violation == PurityViolation.ModifiesLocalState),
                purityScore.Violations.Count(x => x.Violation == PurityViolation.ReadsLocalState),
                purityScore.Violations.Count(x => x.Violation == PurityViolation.ModifiesGlobalState),
                purityScore.Violations.Count(x => x.Violation == PurityViolation.ReadsGlobalState),
            };
            var row = string.Join(", ", s) + Environment.NewLine;
            File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + "test.csv", row);
        }
    }
}