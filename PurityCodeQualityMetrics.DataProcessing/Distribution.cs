using PurityCodeQualityMetrics.GitCrawler;
using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.DataProcessing;

public class Distribution
{
    public static void Generate(List<MethodWithMetrics> data)
    {
        
        var r = data.SelectMany(x => x.Violations)
            .Where(x => x.Violation != PurityViolation.UnknownMethod && x.Violation != PurityViolation.ThrowsException)
            .Where(x => x.Distance == 0)
            .Select(x => x.Violation)
            .ToList();

        var oneprecent = r.Count / 100d;

        var dic = r.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
        var other = new Dictionary<PurityViolation, double>();
        foreach (var x in dic)
        {
            other[x.Key] = x.Value / oneprecent;
        }
        
        
        Console.Write("\\addplot coordinates {");
        Console.Write($"(R-Local, {other.GetValueOrDefault(PurityViolation.ReadsLocalState) :#})");
        Console.Write($"(W-Local, {other.GetValueOrDefault(PurityViolation.ModifiesLocalState):#})");
        Console.Write($"(R-Global, {other.GetValueOrDefault(PurityViolation.ReadsGlobalState) :#})");
        Console.Write($"(W-Global, {other.GetValueOrDefault(PurityViolation.ModifiesGlobalState):#})");
        Console.Write($"(W-Param, {other.GetValueOrDefault(PurityViolation.ModifiesParameter) :#})");
        Console.Write($"(W-nonFresh, {other.GetValueOrDefault(PurityViolation.ModifiesNonFreshObject):#})");
        Console.WriteLine("};");

    }
}