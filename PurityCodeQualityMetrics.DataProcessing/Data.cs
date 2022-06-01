using Newtonsoft.Json;
using PurityCodeQualityMetrics.GitCrawler;
using PurityCodeQualityMetrics.Purity;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PurityCodeQualityMetrics.DataProcessing;

public class Data
{
    public static string Dir = @"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\data\results";

    public static void Rewrite()
    {
        var files = Directory.GetFiles(Dir)
            .Where(x => !x.Contains("-final"))
            .Where(x => Path.GetExtension(x).Contains("json", StringComparison.CurrentCultureIgnoreCase))
            .Select(x => (x, File.ReadAllText(x)))
            .Select(x => (x.x, JsonConvert.DeserializeObject<List<FunctionOutput>>(x.Item2)))
            .ToList();
Console.WriteLine("Read all");
        
        foreach (var file in files)
        {
            File.WriteAllText(file.x, JsonSerializer.Serialize(file.Item2));
        }
    }
    
    public static  List<MethodWithMetrics> GetFinalData(string file = "")
    {
        Console.WriteLine(file);
        if (!string.IsNullOrWhiteSpace(file))
        {
            var json = File.ReadAllText(FuzzyFile(file, final: true));
            var data = JsonConvert.DeserializeObject<List<MethodWithMetrics>>(json);
            return data;
        }
    
        var files = Directory.GetFiles(Dir)
            .Where(x => x.Contains("final"))
            .Where(x => Path.GetExtension(x).Contains("json", StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        return files.Select(File.ReadAllText).SelectMany(x => JsonConvert.DeserializeObject<List<MethodWithMetrics>>(x)!)
            .Where(x => x.HasAllMetrics()).ToList();
    }

    public static List<FunctionOutput> GetData(string file = "")
    {
        if (!string.IsNullOrWhiteSpace(file))
        {
            var json = File.ReadAllText(FuzzyFile(file));
            var data = JsonConvert.DeserializeObject<List<FunctionOutput>>(json);
            return data;
        }
    
        var files = Directory.GetFiles(Dir)
            .Where(x => !x.Contains("final"))
            .Where(x => Path.GetExtension(x).Contains("json", StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        
        
        var d = files.Select(File.ReadAllText).SelectMany(x => JsonConvert.DeserializeObject<List<FunctionOutput>>(x)!).ToList();
        return d.GroupBy(x => x.CommitHash).SelectMany(x => x)
            .Where(x => x.Before != null && x.Before.HasAllMetrics()).ToList();
    }

    public static string FuzzyFile(string name, bool final = false)
    {
        var filename = Directory.GetFiles(Dir)
            .Where(x => x.Contains("final") == final)
            .FirstOrDefault(x => x.Contains(name, StringComparison.CurrentCultureIgnoreCase));
        if (string.IsNullOrWhiteSpace(filename))
            throw new Exception($"Could not find file {name}");

     //   Console.WriteLine($"Reading data from {filename}");
        return filename;
    }   

}


public static class DataHelper
{
    public static bool HasAllMetrics(this MethodWithMetrics m)
    {
        return m.Metrics.Count == 14;
    }
}