namespace PurityCodeQualityMetrics.Purity;

public interface IPurityReportRepo
{
    public PurityReport? GetByName(string name);
    public PurityReport? GetByFullName(string fullname);

    public List<PurityReport> GetAllReports(string start = "");

    public void AddRange(IEnumerable<PurityReport> reports); 
    
    public IEnumerable<string> GetAllUnknownMethods();
}

public class InMemoryPurityRepo : IPurityReportRepo
{
    private IList<PurityReport> _source;

    public InMemoryPurityRepo(IList<PurityReport> source)
    {
        _source = source;
    }

    public PurityReport? GetByName(string name)
    {
        return _source.FirstOrDefault(x => x.Name == name);
    }

    public PurityReport? GetByFullName(string fullname)
    {
        return _source.FirstOrDefault(x => fullname == x.Namespace + "." + x.Name);
    }

    public List<PurityReport> GetAllReports(string start = "")
    {
        throw new NotImplementedException();
    }

    public void AddRange(IEnumerable<PurityReport> reports)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> GetAllUnknownMethods()
    {
        throw new NotImplementedException();
    }
}