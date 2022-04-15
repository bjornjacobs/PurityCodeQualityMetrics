
using FluentAssertions;
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
        var reports = await Helper.GetReports(_purityAnalyser);
        var scores = _purityCalculator.CalculateScores(reports, (x, y) => null);

        var strongComponent = new[]
        {
            nameof(_testClass.Func1),
            nameof(_testClass.Func2),
            nameof(_testClass.Func3)
        }.Select(name => scores.First(x => x.Report.Name.EndsWith(name)));

        
        foreach (var s in strongComponent)
        {
            var noDistance = s.Violations.Select(x => x.Violation).ToList();
            noDistance.Should().Contain(PurityViolation.ReadsLocalState);
            noDistance.Should().NotContain(PurityViolation.ModifiesLocalState);
        }


        var scoreF4 = scores.First(x => x.Report.Name.EndsWith(nameof(_testClass.Func4)));
        Assert.Contains((3, PurityViolation.ReadsLocalState), scoreF4.Violations);
        Assert.Contains((0, PurityViolation.ModifiesLocalState), scoreF4.Violations);
    }
}