using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics;
using PurityCodeQualityMetrics.GitCrawler;
using PurityCodeQualityMetrics.GitCrawler.Issue;
using PurityCodeQualityMetrics.Purity;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Repository = LibGit2Sharp.Repository;





args = new string[] { "roslyn" };

if (!args.Any())
{
    Console.WriteLine("No Project name provided. Exiting.......");
    Environment.Exit(-1);
}

var projectname = args.First();

var factory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Error));
var analyser = new PurityAnalyser(factory.CreateLogger<PurityAnalyser>());
var calculator = new PurityCalculator(factory.CreateLogger<PurityCalculator>());

var l = new LandkroonInterface(new OwnLogger(), analyser, calculator);
await l.Run(TargetProject.ByName(projectname)!);


void ResetAll()
{
    TargetProject.GetTargetProjects().ToList().ForEach(x =>
    {
        Console.WriteLine("Resetting {0}", x.RepositoryName);
        var repo = new Repository(x.RepoPath);
        repo.Reset(ResetMode.Hard, x.MainBranch);
    });
}