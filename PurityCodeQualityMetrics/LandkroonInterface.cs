using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Purity.Storage;

namespace PurityCodeQualityMetrics;

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

    public async Task Run(string repoPath, List<string> solutionFile)
    {
        //Get commits/ with issues
        var repo = new Repository(repoPath);
        
        var bugCommits = repo.Commits
            .Where(x => x.Message.Contains("bug", StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        


        foreach (var b in bugCommits)
        {
            var parents = b.Parents.ToList();

            _logger.LogInformation($"Checking out commit {b.MessageShort} this has {parents.Count} parents");


            if (parents.Count == 1)
            {
                var p = parents.First();
                var stateDff = lastState == null ? new List<string>() : repo.Diff
                    .Compare<Patch>(lastState.Tree, p.Tree).Select(x => x.Path)
                    .Select(x => x.Replace('/', '\\')).ToList();
                
                var diffs = repo.Diff.Compare<Patch>(b.Tree, p.Tree).ToList();
                _logger.LogInformation($"Checking out parent {p.MessageShort} - {diffs.Count}");
              //  Commands.Checkout(repo, p);
                lastState = p;
                await CheckMethods(solutionFile.Select(x => Path.Combine(repoPath, x)).ToList(), diffs, stateDff);
            }
            else
            {
                _logger.LogInformation($"More then one parent skipping");
            }
        }
    }

    public async Task CheckMethods(List<string> solutionPaths, List<PatchEntryChanges> changesList, List<string> filesToCheck)
    {
        try
        {
            var solutionPath = solutionPaths.FirstOrDefault(File.Exists);
            
            var changes = changesList.Select(x => x.Path)
                .Select(x => x.Replace('/', '\\'))
                .Where((x => x.EndsWith(".cs", StringComparison.CurrentCultureIgnoreCase)))
                .ToList();
            
            if (!changes.Any())
            {
                _logger.LogInformation("No c# file changed");
                return;
            }

            var potentialMethodsChanged = changesList
                .SelectMany(x => x.Patch.Split(new []{"\r\n", "\n"}, StringSplitOptions.None))
                .Where(x => x.StartsWith("-"))
                .Select(x => Regex.Matches(x, @"[a-zA-Z]*\s*[a-zA-Z]*\s*\("))
                .SelectMany(x => x.Select(x => x.Value)).ToList();

            var reports = await _purityAnalyser.GeneratePurityReports(solutionPath, filesToCheck);
            _purityReportRepo.AddRange(reports);
            var scores = _purityCalculator.CalculateScores(_purityReportRepo.GetAllReports());
            /*
             *   val patchMatch = """@@ -((\d*),(\d*)) \+((\d*),(\d*)) @@""".r findAllMatchIn  patchValue
            patchMatch.foldLeft(List[(Int, Int, Int, Int)]()){
              (a, value) =>
                val startLineDel = value.group(2).toInt
                val stopLineDel = value.group(2).toInt + value.group(3).toInt
                val startLineAdd = value.group(5).toInt
                val stopLineAdd = value.group(5).toInt + value.group(6).toInt
             */
            
            
            var changedScores = scores.Where(x => true ||
                    changes.Any(y => x.Report.FilePath.Contains(y, StringComparison.CurrentCultureIgnoreCase))
                    && potentialMethodsChanged.Any(y => y.Contains(x.Report.Name.Split(".")[1])))
                .ToList();
            WriteData(changedScores);

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
                purityScore.Violations.Count(x => x.Violation == PurityViolation.ModifiesParameters),
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