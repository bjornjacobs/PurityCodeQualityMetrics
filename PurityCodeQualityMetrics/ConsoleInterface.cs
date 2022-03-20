using CommandLine;
using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics;


class CommandLineOptions
{
    [Option(shortName: 'p', longName: "project")]
    public string Project { get; set; } = null!;

    [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.", Default = false)]
    public bool Verbose { get; set; }
}

public class ConsoleInterface
{
    public static void PrintOverview(IPurityReportRepo repo)
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

    public static void PrintOverview(List<PurityScore> scores)
    {
        foreach (var purityScore in scores)
        {
            Console.WriteLine($"{purityScore.Report.Name}: {purityScore.Puritylevel.ToString()} ({purityScore.Violations.Count})");
        }
    }
}