﻿using CodeQualityAnalyzer.CodeMetrics;
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
                var factory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
                var analyser = new PurityAnalyser(factory.CreateLogger<PurityAnalyser>());
                var calculator = new PurityCalculator(factory.CreateLogger<PurityCalculator>());

                var l = new LandkroonInterface(factory.CreateLogger<LandkroonInterface>(), analyser, calculator);
               // await l.Run(@"C:\Users\BjornJ\dev\repos\jellyfin",  new List<string>(){"Jellyfin.sln", "MediaBrowser.sln"});
                await l.Run(@"C:\Users\BjornJ\dev\repos\akka.net",  new List<string>(){"src\\Akka.sln"});
                
                
                return;
               // GitInterface.Main();
                
                o.Project =
                    @"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics\PurityCodeQualityMetrics.csproj";
                //  o.Project = @"C:\Users\BjornJ\dev\PureTest\PureTest.csproj";

                o.Project = @"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics.sln";

               
                var repo = new EfPurityRepo();
                repo.Clear();

                var analyzer = new PurityAnalyser(factory.CreateLogger<PurityAnalyser>());

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