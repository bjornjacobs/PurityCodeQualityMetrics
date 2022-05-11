using CommandLine;
using ConsoleTables;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Purity.Storage;

var factory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Error));
var logger = new OwnLogger();
logger.LogInformation("");

var project =
 //@"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics\PurityCodeQualityMetrics.csproj";
 //@"C:\Users\BjornJ\dev\PureTest\PureTest.csproj";
 //@"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics.sln";
 @"C:\Users\BjornJ\dev\repos\MoreLINQ\MoreLinq\MoreLinq.csproj";
// @"C:\Users\BjornJ\dev\repos\jellyfin\Jellyfin.sln";
// @"C:\Users\BjornJ\dev\repos\roslyn\Roslyn.sln";
 //@"C:\Users\BjornJ\dev\repos\IdentityServer4\src\IdentityServer4\IdentityServer4.sln";
// @"C:\Users\BjornJ\dev\repos\akka.net\src\Akka.sln";
 //@"C:\Users\BjornJ\dev\repos\ILSpy\ILSpy.sln";
 //@"C:\Users\BjornJ\dev\repos\machinelearning\Microsoft.ML.sln";
 //@"C:\Users\BjornJ\dev\repos\stryker-net\src\Stryker.sln";
var repo = new InMemoryReportRepo();
repo.Clear();

var analyzer = new PurityAnalyser(logger);
var calculator = new PurityCalculator(logger);

var purityReports = project.EndsWith(".sln") ? await analyzer.GeneratePurityReports(project) : await analyzer.GeneratePurityReportsProject(project);
repo.AddRange(purityReports);

var score = calculator.CalculateScores(purityReports, (dependency, report) => null);

//ConsoleTable.From(score.Select(x =>  new {Name = x.Report.Name, Violations = string.Join(",", x.Violations.Select(x => $"{x.Violation}({x.Distance})"))})).Write();
//ConsoleTable.From(score.Select(x =>  new {Name = x.Report.Name, Violations = x.Puritylevel})).Write();
//score.ForEach(x => x.CalculateLevel(0));
var g = score.SelectMany(x => x.Violations.Where(x => x.Distance == 0 && x.Violation != PurityViolation.ThrowsException)
 .Select(x => x.Violation)).GroupBy(x => x);
int total = g.Sum(x => x.Count());
foreach(var x in g)
 Console.WriteLine($"{x.Key}, {x.Count()}, {x.Count() / (total / 100d):0.##}%");

var oneprecent = total / 100d;


var dic = g.ToDictionary(x => x.Key, x => x.Count());
Console.Write("\\addplot coordinates {");
Console.Write($"(R-Local, {dic.GetValueOrDefault(PurityViolation.ReadsLocalState) / oneprecent:#})");
Console.Write($"(W-Local, {dic.GetValueOrDefault(PurityViolation.ModifiesLocalState) / oneprecent:#})");
Console.Write($"(R-Global, {dic.GetValueOrDefault(PurityViolation.ReadsGlobalState) / oneprecent:#})");
Console.Write($"(W-Global, {dic.GetValueOrDefault(PurityViolation.ModifiesGlobalState) / oneprecent:#})");
Console.Write($"(W-Param, {dic.GetValueOrDefault(PurityViolation.ModifiesParameter) / oneprecent:#})");
Console.Write($"(W-nonFresh, {dic.GetValueOrDefault(PurityViolation.ModifiesNonFreshObject) / oneprecent:#})");
Console.WriteLine("};");
 
/// score.Where(x => x.Puritylevel == Puritylevel.Impure).ToList().ForEach(x => Console.WriteLine(x.Report.Name));