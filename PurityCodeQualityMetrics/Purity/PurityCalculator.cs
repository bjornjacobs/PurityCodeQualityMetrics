using Microsoft.Extensions.Logging;

namespace PurityCodeQualityMetrics.Purity;

public class PurityCalculator
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, PurityScore> _table = new Dictionary<string, PurityScore>();

    public PurityCalculator(ILogger<PurityCalculator> logger)
    {
        _logger = logger;
    }

    public List<PurityScore> CalculateScores(List<PurityReport> reports)
    {
        return reports.Select(x =>  CalculateScore(x, name => reports.FirstOrDefault(x => x.FullName == name))).ToList();
    }

    private PurityScore CalculateScore(PurityReport report, Func<string, PurityReport?> getReport)
    {
        if(report == null) return PurityScore.Unknown();
        
        if (_table.TryGetValue(report.FullName, out var val))
        {
            return val;
        }
        var score = new PurityScore(report);
        score.Violations.AddRange(report.Violations);
        _table.Add(report.FullName, score);
        
        var dependencies = report.Dependencies.Select(x => getReport(x.FullName)).Select(x => CalculateScore(x, getReport)).ToList();
        score.Violations.AddRange(dependencies.SelectMany(x=>x.Violations).ToList());
        score.Puritylevel = CalculateLevel(score.Violations);
        return score;
    }

    private Puritylevel CalculateLevel(List<PurityViolation> violations)
    {
        if (violations.Contains(PurityViolation.ReadsGlobalState) ||
            violations.Contains(PurityViolation.ModifiesGlobalState))
            return Puritylevel.Impure;
        
        if (violations.Contains(PurityViolation.ModifiesLocalState) ||
            violations.Contains(PurityViolation.ReadsLocalState))
            return Puritylevel.LocallyImpure;
        
        if (violations.Contains(PurityViolation.ModifiesParameters))
            return Puritylevel.ParameteclyImpure;
        
        if (violations.Contains(PurityViolation.ThrowsException))
            return Puritylevel.ThrowsException;

        return Puritylevel.Pure;
    }
}