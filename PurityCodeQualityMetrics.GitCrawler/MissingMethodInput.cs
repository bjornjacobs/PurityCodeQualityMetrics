using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.GitCrawler;

public class MissingMethodInput
{
    class State
    {
        public string CurrentFunctionName { get; set; }

        public bool ReturnsFresh { get; set; }
        public bool ReadsLocalState { get; set; }
        public bool ModifiesLocalState { get; set; }

        public bool ModifiesNonFreshVariable { get; set; }
        public bool ModifiesParameters { get; set; }
        
        public bool ReadsGlobalState { get; set; }
        public bool ModifiesGlobalState { get; set; }
        public State()
        {
            CurrentFunctionName = "";
            ReturnsFresh = true;
        }

        public List<PurityViolation> Violations()
        {
            var v = new List<PurityViolation>();
            if(ModifiesParameters) v.Add(PurityViolation.ModifiesParameter);
            if(ModifiesNonFreshVariable) v.Add(PurityViolation.ModifiesNonFreshObject);
            if(ReadsLocalState) v.Add(PurityViolation.ReadsLocalState);
            if(ModifiesLocalState) v.Add(PurityViolation.ModifiesLocalState);
            if(ReadsGlobalState) v.Add(PurityViolation.ReadsLocalState);
            if(ModifiesGlobalState) v.Add(PurityViolation.ModifiesGlobalState);
            return v;
        }
    }

    public static PurityReport? FromConsole(MethodDependency dependency)
    {
        if (!dependency.FullName.StartsWith("System", StringComparison.CurrentCultureIgnoreCase))
            return null;
        
        Console.Beep();
        var state = new State
        {
            CurrentFunctionName = dependency.FullName
        };
        Console.WriteLine(state.CurrentFunctionName);
        
        while (true)
        {
            Console.Write($"\rReturnsFresh: {Bs(state.ReturnsFresh)} | ModNonFresh {Bs(state.ModifiesNonFreshVariable)} | ModPar {Bs(state.ModifiesParameters)} |  ReadLocal {Bs(state.ReadsLocalState)} | WriteLocal {Bs(state.ModifiesLocalState)} | ReadGlobal {Bs(state.ReadsGlobalState)} | WriteGlobal {Bs(state.ModifiesGlobalState)}");

            var command = Console.ReadKey();

            switch (command.Key)
            {
                case ConsoleKey.D1:
                    state.ReturnsFresh = !state.ReturnsFresh;
                    break;
                case ConsoleKey.D2:
                    state.ModifiesNonFreshVariable = !state.ModifiesNonFreshVariable;
                    break;
                case ConsoleKey.D3:
                    state.ModifiesParameters = !state.ModifiesParameters;
                    break;
                case ConsoleKey.D4:
                    state.ReadsLocalState = !state.ReadsLocalState;
                    break;
                case ConsoleKey.D5:
                    state.ModifiesLocalState = !state.ModifiesLocalState;
                    break;
                case ConsoleKey.D6:
                    state.ReadsGlobalState = !state.ReadsGlobalState;
                    break;
                case ConsoleKey.D7:
                    state.ModifiesGlobalState = !state.ModifiesGlobalState;
                    break;
                
                case ConsoleKey.S:
                    return null;
                case ConsoleKey.Enter:
                    var d = new PurityReport(dependency.Name, dependency.Namespace, dependency.ReturnType,
                        dependency.ParameterTypes);
                    d.Violations = state.Violations();
                    d.IsMarkedByHand = true;
                    d.ReturnValueIsFresh = state.ReturnsFresh;
                    d.FilePath = "<<MarkedByHand>>";
                    ClearCurrentConsoleLine();
                    return d;
            }
        }
    }
    
    private static string Bs(bool val)
    {
        return val ? "[*]" : "[ ]";
    }
    
    public static void ClearCurrentConsoleLine()
    {
        int currentLineCursor = Console.CursorTop;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth)); 
        
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        Console.Write(new string(' ', Console.WindowWidth)); 
        Console.SetCursorPosition(0, currentLineCursor - 1);
        Console.Write(new string(' ', Console.WindowWidth)); 
        Console.SetCursorPosition(0, currentLineCursor - 1);
    }

}