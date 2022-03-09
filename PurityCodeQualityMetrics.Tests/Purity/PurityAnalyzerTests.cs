using Microsoft.Build.Locator;
using PurityCodeQualityMetrics.Purity;using PurityCodeQualityMetrics.Tests.Purity;
using PurityCodeQualityMetrics.Tests.Purity.TestCode;
using Xunit;

using static PurityCodeQualityMetrics.Tests.Purity.Helper;

namespace PurityCodeQualityMetrics.Tests.Purity;

public class PurityAnalyzerTests
{
    private readonly PurityAnalyzer _sut = new();
    
    //For the use of nameof()
    private static readonly GloballyImpureTestClass GloballyImpureTestClassInstance = new();
    private static readonly PureFunctionsTestCases PureFunctionsTestCases = new();
    

    public static IEnumerable<object[]> GloballyTestData =>
        new List<object[]>
        {
            GenerateTestData(nameof(GloballyImpureTestClassInstance.ModifyAndReturn), PurityViolation.ModifiesGlobalState, PurityViolation.ReadsGlobalState),
            GenerateTestData(nameof(GloballyImpureTestClassInstance.ModifyAndReturnOtherClass), PurityViolation.ModifiesGlobalState, PurityViolation.ReadsGlobalState),
            GenerateTestData(nameof(GloballyImpureTestClassInstance.UseInExpression), PurityViolation.ReadsGlobalState, PurityViolation.ReadsGlobalState),
            GenerateTestData(nameof(GloballyImpureTestClassInstance.AssignToExistingVariable),  PurityViolation.ReadsGlobalState),
            GenerateTestData(nameof(GloballyImpureTestClassInstance.AssignToNewVariable), PurityViolation.ReadsGlobalState)
        };

    public static IEnumerable<object[]> PureFunctionsTestData =>
        new List<object[]>
        {
            GenerateTestData(nameof(PureFunctionsTestCases.PureFunction1)),
            GenerateTestData(nameof(PureFunctionsTestCases.PureFunction2)),
            GenerateTestData(nameof(PureFunctionsTestCases.PureFunctionLambda)),
            GenerateTestData(nameof(PureFunctionsTestCases.PureFunctionParameters))
        };

    [Theory]
    [MemberData(nameof(GloballyTestData))]
    [MemberData(nameof(PureFunctionsTestData))]
    public async Task TestCases(string name, List<PurityViolation> violations)
    {
        var reports = await GenerateReports(_sut);
        var r = reports.First(x => x.Name == name);
        violations.ForEach(x => Assert.Contains(x, r.Violations));
        Assert.Equal(violations.Count, r.Violations.Count);
    }
}