using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics;
using PurityCodeQualityMetrics.GitCrawler;
using PurityCodeQualityMetrics.Purity;


var factory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Error));
var analyser = new PurityAnalyser(factory.CreateLogger<PurityAnalyser>());
var calculator = new PurityCalculator(factory.CreateLogger<PurityCalculator>());

var l = new LandkroonInterface(new OwnLogger(), analyser, calculator);
await l.Run(TargetProject.ByName("ILSpy")!);