namespace PurityCodeQualityMetrics.Purity.Storage;

public class InMemoryReportRepo : IPurityReportRepo
{
    private List<PurityReport> _reports = new List<PurityReport>();

    public PurityReport? GetByName(string name)
    {
        throw new NotImplementedException();
    }

    public PurityReport? GetByFullName(string fullname)
    {
        throw new NotImplementedException();
    }

    public List<PurityReport> GetAllReports(string start = "")
    {
        return _reports;
    }

    public void AddRange(IEnumerable<PurityReport> reports)
    {
        _reports.RemoveAll(x => reports.Any(y => x.FullName == y.FullName));
        _reports.AddRange(reports);
    }

    public void RemoveClassesInFiles(List<string> path)
    {
        var amount = _reports.RemoveAll(x => path.Any(y => x.FilePath.EndsWith(y, StringComparison.CurrentCultureIgnoreCase)));
    }

    public void Clear()
    {
        _reports.Clear();
    }
}