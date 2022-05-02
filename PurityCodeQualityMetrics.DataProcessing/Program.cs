
using System.Text.Json;
using ConsoleTables;
using PurityCodeQualityMetrics.CodeMetrics;
using PurityCodeQualityMetrics.GitCrawler;

var json = File.ReadAllText(Path.Combine(LandkroonInterface.OutputDir, "IdentityServer4.json"));

var data = JsonSerializer.Deserialize<List<FunctionOutput>>(json);

var output = data.Select(x => new
{
    Name = x.FullName,
    PurityBefore = x.Before?.PurityScore.Violations?.Count ?? -1,
    PurityAfter = x.After?.PurityScore.Violations?.Count ?? -1,
});

ConsoleTable.From(output).Write();