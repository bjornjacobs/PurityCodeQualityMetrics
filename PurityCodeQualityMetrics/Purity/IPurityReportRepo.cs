namespace PurityCodeQualityMetrics.Purity;

public interface IPurityReportRepo
{
    public PurityReport? GetByName(string name);
    public PurityReport? GetByFullName(string fullname);
    
    public IEnumerable<PurityReport> GetAllUnknownMethods();
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

    public IEnumerable<PurityReport> GetAllUnknownMethods()
    {
        throw new NotImplementedException();
    }
}