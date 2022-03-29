using CodeQualityAnalyzer.CodeMetrics;
using CommandLine;
using ConsoleTables;
using Microsoft.Build.Locator;
using PurityCodeQualityMetrics.Purity;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics;
using PurityCodeQualityMetrics.Purity.Storage;

public class Program
{
    static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsedAsync(async o =>
            {
                o.Project =
                    @"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics\PurityCodeQualityMetrics.csproj";
                //  o.Project = @"C:\Users\BjornJ\dev\PureTest\PureTest.csproj";

                o.Project = @"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics.sln";

                var factory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
                var repo = new EfPurityRepo();
                repo.Clear();

                var analyzer = new PurityAnalyser(factory.CreateLogger<PurityAnalyser>());
                var calculator = new PurityCalculator(factory.CreateLogger<PurityCalculator>());

                var purityReports = await analyzer.GeneratePurityReports(o.Project);
                repo.AddRange(purityReports);


                var runner = new MetricRunner(o.Project);
                var metrics = await runner.GetSolutionVersionWithMetrics(repo.GetAllReports());

                ConsoleTable.From(metrics.ClassesWithMetrics.Select(x => x.Value).
                    Select(x =>
                    new
                    {
                        Name = x.ClassName,
                        Impurity = x.MetricResult[Measure.Purity],
                        LambdaCount = x.MetricResult[Measure.LambdaCount],
                        LambdaScore = x.MetricResult[Measure.LambdaScore],
                        LambdaSideEffectCount = x.MetricResult[Measure.LambdaSideEffectCount],
                        CommentDensity = x.MetricResult[Measure.CommentDensity],
                        UnterminatedCollections = x.MetricResult[Measure.UnterminatedCollections],
                    })
                ).Write();


                //var scores = calculator.CalculateScores(repo.GetAllReports());
                // ConsoleInterface.PrintOverview(scores, true);
            });
    }
}