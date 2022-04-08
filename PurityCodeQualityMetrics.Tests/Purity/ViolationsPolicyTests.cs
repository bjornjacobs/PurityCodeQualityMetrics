using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using Moq;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Tests.Purity;
using PurityCodeQualityMetrics.Tests.Purity.TestCode;
using Xunit;
using static PurityCodeQualityMetrics.Tests.Purity.Helper;
using FluentAssertions;

namespace PurityCodeQualityMetrics.Tests.Purity;

public class ViolationsPolicyTests
{
    private static readonly Mock<ILogger> _logger = new Mock<ILogger>();
    private readonly PurityAnalyser _sut = new(_logger.Object);

    public static IEnumerable<object[]> Data => Assembly.GetExecutingAssembly().GetTypes()
        .SelectMany(t => t.GetMethods())
        .Where(m => m.GetCustomAttributes(typeof(ViolationsTest), false).Length > 0)
        .Select(x => new object[] {x.Name, x.GetCustomAttribute<ViolationsTest>()!.Value});


    [Theory]
    [MemberData(nameof(Data))]
    public async Task TestPurityViolations(string name, PurityViolation[] violations)
    {
        var reports = await GetReports(_sut);

        var r = reports.First(x => x.Name.EndsWith(name));
        var should = violations.GroupBy(x => x).ToList();
        var actual = r.Violations.GroupBy(x => x).ToList();

        var errorString =
            $" method '{name}' should have [{string.Join(", ", violations)}] but has [{string.Join(",", r.Violations)}] ";


        actual.Should().HaveSameCount(should, errorString);

        foreach (var x in should)
        {
            var y = actual.FirstOrDefault(v => v.Key == x.Key);
            y.Should().NotBeNull(errorString);
            x.Should().HaveSameCount(y, errorString);
        }
    }
}