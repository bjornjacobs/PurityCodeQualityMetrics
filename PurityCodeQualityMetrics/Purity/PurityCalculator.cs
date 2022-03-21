using Microsoft.Extensions.Logging;
using StronglyConnectedComponents;

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
        _logger.LogInformation("Starting calculating scores");
        //Calculate the unkown methods: e.g. methods that we don't have a purity report on
        var allDependencies = reports.SelectMany(x => x.Dependencies).DistinctBy(x => x.FullName);
        var unknowns = allDependencies.Where(d => reports.All(r => d.FullName != r.FullName))
            .Select(x => x.FullName).ToList();

        _logger.LogInformation($"Program has {unknowns.Count} unknown methods");

        //Calculate the strongly connected components using Tarjan's algorithm
        //This solves the problem with recursion and cycles in de call graph inspired by the Hindley–Milner type system
        var graph = reports.Select(x => x.FullName).Concat(unknowns).ToArray();

        var components = graph.DetectCycles(arg =>
            reports.FirstOrDefault(x => x.FullName == arg)?.Dependencies.Select(x => x.FullName) ?? new List<string>()
        );
        
        _logger.LogInformation($"Program has {components.Count} strongly connected components");
        
        //Calculate the purity per component
        var finder = (string name) => reports.FirstOrDefault(x => x.FullName == name);
        return components.SelectMany(x => CalculateScore(x.Select(finder).Where(x => x != null).ToList()!, finder)).ToList();
    }

    private List<PurityScore> CalculateScore(List<PurityReport> component, Func<string, PurityReport?> getReport)
    {
        var violations = component.SelectMany(x => x.Violations).ToList();
        var scores = component.Select(x => new PurityScore(x, violations)).ToList();

        //Calculates dependency outside of component 
        foreach (var score in scores)
        {
            score.Violations.AddRange(
                score.Report.Dependencies
                    .Where(x => component.All(y => x.FullName != y.FullName))
                    .Select(x => _table.GetValueOrDefault(x.FullName))
                    .SelectMany(x =>
                        x == null ? new List<PurityViolation> {PurityViolation.UnknownMethod} : x.Violations)
                    .ToList()
            );
            _table.Add(score.Report.FullName, score);
        }

        return scores;
    }
}