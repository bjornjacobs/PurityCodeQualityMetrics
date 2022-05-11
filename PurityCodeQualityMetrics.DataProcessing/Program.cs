using System.Text.Json;
using System.Text.Json.Serialization;
using ConsoleTables;
using PurityCodeQualityMetrics;
using PurityCodeQualityMetrics.CodeMetrics;
using PurityCodeQualityMetrics.DataProcessing;
using PurityCodeQualityMetrics.GitCrawler;
using PurityCodeQualityMetrics.Purity;



var data = GetData("jelly");

Distribution.Generate(data);


List<FunctionOutput> GetData(string file = "")
{
    if (!string.IsNullOrWhiteSpace(file))
    {
        var json = File.ReadAllText(FuzzyFile(file));
        var data = JsonSerializer.Deserialize<List<FunctionOutput>>(json);
        return data;
    }
    
    var files = Directory.GetFiles(LandkroonInterface.OutputDir)
        .Where(x => Path.GetExtension(x).Contains("json", StringComparison.CurrentCultureIgnoreCase))
        .ToList();
    return files.Select(File.ReadAllText).SelectMany(x => JsonSerializer.Deserialize<List<FunctionOutput>>(x)!).ToList();
}

string FuzzyFile(string name)
{
    var filename = Directory.GetFiles(LandkroonInterface.OutputDir)
        .Single(x => x.Contains(name, StringComparison.CurrentCultureIgnoreCase));

    Console.WriteLine($"Reading data from {filename}");
    return filename;
}   

static class Helper
{
    public static double NormalizedViolationCount(this MethodWithMetrics x)
    {
        var v = x.Violations.Where(x => x.Violation != PurityViolation.UnknownMethod);
        
        return v.Count() / (double) x.TotalLinesOfSourceCode;
    }
}


