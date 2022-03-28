
using Microsoft.Extensions.Logging;
using Moq;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Tests.Purity.TestCode;
using Xunit;

namespace PurityCodeQualityMetrics.Tests.Purity;

public class ScoresCalculatorTests
{
    private static readonly Mock<ILogger> _logger = new Mock<ILogger>();
    
    private readonly PurityAnalyser _purityAnalyser = new(_logger.Object);
    private readonly PurityCalculator _purityCalculator = new(_logger.Object);

    private CyclesTestClass _testClass = new();
    
    [Fact]
    public async Task TestCycles()
    {
        var reports = await Helper.GenerateReports(_purityAnalyser);
        var scores = _purityCalculator.CalculateScores(reports);

        var strongComponent = new[]
        {
            nameof(_testClass.Func1),
            nameof(_testClass.Func2),
            nameof(_testClass.Func3)
        }.Select(name => scores.First(x => x.Report.Name.EndsWith(name)));

        foreach (var s in strongComponent)
        {
            Assert.Contains((1, PurityViolation.ReadsLocalState), s.Violations);
            Assert.DoesNotContain((1, PurityViolation.ModifiesLocalState), s.Violations);
        }


        var scoreF4 = scores.First(x => x.Report.Name.EndsWith(nameof(_testClass.Func4)));
        Assert.Contains((1, PurityViolation.ReadsLocalState), scoreF4.Violations);
        Assert.Contains((1, PurityViolation.ModifiesLocalState), scoreF4.Violations);
    }
}