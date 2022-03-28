using CommandLine;
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
                o.Project = @"C:\Users\BjornJ\dev\PureTest\PureTest.csproj";

                var factory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
                var repo = new EfPurityRepo();


                var analyzer = new PurityAnalyser(factory.CreateLogger<PurityAnalyser>());
                var calculator = new PurityCalculator(factory.CreateLogger<PurityCalculator>());

                var purityReports = await analyzer.GeneratePurityReports(o.Project);
      

      
                repo.AddRange(purityReports);
                var scores = calculator.CalculateScores(repo.GetAllReports());
                ConsoleInterface.PrintOverview(scores, true);
            });
    }
}