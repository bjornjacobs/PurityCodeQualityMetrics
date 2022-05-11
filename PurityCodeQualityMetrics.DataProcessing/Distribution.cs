using PurityCodeQualityMetrics.GitCrawler;
using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.DataProcessing;

public class Distribution
{
    public static void Generate(List<FunctionOutput> data)
    {
        var before = data.Select(x => x.Before)
            .Where(x => x != null);

        var r = before.SelectMany(x => x.Violations)
            .Where(x => x.Violation != PurityViolation.UnknownMethod)
            .Where(x => x.Distance == 0)
            .Select(x => x.Violation);
        
        var oneprecent = r.Count() / 100d;
        var dic = r.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
        Console.Write("\\addplot coordinates {");
        Console.Write($"(R-Local, {dic.GetValueOrDefault(PurityViolation.ReadsLocalState) / oneprecent:#})");
        Console.Write($"(W-Local, {dic.GetValueOrDefault(PurityViolation.ModifiesLocalState) / oneprecent:#})");
        Console.Write($"(R-Global, {dic.GetValueOrDefault(PurityViolation.ReadsGlobalState) / oneprecent:#})");
        Console.Write($"(W-Global, {dic.GetValueOrDefault(PurityViolation.ModifiesGlobalState) / oneprecent:#})");
        Console.Write($"(W-Param, {dic.GetValueOrDefault(PurityViolation.ModifiesParameter) / oneprecent:#})");
        Console.Write($"(W-nonFresh, {dic.GetValueOrDefault(PurityViolation.ModifiesNonFreshObject) / oneprecent:#})");
        Console.WriteLine("};");

    }
}