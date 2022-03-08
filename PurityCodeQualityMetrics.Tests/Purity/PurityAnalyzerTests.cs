using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Tests.Purity.TestCode;
using Xunit;
using Xunit.Abstractions;

namespace PurityCodeQualityMetrics.Tests.Purity;

public class PurityAnalyzerTests
{
    private readonly PurityAnalyzer _sut = new();

    private readonly TestClass _testClassInstance = new(); //For the use of nameof()

    public PurityAnalyzerTests()
    {
        if(!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();
    }

    private async Task<IList<PurityReport>> GetReports()
    {
        var dir = Directory.GetCurrentDirectory();
        var projectDir = "PurityCodeQualityMetrics";
        var testPath = "/PurityCodeQualityMetrics.Tests/PurityCodeQualityMetrics.Tests.csproj";
        
        var testProject = dir.Split(projectDir).First() + projectDir + testPath;
        var reports = await _sut.GeneratePurityReports(testProject);
        return reports;
    }

    [Fact]
    public async Task AnalyzePure()
    {
        var reports = await GetReports();
        var r = reports.First(x => x.Name == nameof(_testClassInstance.GloballyImpure));
        Assert.Contains(PurityViolation.ModifiesGlobalState, r.Violations);
    }
}