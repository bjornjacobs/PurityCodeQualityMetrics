
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using Moq;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Tests.Purity;
using PurityCodeQualityMetrics.Tests.Purity.TestCode;
using Xunit;
using static PurityCodeQualityMetrics.Tests.Purity.Helper;

namespace PurityCodeQualityMetrics.Tests.Purity;

public class ViolationsPolicyTests
{
    private static Mock<ILogger> _logger = new Mock<ILogger>();
    private readonly PurityAnalyser _sut = new(_logger.Object);

    //For the use of nameof()
    private static readonly GloballyImpureTestClass GloballyImpureTestClassInstance = new();
    private static readonly PureFunctionsTestCases PureFunctionsTestCases = new();
    private static readonly LocallyImpureTestClass LocallyImpureTestClass = new();


    public static IEnumerable<object[]> GloballyTestData =>
        new List<object[]>
        {
            GenerateTestData(nameof(GloballyImpureTestClassInstance.ModifyAndReturn),
                PurityViolation.ModifiesGlobalState, PurityViolation.ReadsGlobalState),
            GenerateTestData(nameof(GloballyImpureTestClassInstance.ModifyAndReturnOtherClass),
                PurityViolation.ModifiesGlobalState, PurityViolation.ReadsGlobalState),
            GenerateTestData(nameof(GloballyImpureTestClassInstance.UseInExpression), PurityViolation.ReadsGlobalState,
                PurityViolation.ReadsGlobalState),
            GenerateTestData(nameof(GloballyImpureTestClassInstance.AssignToExistingVariable),
                PurityViolation.ReadsGlobalState),
            GenerateTestData(nameof(GloballyImpureTestClassInstance.AssignToNewVariable),
                PurityViolation.ReadsGlobalState)
        };

    public static IEnumerable<object[]> PureFunctionsTestData =>
        new List<object[]>
        {
            GenerateTestData(nameof(PureFunctionsTestCases.PureFunction1)),
            GenerateTestData(nameof(PureFunctionsTestCases.PureFunction2)),
            GenerateTestData(nameof(PureFunctionsTestCases.PureFunctionLambda)),
            GenerateTestData(nameof(PureFunctionsTestCases.PureFunctionParameters))
        };
    
    public static IEnumerable<object[]> LocallyImpureTestData =>
        new List<object[]>
        {
            GenerateTestData(nameof(LocallyImpureTestClass.LocallyImpure), PurityViolation.ModifiesLocalState, PurityViolation.ReadsLocalState),
            GenerateTestData(nameof(LocallyImpureTestClass.LocallYImpure2), PurityViolation.ModifiesLocalState),
            GenerateTestData(nameof(LocallyImpureTestClass.LocallYImpure3), PurityViolation.ReadsLocalState, PurityViolation.ReadsLocalState),
            GenerateTestData(nameof(LocallyImpureTestClass.NoLocalImpure), PurityViolation.ReadsGlobalState),
        };

    [Theory]
    [MemberData(nameof(GloballyTestData))]
    [MemberData(nameof(PureFunctionsTestData))]
    [MemberData(nameof(LocallyImpureTestData))]
    public async Task TestCases(string name, List<PurityViolation> violations)
    {
        var reports = await GenerateReports(_sut);
        var r = reports.First(x => x.Name.EndsWith(name));
        violations.ForEach(x => Assert.Contains(x, r.Violations));
        Assert.Equal(violations.Count, r.Violations.Count);
    }
}