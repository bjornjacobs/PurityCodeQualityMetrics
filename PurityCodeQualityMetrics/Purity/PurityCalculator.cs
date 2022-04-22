using Microsoft.Extensions.Logging;
using StronglyConnectedComponents;

namespace PurityCodeQualityMetrics.Purity;

public class PurityCalculator
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, PurityScore> _table = new Dictionary<string, PurityScore>();

    public PurityCalculator(ILogger logger)
    {
        _logger = logger;
    }

    public List<PurityScore> CalculateScores(List<PurityReport> reports,
        Func<MethodDependency, PurityReport, PurityReport?> GetUnknown)
    {
        _table.Clear();
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
        return components.SelectMany(x =>
            CalculateScoreForComponent(x.Select(finder).Where(x => x != null).ToList()!, finder, GetUnknown)).ToList();
    }

    private List<PurityScore> CalculateScoreForComponent(List<PurityReport> component,
        Func<string, PurityReport?> getReport, Func<MethodDependency, PurityReport, PurityReport?> GetUnknown)
    {
        var componentsLines = component.Sum(x => x.SourceLinesOfCode);
        var scores = component.Select(x => new PurityScore(x, new List<ViolationWithDistance>(), componentsLines))
            .ToList();
        var distances = FloydWarshall(component);

        var componentDepCount = component.Sum(x => x.Dependencies.Count);

        //Calculates dependency outside of component 
        foreach (var score in scores)
        {
            var scoreIndex = component.IndexOf(score.Report);
            score.Violations.AddRange(score.Report.Violations.Select(x => new ViolationWithDistance(0, x)));
            foreach (var d in component.Where(x => x != score.Report))
            {
                var depIndex = component.IndexOf(d);
                var dis = distances[scoreIndex, depIndex];

                score.Violations.AddRange(d.Violations.Select(x => new ViolationWithDistance(dis, x)));
            }

            PurityScore? GetUnknownScore(MethodDependency d)
            {
                var report = GetUnknown(d, score.Report);
                if (report == null) return null;

                var s = new PurityScore(report, report.Violations.Select(x => new ViolationWithDistance(1, x)).ToList(),
                    report.SourceLinesOfCode);
                s.ReturnIsFresh = report.ReturnValueIsFresh;
                s.CalculateLevel();
                _table[d.FullName] = s;
                return s;
            }

            var depsOutside = score.Report.Dependencies
                .Where(x => component.All(y => x.FullName != y.FullName))
                .Select(x => _table.GetValueOrDefault(x.FullName) ?? GetUnknownScore(x)).ToList();

            score.Violations.AddRange(
                depsOutside
                    .SelectMany(x =>
                        x == null
                            ? new List<ViolationWithDistance>
                                {new ViolationWithDistance(1, PurityViolation.UnknownMethod)}
                            : x.Violations.Select(x => new ViolationWithDistance(x.Distance + 1, x.Violation)))
                    .ToList()
            );
            score.DependencyCount = depsOutside.Sum(x => x?.DependencyCount ?? 0) + componentDepCount;
            score.LinesOfSourceCode = depsOutside.Sum(x => x?.LinesOfSourceCode ?? 0) + componentsLines;

            score.ReturnIsFresh = score.Report.ReturnValueIsFresh &&
                                  score.Report.Dependencies.Where(x =>
                                          x.FreshDependsOnMethodReturnIsFresh &&
                                          component.All(y => x.FullName != y.FullName))
                                      .Select(x => _table.GetValueOrDefault(x.FullName))
                                      .All(x => x?.ReturnIsFresh ??
                                                true); //If unknown make the assumption that it is fresh

            score.CalculateLevel();
            _table.Add(score.Report.FullName, score);
        }

        return scores;
    }


    public static int[,] FloydWarshall(List<PurityReport> component)
    {
        int[,] graph = new int[component.Count, component.Count];
        for (int x = 0; x < component.Count; x++)
        {
            for (int y = 0; y < component.Count; y++)
            {
                graph[x, y] = component[x].Dependencies.Any(d => component[y].FullName == d.FullName) ? 1 : 1_000_000;
            }
        }

        var verticesCount = component.Count;

        int[,] distance = new int[verticesCount, verticesCount];

        for (int i = 0; i < verticesCount; ++i)
        for (int j = 0; j < verticesCount; ++j)
            distance[i, j] = graph[i, j];

        for (int k = 0; k < verticesCount; ++k)
        {
            for (int i = 0; i < verticesCount; ++i)
            {
                for (int j = 0; j < verticesCount; ++j)
                {
                    if (distance[i, k] + distance[k, j] < distance[i, j])
                        distance[i, j] = distance[i, k] + distance[k, j];
                }
            }
        }

        return distance;
    }
}