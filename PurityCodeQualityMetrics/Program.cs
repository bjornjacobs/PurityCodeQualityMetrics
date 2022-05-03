using CommandLine;
using ConsoleTables;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Purity.Storage;


var factory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Error));

var project =
//@"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics\PurityCodeQualityMetrics.csproj";
 @"C:\Users\BjornJ\dev\PureTest\PureTest.csproj";
 //@"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics.sln";

var repo = new InMemoryReportRepo();
repo.Clear();

var analyzer = new PurityAnalyser(factory.CreateLogger<PurityAnalyser>());
var calculator = new PurityCalculator(factory.CreateLogger<PurityCalculator>());

var purityReports = await analyzer.GeneratePurityReportsProject(project);
repo.AddRange(purityReports);

var score = calculator.CalculateScores(purityReports, (dependency, report) => null);


ConsoleTable.From(score.Select(x =>  new {Name = x.Report.Name, Violations = string.Join(",", x.Violations.Select(x => $"{x.Violation}({x.Distance})"))})).Write();