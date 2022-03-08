using Microsoft.Build.Locator;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Tests.Purity.TestCode;
using Xunit;

namespace PurityCodeQualityMetrics.Tests.Purity;

public class PurityAnalyzerTests
{
    private readonly PurityAnalyzer _sut = new();
    private readonly TestClass _testClassInstance = new(); //For the use of nameof()
    
    private async Task<IList<PurityReport>> GetReports()
    {
        if(!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();
        
        //Load itself to get reports for testclasses
        var dir = Directory.GetCurrentDirectory();
        const string projectDir = "PurityCodeQualityMetrics";
        const string testPath = "/PurityCodeQualityMetrics.Tests/PurityCodeQualityMetrics.Tests.csproj";
        
        var testProject = dir.Split(projectDir).First() + projectDir + testPath;
        var reports = await _sut.GeneratePurityReports(testProject);
        return reports;
    }

    [Fact]
    public async Task Pure()
    {
        var reports = await GetReports();
        var r = reports.First(x => x.Name == nameof(_testClassInstance.PureFunction));
        Assert.Empty(r.Violations);
    }

    [Fact]
    public async Task ModifiesLocal_ReadsLocal()
    {
        var reports = await GetReports();
        var r = reports.First(x => x.Name == nameof(_testClassInstance.LocallyImpure));
        Assert.Contains(PurityViolation.ModifiesLocalState, r.Violations);
        Assert.Contains(PurityViolation.ReadsLocalState, r.Violations);
        Assert.Equal(2, r.Violations.Count);
    }
    
    [Fact]
    public async Task ModifiesGlobal_ReadsGlobal()
    {
        var reports = await GetReports();
        var r = reports.First(x => x.Name == nameof(_testClassInstance.GloballyImpure));
        Assert.Contains(PurityViolation.ModifiesGlobalState, r.Violations);
        Assert.Contains(PurityViolation.ReadsGlobalState, r.Violations);
        Assert.Equal(2, r.Violations.Count);
    }
    
    [Fact]
    public async Task ModifiesGlobal_ReadsGlobal_OtherClass()
    {
        var reports = await GetReports();
        var r = reports.First(x => x.Name == nameof(_testClassInstance.GloballyImpureOtherClass));
        Assert.Contains(PurityViolation.ModifiesGlobalState, r.Violations);
        Assert.Contains(PurityViolation.ReadsGlobalState, r.Violations);
        Assert.Equal(2, r.Violations.Count);
    }
}