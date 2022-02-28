using System.Data;

namespace PurityCodeQualityMetrics.Purity;


/// <summary>
/// Calculates the working set. The working set is the set of all
/// methods in the lookup table that have empty dependency sets. A
/// method can only be in the working set once, so if a method with
/// empty dependency set has already been in the working set, it is not
/// re-added.
/// </summary>
public class WorkingSet : List<Method>
{
    private readonly LookupTable _lookupTable;
    private readonly List<Method> _history = new List<Method>();

    public WorkingSet(LookupTable lookupTable)
    {
        this._lookupTable = lookupTable;
        Calculate();
    }
    
    public void Calculate()
    {
        Clear();

        foreach (var row in _lookupTable.table.AsEnumerable())
        {
            Method identifier = row.Field<Method>("identifier");
            IEnumerable<Method> dependencies = row.Field<IEnumerable<Method>>("dependencies");
            if (!dependencies.Any() && !_history.Contains(identifier))
            {
                Add(identifier);
                _history.Add(identifier);
            }
        }
    }
}