using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.Tests.Purity.TestCode;

public class GloballyImpureTestClass
{
    private static int _global = 9;
    private const int _const = 9;
    private static TestEnum _testEnum = TestEnum.Test;


    [ViolationsTest(PurityViolation.ModifiesGlobalState, PurityViolation.ReadsGlobalState)]
    public int ModifyAndReturn()
    {
        _global = 16;
        return _global;
    }

    [ViolationsTest(PurityViolation.ModifiesGlobalState, PurityViolation.ReadsGlobalState)]
    public int ModifyAndReturnOtherClass()
    {
        GloballyImpureTestClass2.PublicState = 2;
        return GloballyImpureTestClass2.PublicState;
    }

    [ViolationsTest(PurityViolation.ReadsGlobalState)]
    public void AssignToNewVariable()
    {
        int value = _global;
    }

    [ViolationsTest(PurityViolation.ReadsGlobalState)]
    public void AssignToExistingVariable()
    {
        int value = 5;
        value = _global;
    }

    [ViolationsTest(PurityViolation.ReadsGlobalState, PurityViolation.ReadsGlobalState)]
    public void UseInExpression()
    {
        int value = _global + 5 * _global;
    }
    
    [ViolationsTest]
    public void TestConst()
    {
        var d = _const;
    }

    [ViolationsTest(PurityViolation.ReadsGlobalState)]
    public void TestEnumConstRead()
    {
        var v = _testEnum;
    }
    
    [ViolationsTest(PurityViolation.ModifiesGlobalState)]
    public void TestEnumConstWriteTo()
    {
        _testEnum = TestEnum.Functions;
    }
    
    [ViolationsTest]
    public void TestEnumConst()
    {
        var d = TestEnum.Functions;
    }
}

public class GloballyImpureTestClass2
{
    public static int PublicState = 1;
}

public enum TestEnum
{
    To,
    Test,
    The,
    Functions
}