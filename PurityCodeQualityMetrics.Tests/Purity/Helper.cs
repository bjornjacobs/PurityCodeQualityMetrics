using Microsoft.Build.Locator;
using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.Tests.Purity;

public class ViolationsTest : Attribute
{
    public PurityViolation[] Value { get; private set; }

    public ViolationsTest(params PurityViolation[] value)
    {
        Value = value;
    }
}

public class Helper
{
    private static List<PurityReport>? _cache = null;

    private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, Int32.MaxValue);

    public static async Task<List<PurityReport>> GetReports(PurityAnalyser _sut)
    {
        await Semaphore.WaitAsync();

        if (_cache != null)
        {
            Semaphore.Release();
            return _cache;
        }

        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();

        //Load itself to get reports for testclasses
        var dir = Directory.GetCurrentDirectory();
        const string projectDir = "PurityCodeQualityMetrics";
        const string testPath = "/PurityCodeQualityMetrics.Tests/PurityCodeQualityMetrics.Tests.csproj";

        var testProject = dir.Split(projectDir).First() + projectDir + testPath;
        _cache = await _sut.GeneratePurityReportsProject(testProject);
        Semaphore.Release();
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