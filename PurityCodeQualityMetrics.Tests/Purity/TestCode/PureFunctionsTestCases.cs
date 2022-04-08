namespace PurityCodeQualityMetrics.Tests.Purity.TestCode;

public class PureFunctionsTestCases
{
    [ViolationsTest]
    public int PureFunction1()
    {
        return 1;
    }
    [ViolationsTest]
    public int PureFunction2()
    {
        int x = 5 * 5;
        var y = x + 8;
        x = y + x;
        return y;
    }
    [ViolationsTest]
    public int PureFunctionParameters(int x, int y)
    {
        return x + y;
    }
    [ViolationsTest]
    public int PureFunctionLambda()
    {
        return new List<int> {5, 5, 5}.Select(x => x * x).Aggregate((a, b) => a + b);
    }
}