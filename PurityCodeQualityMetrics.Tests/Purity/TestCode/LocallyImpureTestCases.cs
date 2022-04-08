using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.Tests.Purity.TestCode;

public class LocallyImpureTestClass
{
    private int _member;
    private static int Global = 5;

    [ViolationsTest(PurityViolation.ReadsLocalState, PurityViolation.ModifiesLocalState)]
    public int LocallyImpure()
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
}