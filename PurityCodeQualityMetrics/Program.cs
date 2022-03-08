using System.Diagnostics;
using CommandLine;
using Microsoft.Build.Locator;
using PurityCodeQualityMetrics.Purity;
using PurityAnalyzer = PurityCodeQualityMetrics.Purity.PurityAnalyzer;


await Parser.Default.ParseArguments<CommandLineOptions>(args)
    .WithParsedAsync(async o =>
    {
        MSBuildLocator.RegisterDefaults();
        o.Project = @"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics\PurityCodeQualityMetrics.csproj";
        o.Project = @"C:\Users\BjornJ\dev\PureTest\PureTest.csproj";
     //   o.Project = @"C:\Users\BjornJ\dev\runtime\src\libraries\Microsoft.Extensions.Logging\src\Microsoft.Extensions.Logging.csproj";
        
        var analyzer = new PurityAnalyzer();
        var purityReports = await analyzer.GeneratePurityReports(o.Project);
        IPurityReportRepo repo = new InMemoryPurityRepo(purityReports);
        foreach (var report in purityReports)
        {
            Console.WriteLine(report.ToString());
        }
    });


class CommandLineOptions
{
    [Option(shortName:'p', longName:"project")]
    public string Project { get; set; }
    
    [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.", Default = false)]
    public bool Verbose { get; set; }
}