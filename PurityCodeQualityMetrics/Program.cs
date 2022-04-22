using CommandLine;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Purity.Storage;


var factory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Error));

var project =
@"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics\PurityCodeQualityMetrics.csproj";
// @"C:\Users\BjornJ\dev\PureTest\PureTest.csproj";
 //@"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics.sln";

var repo = new InMemoryReportRepo();
repo.Clear();

var analyzer = new PurityAnalyser(factory.CreateLogger<PurityAnalyser>());

var purityReports = await analyzer.GeneratePurityReportsProject(project);
repo.AddRange(purityReports);

ConsoleInterface.PrintOverview(repo);


//  var runner = new MetricRunner(o.Project);
//    var metrics = await runner.GetSolutionVersionWithMetrics(repo.GetAllReports());

// ConsoleTable.From(metrics.ClassesWithMetrics.Select(x => x.Value).
//     Select(x =>
//     new
//     {
//         Name = x.ClassName,
//         Impurity = x.MetricResult[Measure.Purity],
//         LambdaCount = x.MetricResult[Measure.LambdaCount],
//         LambdaScore = x.MetricResult[Measure.LambdaScore],
//         LambdaSideEffectCount = x.MetricResult[Measure.LambdaSideEffectCount],
//         CommentDensity = x.MetricResult[Measure.CommentDensity],
//         UnterminatedCollections = x.MetricResult[Measure.UnterminatedCollections],
//     })
// ).Write();


//var scores = calculator.CalculateScores(repo.GetAllReports());
// ConsoleInterface.PrintOverview(scores, true);