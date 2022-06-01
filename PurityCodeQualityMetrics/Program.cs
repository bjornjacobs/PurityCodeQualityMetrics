using System.Text.Json;
using PurityCodeQualityMetrics;
using PurityCodeQualityMetrics.GitCrawler;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Purity.Storage;




var logger = new OwnLogger();
var analyzer = new PurityAnalyser(logger);
var calculator = new PurityCalculator(logger);

var _nonVolitilePurityRepo = new EfPurityRepo("standard_lib");


var project = TargetProject.ByName("reactive");

var metric = new OptimizedMetricRunner(analyzer, calculator, (dependency, report) => _nonVolitilePurityRepo.GetByFullName(dependency.FullName));

var slnFile = project.SolutionFile.Select(x => Path.Combine(project.RepoPath, x)).First(File.Exists);

var path = Path.Combine(@"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\data\results-test-env\", $"{project.RepositoryName}-final.json");

await metric.RunAll(slnFile, path);
