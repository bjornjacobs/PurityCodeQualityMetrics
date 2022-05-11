
using PurityCodeQualityMetrics.GitCrawler;

public class Histogram
{
    
    public static void Generate(List<FunctionOutput> data)
    {
        var banda = data.Where(x => x.Before != null && x.After != null).ToList();
        
        var delta = banda
            .Select(x => x.After.NormalizedViolationCount() - x.Before.NormalizedViolationCount())
            .OrderBy(x => x)
            .Where(x => x != 0).ToList();

        var dis = delta
            .Select(x => Math.Round(x * 2, 1, MidpointRounding.AwayFromZero) / 2)
            .GroupBy(x => x)
            .Select(x => (x.Key, x.Count()))
            .OrderBy(x => x.Key)
            .ToList();

        foreach (var x in dis.OrderBy(x => x.Key))
            Console.Write($"({x.Key.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)},{x.Item2})");
    }    
}
