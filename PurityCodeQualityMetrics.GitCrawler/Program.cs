using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics.Git;
using PurityCodeQualityMetrics.GitCrawler;
using PurityCodeQualityMetrics.GitCrawler.Issue;
using PurityCodeQualityMetrics.Purity;

var service =new IssueService();

var issues = service.GetIssues(TargetProject.GetTargetProjects()[2]);
Console.WriteLine(issues.Count);


var factory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Error));
var analyser = new PurityAnalyser(factory.CreateLogger<PurityAnalyser>());
var calculator = new PurityCalculator(factory.CreateLogger<PurityCalculator>());

var l = new LandkroonInterface(factory.CreateLogger<LandkroonInterface>(), analyser, calculator);
await l.Run(@"C:\Users\BjornJ\dev\repos\jellyfin",  new List<string>(){"Jellyfin.sln", "MediaBrowser.sln"}, "origin/master");
// await l.Run(@"C:\Users\BjornJ\dev\repos\akka.net",  new List<string>(){"src\\Akka.sln"}, "origin/master");
                
// await l.Run(@"C:\Users\BjornJ\dev\repos\test_repo",  new List<string>(){"test_repo.sln"}, "master");
return;
