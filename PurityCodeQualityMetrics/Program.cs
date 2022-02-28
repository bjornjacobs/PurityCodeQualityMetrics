using System.Diagnostics;
using CommandLine;
using PurityCodeQualityMetrics.Purity;

static void AnalyzeAndPrintEvaluate(IEnumerable<string> files, bool verbose)
{
    var analyzer = new PurityAnalyzer(files, verbose);
    LookupTable lt = analyzer.Analyze()
        .StripMethodsNotDeclaredInAnalyzedFiles()
        .StripInterfaceMethods();
    Console.WriteLine();
    Console.WriteLine($"Methods with [Pure] attribute:");
    Console.WriteLine();
    Console.WriteLine($"  Pure: {lt.CountMethodsWithPurity(PurityValue.Pure, true)}");
    Console.WriteLine($"  Impure");
    Console.WriteLine($"    - Throws exception: " +
                      lt.CountMethodsWithPurity(PurityValue.ThrowsException, true));
    Console.WriteLine($"    - Other: {lt.CountMethodsWithPurity(PurityValue.Impure, true)}");
    Console.WriteLine($"  Unknown: {lt.CountMethodsWithPurity(PurityValue.Unknown, true)}");
    Console.WriteLine($"  Total: {lt.CountMethods(true)}");
    Console.WriteLine();
    Console.WriteLine($"Methods without [Pure] attribute:");
    Console.WriteLine();
    Console.WriteLine($"  Pure: {lt.CountMethodsWithPurity(PurityValue.Pure, false)}");
    Console.WriteLine($"  Impure: " + lt.CountMethodsWithPurity(
        new PurityValue[] {PurityValue.Impure, PurityValue.ThrowsException}, false)
    );
    Console.WriteLine($"  Unknown: {lt.CountMethodsWithPurity(PurityValue.Unknown, false)}");
    Console.WriteLine($"  Total: {lt.CountMethods(false)}");
    Console.WriteLine();
    Console.WriteLine($"Total number of methods: {lt.CountMethods()}");
    Console.WriteLine(lt.GetFalsePositivesAndNegatives());
}

static void AnalyzeAndPrint(IEnumerable<string> files, bool pureAttributesOnly, bool evaluate, bool verbose)
{
    if (evaluate) AnalyzeAndPrintEvaluate(files, verbose);
    
    var analyzer = new PurityAnalyzer(files, verbose);
    LookupTable lt = analyzer.Analyze()
        .StripMethodsNotDeclaredInAnalyzedFiles()
        .StripInterfaceMethods();
    Console.WriteLine(lt.ToStringNoDependencySet(pureAttributesOnly));
    Console.WriteLine("Method purity ratios:");
    if (pureAttributesOnly)
    {
        Console.WriteLine(lt.GetPurityRatiosPureAttributesOnly());
    }
    else
    {
        Console.WriteLine(lt.GetPurityRatios());
    }
}

Parser.Default.ParseArguments<CommandLineOptions>(args)
    .WithParsed<CommandLineOptions>(o =>
    {
        var watch = Stopwatch.StartNew();
        
        if (string.IsNullOrEmpty(o.Directory))
        {
            Console.WriteLine("Please provide path(s) to the directory of C# file(s) to be analyzed.");
        }
        else if (!string.IsNullOrEmpty(o.InputString))
        {
            int flagIndex = Array.IndexOf(args, "--string") + 1;
            if (flagIndex < args.Length)
            {
                string file = args[flagIndex];
                AnalyzeAndPrint(new List<string> {file}, o.PureAttribute, o.Evaluate, o.Verbose);
            }
            else
            {
                Console.WriteLine("Missing program string to be parsed as an argument.");
            }
        }
        else if (o.InputFiles.Any())
        {
            try
            {
                int flagIndex = Array.IndexOf(args, "--files") + 1;
                IEnumerable<string> files = args.Skip(flagIndex).Select(
                    a => File.ReadAllText(a)
                );

                AnalyzeAndPrint(files, o.PureAttribute, o.Evaluate, o.Verbose);
            }
            catch (FileNotFoundException err)
            {
                Console.WriteLine(err.Message);
            }
            catch (Exception err)
            {
                Console.WriteLine($"Something went wrong when reading the file(s)" +
                                  $":\n\n{err.Message}");
            }
        }
        else
        {
            try
            {
                Console.WriteLine(o.Directory);
                IEnumerable<string> files = Directory.GetFiles(
                            o.Directory,
                            "*.cs",
                            SearchOption.AllDirectories
                        ).Select(File.ReadAllText);

                AnalyzeAndPrint(files, o.PureAttribute, o.Evaluate, o.Verbose);
            }
            catch (FileNotFoundException err)
            {
                Console.WriteLine(err.Message);
            }
            catch (Exception err)
            {
                Console.WriteLine($"Something went wrong when reading the file(s)" +
                                  $":\n\n{err}");
            }
        }

        watch.Stop();
        Console.WriteLine($"Elapsed: {watch.Elapsed}");
    });


class CommandLineOptions
{
    [Option(shortName:'d', longName:"directory", Required = false)]
    public string Directory { get; set; }
    
    [Option('r', "read", Required = false, HelpText = "Input files to be processed.")]
    public IEnumerable<string> InputFiles { get; set; }

    [Option('s', "string", Required = false, HelpText = "")]
    public string InputString { get; set; }

    [Option('e', "evaluate", Required = false, HelpText = "", Default = false)]
    public bool Evaluate { get; set; }

    [Option('p', "pure-attribute", Required = false, HelpText = "", Default = false)]
    public bool PureAttribute { get; set; }

    [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.", Default = false)]
    public bool Verbose { get; set; }
}