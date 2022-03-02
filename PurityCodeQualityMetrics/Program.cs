using System.Diagnostics;
using CommandLine;
using Microsoft.Build.Locator;
using PurityAnalyzer = PurityCodeQualityMetrics.Purity.PurityAnalyzer;


await Parser.Default.ParseArguments<CommandLineOptions>(args)
    .WithParsedAsync(async o =>
    {
        MSBuildLocator.RegisterDefaults();
        
        var analyzer = new PurityAnalyzer();
        var purityReports = await analyzer.GeneratePurityReports("");


        var watch = Stopwatch.StartNew();

        watch.Stop();
        Console.WriteLine($"Elapsed: {watch.Elapsed}");
    });


class CommandLineOptions
{
    [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.", Default = false)]
    public bool Verbose { get; set; }
}