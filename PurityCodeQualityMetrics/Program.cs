using System.Diagnostics;
using CommandLine;
using Microsoft.Build.Locator;
using PurityCodeQualityMetrics.Purity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using PurityCodeQualityMetrics.Purity.Storage;

public class Program
{
    static async Task Main(string[] args) =>
        await Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsedAsync(async o =>
            {
                await using var db = new DatabaseContext();
                await db.Database.EnsureDeletedAsync();
                
                MSBuildLocator.RegisterDefaults();
                o.Project =
                    @"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics\PurityCodeQualityMetrics.csproj";
              //  o.Project = @"C:\Users\BjornJ\dev\PureTest\PureTest.csproj";
                //   o.Project = @"C:\Users\BjornJ\dev\runtime\src\libraries\Microsoft.Extensions.Logging\src\Microsoft.Extensions.Logging.csproj";

                var factory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
                var analyzer = new PurityAnalyzer(factory.CreateLogger<PurityAnalyzer>());

                var purityReports = await analyzer.GeneratePurityReports(o.Project);
                IPurityReportRepo repo = new EfPurityRepo();
                repo.AddRange(purityReports);
                PrintOverview(repo);
            });


    static void PrintOverview(IPurityReportRepo repo)
    {
        var all = repo.GetAllReports().OrderBy(x => x.Name).ToList();
        Console.WriteLine("--- Methods and dependencies ---");
        foreach (var report in all)
        {
            Console.WriteLine($"{report.FullName}");
            if (report.Violations.Any())
            {
                Console.WriteLine(" Violations:");
                report.Violations.ForEach(x => Console.WriteLine($"  - {x.ToString()}"));
            }
            else
            {
                Console.WriteLine(" No violations");
            }

            if (report.Dependencies.Any())
            {
                Console.WriteLine(" Dependencies:");
                report.Dependencies.ForEach(x => Console.WriteLine($"  - {x.Name}"));
            }
            else
            {
                Console.WriteLine(" No dependencies");
            }
        }
        var unknown = repo.GetAllUnknownMethods().OrderBy(x => x).ToList();
        if (unknown.Any())
        {
            Console.WriteLine($" Unknown methods {unknown.Count}:");
            unknown.ForEach(x => Console.WriteLine($"  - {x}"));
        }
        else
        {
            Console.WriteLine(" All methods are known");
        }
    }
}

class CommandLineOptions
{
    [Option(shortName:'p', longName:"project")]
    public string Project { get; set; } = null!;

    [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.", Default = false)]
    public bool Verbose { get; set; }
}