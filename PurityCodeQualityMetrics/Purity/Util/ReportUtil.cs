namespace PurityCodeQualityMetrics.Purity.Util;

public static class ReportUtil
{
    public static List<MethodDependency> GetAllUnkownMethods(this List<PurityReport> reports)
    {
        var dep = reports.SelectMany(x => x.Dependencies).DistinctBy(x => x.FullName);
        
        return dep
            .Where(d => reports.All(r => d.FullName != r.FullName))
            .DistinctBy(x => x.FullName).ToList();
    }
}