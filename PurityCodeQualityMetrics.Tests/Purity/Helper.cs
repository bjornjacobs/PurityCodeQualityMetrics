using Microsoft.Build.Locator;
using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.Tests.Purity;

public class Helper
{
    private static IList<PurityReport>? _cache = null;

    public static async Task<IList<PurityReport>> GenerateReports(PurityAnalyzer _sut)
    {
        if (_cache != null) return _cache;

        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();

        //Load itself to get reports for testclasses
        var dir = Directory.GetCurrentDirectory();
        const string projectDir = "PurityCodeQualityMetrics";
        const string testPath = "/PurityCodeQualityMetrics.Tests/PurityCodeQualityMetrics.Tests.csproj";

        var testProject = dir.Split(projectDir).First() + projectDir + testPath;
        _cache = await _sut.GeneratePurityReports(testProject);
        return _cache;
    }

    public static object[] GenerateTestData(string methodName, params PurityViolation[] violations)
    {
        return new object[]
        {
            methodName,
            violations.ToList()
        };
    }
}