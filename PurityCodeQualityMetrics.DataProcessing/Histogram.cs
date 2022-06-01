
using PurityCodeQualityMetrics;
using PurityCodeQualityMetrics.GitCrawler;
using PurityCodeQualityMetrics.Purity;

public class Histogram
{
    
    public static void Generate(List<FunctionOutput> data)
    {
        var banda = data.Where(x => x.Before != null && x.After != null).ToList();

        var delta = banda
            .Select(x => x.Before.NormalizedViolationCount() - x.After.NormalizedViolationCount())
            .OrderBy(x => x)
            .Where(x => x != 0).ToList();

        var dis = delta
            .Select(x => Math.Round(x * 2, 1, MidpointRounding.AwayFromZero) / 2)
            .GroupBy(x => x)
            .Select(x => (x.Key, x.Count()))
            .OrderBy(x => x.Key)
            .ToList();

        for (double i = dis.MinBy(x => x.Key).Key; i <= dis.MaxBy(x => x.Key).Key; i += 0.05)
        {
            var x = dis.FirstOrDefault(x => Math.Abs(x.Key - i) < 0.01);
            x.Key = i;
            
            Console.Write($"({x.Key.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)},{x.Item2})");
        }

        Console.WriteLine();
        Console.WriteLine(delta.Select(x => (double)x).Average());
     //   foreach (var x in dis.OrderBy(x => x.Key))
       //     Console.Write($"({x.Key.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)},{x.Item2})");
    }    
}


static class Helper
{
    public static double NormalizedViolationCount(this MethodWithMetrics x)
    {
        var v = x.Violations.Where(x =>
            x.Violation != PurityViolation.UnknownMethod && x.Violation != PurityViolation.ThrowsException);

        return v.Count() / (double) x.TotalLinesOfSourceCode;
    }
}