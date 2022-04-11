using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.Tests.Purity.TestCode;

public class LocallyImpureTestClass
{
    private int _member;
    private int _property;
    private static int Global = 5;

    [ViolationsTest(PurityViolation.ReadsLocalState, PurityViolation.ModifiesLocalState)]
    public int LocallyImpureTest()
    {
        _member = 1;
        return _member;
    }

    [ViolationsTest(PurityViolation.ModifiesLocalState)]
    public int LocallYImpure2()
    {
        this._member = 5;
        return 1;
    }
    
    [ViolationsTest(PurityViolation.ReadsLocalState, PurityViolation.ReadsLocalState)]
    public void LocallYImpure3()
    {
        var x = this._member;
        x = _member;
    }

    [ViolationsTest(PurityViolation.ReadsGlobalState)]
    public void NoLocalImpure()
    {
        var x = 5;
        var y = 3;
        x = x + y;
        y = Global;
    }
    
    [ViolationsTest(PurityViolation.ReadsLocalState)]
    public void OnlyGetOneViolation()
    {
        var x = _member;
        x = 5;
    }
    
    [ViolationsTest(PurityViolation.ReadsLocalState)]
    public void PropertyReadTest()
    {
        var d = _property;
    }
    
    [ViolationsTest(PurityViolation.ModifiesLocalState)]
    public void PropertyWriteTest()
    {
        _property = 5;
    }
}