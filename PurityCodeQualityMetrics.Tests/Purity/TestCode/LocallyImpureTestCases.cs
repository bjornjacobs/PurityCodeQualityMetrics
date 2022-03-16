namespace PurityCodeQualityMetrics.Tests.Purity.TestCode;

public class LocallyImpureTestClass
{
    private int _member;
    private static int Global = 5;

    public int LocallyImpure()
    {
        _member = 1;
        return _member;
    }

    public int LocallYImpure2()
    {
        this._member = 5;
        return 1;
    }
    
    public void LocallYImpure3()
    {
        var x = this._member;
        x = _member;
    }

    public void NoLocalImpure()
    {
        var x = 5;
        var y = 3;
        x = x + y;
        y = Global;
    }
}