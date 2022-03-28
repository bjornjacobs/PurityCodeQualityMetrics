namespace PurityCodeQualityMetrics.Purity;

public interface IPurityReportRepo
{
    public PurityReport? GetByName(string name);
    public PurityReport? GetByFullName(string fullname);

    public List<PurityReport> GetAllReports(string start = "");

    public void AddRange(IEnumerable<PurityReport> reports);

    public void Clear();
}