using CommandLine;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Purity.Storage;

namespace PurityCodeQualityMetrics;

public class Program
{
    static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsedAsync(async o =>
            {
                var factory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
                var analyser = new PurityAnalyser(factory.CreateLogger<PurityAnalyser>());
                var calculator = new PurityCalculator(factory.CreateLogger<PurityCalculator>());

                var l = new LandkroonInterface(factory.CreateLogger<LandkroonInterface>(), analyser, calculator);
                // await l.Run(@"C:\Users\BjornJ\dev\repos\jellyfin",  new List<string>(){"Jellyfin.sln", "MediaBrowser.sln"});
                //   await l.Run(@"C:\Users\BjornJ\dev\repos\akka.net",  new List<string>(){"src\\Akka.sln"});
                
                
        
                // GitInterface.Main();
                
                o.Project =
                    @"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics\PurityCodeQualityMetrics.csproj"; 
                o.Project = @"C:\Users\BjornJ\dev\PureTest\PureTest.csproj";

                //  o.Project = @"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics.sln";

               
                var repo = new InMemoryReportRepo();
                repo.Clear();

                var analyzer = new PurityAnalyser(factory.CreateLogger<PurityAnalyser>());

                var purityReports = await analyzer.GeneratePurityReportsProject(o.Project);
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
            });
    }
}