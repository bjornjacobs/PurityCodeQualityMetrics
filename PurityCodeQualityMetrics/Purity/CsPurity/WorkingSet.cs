using System.Data;

namespace PurityCodeQualityMetrics.Purity.CsPurity;


/// <summary>
/// Calculates the working set. The working set is the set of all
/// methods in the lookup table that have empty dependency sets. A
/// method can only be in the working set once, so if a method with
/// empty dependency set has already been in the working set, it is not
/// re-added.
/// </summary>
public class WorkingSet : List<CSharpMethod>
{
    private readonly LookupTable _lookupTable;
    private readonly List<CSharpMethod> _history = new List<CSharpMethod>();

    public WorkingSet(LookupTable lookupTable)
    {
        this._lookupTable = lookupTable;
        Calculate();
    }
    
    public void Calculate()
    {
        Clear();

        foreach (var row in _lookupTable.Table.AsEnumerable())
        {
            CSharpMethod identifier = row.Field<CSharpMethod>("identifier");
            IEnumerable<CSharpMethod> dependencies = row.Field<IEnumerable<CSharpMethod>>("dependencies");
            if (!dependencies.Any() && !_history.Contains(identifier))
            {
                Add(identifier);
                _history.Add(identifier);
            }
        }
    }
}