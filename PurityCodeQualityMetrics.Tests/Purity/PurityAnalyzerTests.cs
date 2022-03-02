using PurityCodeQualityMetrics.Purity;
using Xunit;

namespace PurityCodeQualityMetrics.Tests.Purity;

public class PurityAnalyzerTests
{
  //  private PurityAnalyzer _sut = new();
    
    [Fact]
    public void AnalyzePurity_Pure()
    {
        var analyzer = new PurityAnalyzer();
    }
}