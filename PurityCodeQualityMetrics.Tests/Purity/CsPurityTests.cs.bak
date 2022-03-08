using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PurityCodeQualityMetrics.Purity.CsPurity;
using Xunit;

namespace PurityCodeQualityMetrics.Tests.Purity;

// ReSharper disable once ClassNeverInstantiated.Global
public class CsPurityTests
{
    public class UnitTest
    {
        [Fact]
        public void TestAnalyzeBasicPurity()
        {
            var file = (@"
                class C1
                {
                    int bar = 42;
                    int Foo()
                    {
                        return bar;
                    }

                    public int Bar() => bar;
                }
            ");
            LookupTable resultTable = new PurityAnalyzer(file).Analyze();
            var fooDeclaration = resultTable.GetMethodByName("Foo");
            var barDeclaration = resultTable.GetMethodByName("Bar");

            Assert.Equal(PurityValue.Pure, resultTable.GetPurity(fooDeclaration));
            Assert.Equal(PurityValue.Pure, resultTable.GetPurity(barDeclaration));
        }

        /// <summary>
        /// Empty input or input with no methods should have no PurityValue.
        /// </summary>
        [Fact]
        public void TestNoMethodsInInput()
        {
            var file1 = ("");
            var file2 = ("foo");
            var file3 = (@"
                namespace TestSpace
                {
                    class TestClass { }
                }
            ");

            LookupTable result1 = new PurityAnalyzer(file1).Analyze();
            LookupTable result2 = new PurityAnalyzer(file2).Analyze();
            LookupTable result3 = new PurityAnalyzer(file3).Analyze();

            Assert.False(result1.Table.AsEnumerable().Any());
            Assert.False(result2.Table.AsEnumerable().Any());
            Assert.False(result3.Table.AsEnumerable().Any());
        }

        /// <summary>
        /// PurityAnalyzer should handle local implicitly typed variables.
        ///
        /// Because `var` counts as an IdentifierNameSyntax this initially
        /// caused problems with the code.
        /// </summary>
        [Fact]
        public void HandleImmplicitlyTypedVariables()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        var bar = 42;
                        return bar;
                    }
                }
            ");
            new PurityAnalyzer(file).Analyze();
        }

        [Fact]
        public void TestReadsStaticField()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        bar();
                        C2.fooz();
                        return 42 + C2.StaticValue;
                    }

                    static int bar()
                    {
                        return 1;
                    }

                    void faz()
                    {
                        C2.fooz();
                    }

                    int foz()
                    {
                        return C3.StaticValue;
                    }
                }

                class C2
                {
                    public static int StaticValue = 1;
                    public static int fooz()
                    {
                        return 3;
                    }
                }

                static class C3
                {
                    public static int StaticValue = 3;
                }
            ");
            PurityAnalyzer PurityAnalyzer = new PurityAnalyzer(file);
            var fooDeclaration =
                HelpMethods.GetMethodDeclaration("foo", PurityAnalyzer.LookupTable.Trees.Single().GetRoot());
            var barDeclaration =
                HelpMethods.GetMethodDeclaration("bar", PurityAnalyzer.LookupTable.Trees.Single().GetRoot());
            var fazDeclaration =
                HelpMethods.GetMethodDeclaration("faz", PurityAnalyzer.LookupTable.Trees.Single().GetRoot());

            Assert.True(PurityAnalyzer.ReadsStaticFieldOrProperty(fooDeclaration));
            Assert.False(PurityAnalyzer.ReadsStaticFieldOrProperty(barDeclaration));
            Assert.False(PurityAnalyzer.ReadsStaticFieldOrProperty(fazDeclaration));
        }

        [Fact]
        public void TestReadsStaticProperty()
        {
            var file = (@"
                static class C1
                {
                    string foo() {
                        return C2.Name;
                    }
                }

                class C2
                {
                    public static string Name { get; set; } = ""foo"";
                }
            ");
            PurityAnalyzer PurityAnalyzer = new PurityAnalyzer(file);
            var fooDeclaration =
                HelpMethods.GetMethodDeclaration("foo", PurityAnalyzer.LookupTable.Trees.Single().GetRoot());

            Assert.True(PurityAnalyzer.ReadsStaticFieldOrProperty(fooDeclaration));
        }

        [Fact]
        // Calling a static method is not considered impure
        public void TestCallsStaticMethod()
        {
            var file = (@"
                class C1
                {
                    public string Foo() {
                        return C2.bar();
                    }
                }

                class C2
                {
                    public static string bar() { return ""bar""; }
                }
            ");
            PurityAnalyzer PurityAnalyzer = new PurityAnalyzer(file);
            var foo = HelpMethods.GetMethodDeclaration("Foo", PurityAnalyzer.LookupTable.Trees.Single().GetRoot());

            Assert.False(PurityAnalyzer.ReadsStaticFieldOrProperty(foo));
        }

        [Fact]
        // Calling a static method is not considerd impure
        public void TestEnumInAttribute()
        {
            var file = (@"
                using System;
                class TestClass {
                    [Foo(Color.Blue)]
                    public string Bar() {
                        return ""bar"";
                    }

                    public enum Color
                    {
                        Red, Green, Blue
                    }

                    public class FooAttribute : Attribute
                    {
                        private Color color;

                        public FooAttribute(Color color)
                        {
                            this.color = color;
                        }
                    }
                }
            ");
            PurityAnalyzer PurityAnalyzer = new PurityAnalyzer(file);
            var bar = HelpMethods.GetMethodDeclaration("Bar", PurityAnalyzer.LookupTable.Trees.Single().GetRoot());

            Assert.False(PurityAnalyzer.ReadsStaticFieldOrProperty(bar));
        }

        // Implicitly static property means a non-static property pointing to a
        // static field
        //[Fact] // Not implemented in PurityAnalyzer for now
        public void TestReadsImplicitlyStaticProperty()
        {
            var file = (@"
                class C1
                {
                    string foo() {
                        C2 c2 = new C2();
                        return c2.Name;
                    }
                }

                class C2
                {
                    static string _name = ""foo"";
                    public string Name
                    {
                        get => _name;
                        set => _name = value;
                    }
                }
            ");
            PurityAnalyzer PurityAnalyzer = new PurityAnalyzer(file);
            var fooDeclaration =
                HelpMethods.GetMethodDeclaration("foo", PurityAnalyzer.LookupTable.Trees.Single().GetRoot());

            Assert.True(PurityAnalyzer.ReadsStaticFieldOrProperty(fooDeclaration));
        }

        [Fact]
        public void TestThrowException()
        {
            var file = (@"
                class C1
                {
                    void foo() {
                        throw new Exception(
                            $""Foo exception""
                        );
                    }

                    int bar() {
                        return 42;
                    }
                }
            ");
            PurityAnalyzer PurityAnalyzer = new PurityAnalyzer(file);
            var fooDeclaration = PurityAnalyzer.LookupTable.GetMethodByName("foo");
            var barDeclaration = PurityAnalyzer.LookupTable.GetMethodByName("bar");
            Assert.True(PurityAnalyzer.ThrowsException(fooDeclaration));
            Assert.False(PurityAnalyzer.ThrowsException(barDeclaration));
        }

        [Fact]
        public void TestAnalyze()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        C2 c2 = new C2();
                        return c2.foz();
                    }

                    int bar()
                    {
                        C2.baz();
                        return 42;
                    }
                }

                class C2
                {
                    public static int value = 42;

                    public static void baz()
                    {
                        value = 3;
                        value++;
                    }

                    public int foz() {
                        return 1;
                    }

                    public unsafe int faz() {
                        return 1;
                    }
                }
            ");
            LookupTable resultTable = new PurityAnalyzer(file).Analyze();

            var fooDeclaration = resultTable.GetMethodByName("foo");
            var barDeclaration = resultTable.GetMethodByName("bar");
            var bazDeclaration = resultTable.GetMethodByName("baz");
            var fozDeclaration = resultTable.GetMethodByName("foz");
            var fazDeclaration = resultTable.GetMethodByName("faz");

            Assert.Equal(PurityValue.Pure, resultTable.GetPurity(fooDeclaration));
            Assert.Equal(PurityValue.Impure, resultTable.GetPurity(barDeclaration));
            Assert.Equal(PurityValue.Impure, resultTable.GetPurity(bazDeclaration));
            Assert.Equal(PurityValue.Pure, resultTable.GetPurity(fozDeclaration));
            Assert.Equal(PurityValue.Impure, resultTable.GetPurity(fazDeclaration));
        }

        [Fact]
        public void TestAnalyze2()
        {
            var file = (@"
                public class LinkedList
                {
                    private Node head;
                    private Node tail;

                    // Returns length of list
                    public static int Length(LinkedList list)
                    {
                        Node current = list.head;
                        int length = 0;

                        while (current != null)
                        {
                            length++;
                            current = current.next;
                        }
                        return length;
                    }

                    // Appends data to the list
                    public void Add(Object data)
                    {
                        if (LinkedList.Length(this) == 0)
                        {
                            head = new Node(data);
                            tail = head;
                        }
                        else
                        {
                            Node addedNode = new Node(data);
                            tail.next = addedNode;
                            tail = addedNode;
                        }
                    }

                    // Removes item at index from list.
                    // Assumes that list is non-empty and
                    // that index is non-negative and less
                    // than list's length
                    static public void Remove(int index, LinkedList list)
                    {
                        if (index == 0)
                        {
                            list.head = list.head.next;
                        }
                        else
                        {
                            Node pre = list.head;

                            for (int i = 0; i < index - 1; i++)
                            {
                                pre = pre.next;
                            }
                            pre.next = pre.next.next;
                        }
                    }

                    public static void PrintListLength(LinkedList list)
                    {
                        Console.WriteLine(Length(list));
                    }

                    public void PrintLength()
                    {
                        PrintListLength(this);
                    }

                    private class Node
                    {
                        public Node next;
                        public Object data;

                        public Node() { }

                        public Node(Object data)
                        {
                            this.data = data;
                        }
                    }
                }
            ");
            LookupTable resultTable = new PurityAnalyzer(file).Analyze();

            var lengthDeclaration = resultTable.GetMethodByName("Length");
            var addDeclaration = resultTable.GetMethodByName("Add");
            var removeDeclaration = resultTable.GetMethodByName("Remove");
            var printListLengthDeclaration = resultTable.GetMethodByName("PrintListLength");
            var printLengthDeclaration = resultTable.GetMethodByName("PrintLength");

            //TODO: Implement checks for for commented purities
            Assert.Equal(PurityValue.Pure, resultTable.GetPurity(lengthDeclaration));

            Assert.Equal(PurityValue.Impure, resultTable.GetPurity(addDeclaration));
            //Assert.Equal(PurityValue.LocallyImpure, resultTable.GetPurity(addDeclaration));

            Assert.Equal(PurityValue.Impure, resultTable.GetPurity(removeDeclaration));
            //Assert.Equal(PurityValue.ParametricallyImpure, resultTable.GetPurity(removeDeclaration));

            Assert.Equal(PurityValue.Impure, resultTable.GetPurity(printListLengthDeclaration));
            Assert.Equal(PurityValue.Impure, resultTable.GetPurity(printLengthDeclaration));
        }

        [Fact]
        public void TestAnalyzeUnknownPurity()
        {
            var file = (@"
                class C1
                {
                    public List<int> foo()
                    {
                        return C2.bar();
                    }

                    public int foz()
                    {
                        return 1;
                    }
                }

                class C2
                {
                    public static List<int> bar() {
                        return C2.baz();
                    }

                    public static List<int> baz() {
                        List<int> l = new List<int>();
                        l.Add(1);
                        var c = l.Contains(1);
                        return l;
                    }
                }
            ");
            LookupTable resultTable = new PurityAnalyzer(file).Analyze();

            var fooDeclaration = resultTable.GetMethodByName("foo");
            var fozDeclaration = resultTable.GetMethodByName("foz");
            var barDeclaration = resultTable.GetMethodByName("bar");
            var bazDeclaration = resultTable.GetMethodByName("baz");

            Assert.Equal(PurityValue.Unknown, resultTable.GetPurity(fooDeclaration));
            Assert.Equal(PurityValue.Pure, resultTable.GetPurity(fozDeclaration));
            Assert.Equal(PurityValue.Unknown, resultTable.GetPurity(barDeclaration));
            Assert.Equal(PurityValue.Unknown, resultTable.GetPurity(bazDeclaration));
        }

        [Fact]
        public void TestPurityIsKnownPrior()
        {
            Assert.True(PurityAnalyzer.PurityIsKnownPrior(new CSharpMethod("Console.WriteLine")));
            Assert.False(PurityAnalyzer.PurityIsKnownPrior(new CSharpMethod("foo")));
            Assert.False(PurityAnalyzer.PurityIsKnownPrior(new CSharpMethod("")));

            Assert.True(PurityAnalyzer.PurityIsKnownPrior("Console.WriteLine"));
            Assert.False(PurityAnalyzer.PurityIsKnownPrior("foo"));
            Assert.False(PurityAnalyzer.PurityIsKnownPrior(""));

            var file = (@"
                class C1
                {
                    int foo()
                    {
                        return 3;
                    }
                }
            ");
            var lt = new PurityAnalyzer(file).Analyze();
            CSharpMethod foo = lt.GetMethodByName("foo");

            Assert.False(PurityAnalyzer.PurityIsKnownPrior(foo));
        }

        [Fact]
        public void TestKnownPuritiesNoDuplicates()
        {
            Assert.True(
                PurityList.List.GroupBy(p => p)
                    .Where(g => g.Count() > 1)
                    .Count() == 0
            );
        }

        [Fact]
        public void TestAnalyzeMultipleFiles()
        {
            var file1 = (@"
                using System;
                using ConsoleApp1;

                namespace ConsoleApp2
                {
                    class Program
                    {
                        public static string Bar()
                        {
                            return ""bar"";
                        }

                        static void Main()
                        {
                            Bar();
                            Console.WriteLine(Class1.Foo(""foo""));
                        }
                    }
                }
            ");

            var file2 = (@"
                using System;
                using System.Collections.Generic;
                using System.Text;

                namespace ConsoleApp1
                {
                    class Class1
                    {
                        public static string Foo(string val)
                        {
                            return val;
                        }
                    }
                }
            ");

            LookupTable lt = new PurityAnalyzer(new List<string> {file1, file2})
                .Analyze()
                .StripMethodsNotDeclaredInAnalyzedFiles();

            var foo = lt.GetMethodByName("Foo");
            var bar = lt.GetMethodByName("Bar");
            var main = lt.GetMethodByName("Main");

            Assert.Equal(3, lt.Table.Rows.Count);
            Assert.True(lt.HasMethod(foo));
            Assert.True(lt.HasMethod(bar));
            Assert.True(lt.HasMethod(main));
        }

        [Fact]
        public void TestLocalFunction()
        {
            var file = (@"
                class Program
                {
                    static string Foo()
                    {
                        Foz();
                        return Bar();

                        string Bar()
                        {
                            return Baz();

                            string Baz()
                            {
                                string baz = ""baz"";
                                return baz;
                            }
                        }
                    }

                    static int Foz()
                    {
                        return 0;
                    }
                }
            ");
            LookupTable lt = new PurityAnalyzer(file).Analyze();

            var foo = lt.GetMethodByName("Foo");
            var foz = lt.GetMethodByName("Foz");

            // Foo() should not depend on Bar() or Baz() since they are a local
            // functions inside Foo(). The local functions are simply ignored
            // when calculating Foo()'s dependencies.
            //
            // Foo() should only depend on Foz()
            Assert.Equal(foz, lt.CalculateDependencies(foo).Single());
            Assert.True(lt.HasMethod(foo));
            Assert.True(lt.HasMethod(foz));
            Assert.Equal(2, lt.Table.Rows.Count);
        }

        [Fact]
        public void TestDelegateFunction()
        {
            var file = (@"
                class Program
                {
                    // delegate declaration
                    public delegate void PrintString(string s);

                    public static void WriteToScreen(string str) {
                        Console.WriteLine(""The String is: {0}"", str);
                    }

                    public static void sendString(PrintString ps) {
                        ps(""Hello World"");
                    }

                    static void Foo() {
                        PrintString ps1 = new PrintString(WriteToScreen);
                        sendString(ps1);
                    }

                    static void Bar() {
                        PrintString ps1 = new PrintString(WriteToScreen);
                        ps1.BeginInvoke(""foo"", null, null);
                        ps1.EndInvoke(null);
                    }
                }
            ");
            LookupTable lt = new PurityAnalyzer(file).Analyze();
            Assert.Equal(5, lt.Table.Rows.Count);
        }

        [Fact]
        public void TestOverloading()
        {
            var file = (@"
                class Program
                {
                    int Foo(int i) {
                        return i * i;
                    }

                    int Foo(int i1, int i2) {
                        Console.WriteLine(i1);
                        return i1 * i2;
                    }

                    int Bar() {
                        return Foo(2) + Foo(3, 4);
                    }

                    int Bar1() {
                        return Foo(2);
                    }

                    int Bar2() {
                        return Foo(3, 4);
                    }
                }
            ");
            LookupTable lt = new PurityAnalyzer(file).Analyze();
            var tree = lt.Trees.Single();
            var root = (CompilationUnitSyntax) tree.GetRoot();

            var foo1 = new CSharpMethod(
                root
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Identifier.Text == "Foo")
                    .First()
            );
            var foo2 = new CSharpMethod(
                root
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Identifier.Text == "Foo")
                    .Last()
            );

            var bar = lt.GetMethodByName("Bar");
            var bar1 = lt.GetMethodByName("Bar1");
            var bar2 = lt.GetMethodByName("Bar2");

            Assert.Equal(PurityValue.Impure, lt.GetPurity(bar));
            Assert.True(HelpMethods.HaveEqualElements(
                lt.CalculateDependencies(bar),
                new List<CSharpMethod> {foo1, foo2}
            ));

            Assert.Equal(PurityValue.Pure, lt.GetPurity(bar1));
            Assert.True(HelpMethods.HaveEqualElements(
                lt.CalculateDependencies(bar1),
                new List<CSharpMethod> {foo1}
            ));

            Assert.Equal(PurityValue.Impure, lt.GetPurity(bar2));
            Assert.True(HelpMethods.HaveEqualElements(
                lt.CalculateDependencies(bar2),
                new List<CSharpMethod> {foo2}
            ));
        }

        // For now, constructors are ignored by PurityAnalyzer.Analyze() and so the
        // constructor for class B is never analyzed
        [Fact]
        public void TestConstructorCall()
        {
            var file = (@"
                class A
                {
                    void Foo(int i) {
                        new B(i);
                    }
                }

                class B
                {
                    int val;
                    public B(int val) {
                        this.val = val;
                    }
                }
            ");
            LookupTable lt = new PurityAnalyzer(file).Analyze();
            var foo = lt.GetMethodByName("Foo");

            Assert.Equal(1, lt.Table.Rows.Count);
            Assert.True(lt.HasMethod(foo));
        }

        [Fact]
        public void TestAnalyzeInterface()
        {
            var file = (@"
                public interface IParseTree
                {
                    new IParseTree Foo(int i);
                }

                public class A : IParseTree
                {
                    public IParseTree Foo(int i) {
                        return null;
                    }
                }
            ");
            LookupTable lt = new PurityAnalyzer(file).Analyze();
            var foo = new CSharpMethod(lt
                .Trees
                .Single()
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.Text == "Foo")
                .Last()
            );

            Assert.Equal(1, lt.Table.Rows.Count);
            Assert.True(lt.HasMethod(foo));
        }

        [Fact]
        public void TestRecursion()
        {
            var file = (@"
                class A
                {
                    int Foo(int i) {
                        if (i == 0) return 0;
                        return 1 + Foo(i - 1);
                    }
                }
            ");
            LookupTable lt = new PurityAnalyzer(file).Analyze();
            var foo = lt.GetMethodByName("Foo");

            Assert.Equal(1, lt.Table.Rows.Count);
            Assert.True(lt.HasMethod(foo));
            Assert.Equal(PurityValue.Pure, lt.GetPurity(foo));
            Assert.True(!lt.CalculateDependencies(foo).Any());
        }

        [Fact]
        public void TestSecondHandRecursion()
        {
            var file = (@"
                class A
                {
                    int Foo(int i, int j) {
                        return Foo(i) + Foo(j);
                    }

                    int Foo(int i) {
                        if (i == 0) return 0;
                        return 1 + Foo(i - 1);
                    }
                }
            ");
            LookupTable lt = new PurityAnalyzer(file).Analyze();
            var foo1 = new CSharpMethod(lt
                .Trees
                .Single()
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.Text == "Foo")
                .First()
            );
            var foo2 = new CSharpMethod(lt
                .Trees
                .Single()
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.Text == "Foo")
                .Last()
            );

            Assert.Equal(2, lt.Table.Rows.Count);
            Assert.Equal(foo2, lt.CalculateDependencies(foo1).Single());
            Assert.True(!lt.CalculateDependencies(foo2).Any());
        }

        [Fact]
        public void TestMutualRecursion()
        {
            var file = (@"
                class A
                {
                    int Foo(int i) {
                        if (i == 0) return 0;
                        return Bar(i - 1)
                    }

                    int Bar(int i) {
                        if (i == 0) return 0;
                        return 1 + Foo(i - 1);
                    }
                }
            ");
            LookupTable lt = new PurityAnalyzer(file).Analyze();
            var foo = lt.GetMethodByName("Foo");
            var bar = lt.GetMethodByName("Bar");

            Assert.Equal(2, lt.Table.Rows.Count);
            Assert.Equal(bar, lt.CalculateDependencies(foo).Single());
            Assert.Equal(foo, lt.CalculateDependencies(bar).Single());
        }

        [Fact]
        public void TestHasPureAttribute()
        {
            var file = (@"
                using System.Diagnostics.Contracts;

                class Class2
                {
                    [Pure]
                    public string Foo()
                    {
                        return ""foo"";
                    }

                    [Foo]
                    public string Bar()
                    {
                        return ""bar"";
                    }

                    public string Baz()
                    {
                        return ""baz"";
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            var foo = HelpMethods.GetMethodDeclaration("Foo", root);
            var bar = HelpMethods.GetMethodDeclaration("Bar", root);
            var baz = HelpMethods.GetMethodDeclaration("Baz", root);

            Assert.True(foo.HasPureAttribute());
            Assert.False(bar.HasPureAttribute());
            Assert.False(baz.HasPureAttribute());
        }

        [Fact]
        public void TestHasBody()
        {
            var file = (@"
                class Foz
                {
                    public int Bar()
                    {
                        return 0;
                    }
                }

                public interface IParseTree
                {
                    IParseTree Foo(int i);
                }

                abstract class Shape
                {
                    public abstract int GetArea();
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            var bar = HelpMethods.GetMethodDeclaration("Bar", root);
            var foo = HelpMethods.GetMethodDeclaration("Foo", root);
            var getArea = HelpMethods.GetMethodDeclaration("GetArea", root);

            Assert.True(bar.HasBody());
            Assert.False(foo.HasBody());
            Assert.False(getArea.HasBody());
        }

        [Fact]
        public void TestEnumsAreImpure()
        {
            if (!PurityAnalyzer.EnumsAreImpure) return;

            var file = (@"
            namespace Test {
                public class TestClass {
                    public TypeCode GetTypeCode() {
                        return TypeCode.String;
                    }

                    public TypeCode Foo(TypeCode tc) {
                        return tc;
                    }

                    public enum TypeCode
                    {
                        String = 18
                    }
                }
            }
            ");

            LookupTable lt = new PurityAnalyzer(file).Analyze();
            var m = lt.GetMethodByName("GetTypeCode");
            var foo = lt.GetMethodByName("Foo");

            // Since enums are static, reading their value is considered impure
            Assert.NotEqual(PurityValue.Pure, lt.GetPurity(m));
            // But returning an enum or using it as a parameter is not impure
            Assert.Equal(PurityValue.Pure, lt.GetPurity(foo));
        }

        [Fact]
        public void TestEnumsAreNotImpure()
        {
            if (PurityAnalyzer.EnumsAreImpure) return;

            var file = (@"
            namespace Test {
                public class TestClass {
                    public bool Baz(TypeCode tc, int foo) {
                        return tc == TypeCode.String;
                    }

                    public TypeCode GetTypeCode() {
                        return TypeCode.String;
                    }

                    public TypeCode Foo(TypeCode tc) {
                        return tc;
                    }

                    public enum TypeCode {
                        String = 18
                    }
                }
            }
            ");

            LookupTable lt = new PurityAnalyzer(file).Analyze();
            var m = lt.GetMethodByName("GetTypeCode");
            var baz = lt.GetMethodByName("Baz");
            var foo = lt.GetMethodByName("Foo");

            Assert.Equal(PurityValue.Pure, lt.GetPurity(m));
            Assert.Equal(PurityValue.Pure, lt.GetPurity(baz));
            Assert.Equal(PurityValue.Pure, lt.GetPurity(foo));
        }

//         [Fact]
//         public void TestContainsUnknownIdentifier()
//         {
//             if (PurityAnalyzer.EnumsAreImpure) return;
//
//             var file = (@"
//                 class A {
//                     int val = 10;
//
//                     int Foo()
//                     {
//                         return val;
//                     }
//
//                     int Bar()
//                     {
//                         Console.WriteLine(""bar"");
//                     }
//
//                     [Foo]
//                     int Baz()
//                     {
//                         UnknownClass.UnknownMethod();
//                     }
//
//                     char[] GetBestFitUnicodeToBytesData()
//                     {
//                         return EmptyArray<Char>.Value;
//                     }
//                 }
//             ");
//
//             PurityAnalyzer PurityAnalyzer = new PurityAnalyzer(file);
//             
//             var foo = PurityAnalyzer.LookupTable.GetMethodByName("Foo");
//             var bar = PurityAnalyzer.LookupTable.GetMethodByName("Bar");
//             var baz = PurityAnalyzer.LookupTable.GetMethodByName("Baz");
//             var m = PurityAnalyzer.LookupTable.GetMethodByName("GetBestFitUnicodeToBytesData");
//
//             Assert.False(PurityAnalyzer.ContainsUnknownIdentifier(foo));
//             Assert.False(PurityAnalyzer.ContainsUnknownIdentifier(bar));
//             Assert.True(PurityAnalyzer.ContainsUnknownIdentifier(baz));
//             Assert.True(PurityAnalyzer.ContainsUnknownIdentifier(m));
//         }

        [Fact]
        public void TestPureCalleImpureCaller()
        {
            var file = (@"
                class A {

                    int Foo()
                    {
                        UnknownMethod();
                        return Bar();
                    }

                    int Bar()
                    {
                        return 42;
                    }
                }
            ");

            LookupTable lookupTable = new PurityAnalyzer(file).Analyze();
            var foo = lookupTable.GetMethodByName("Foo");
            var bar = lookupTable.GetMethodByName("Bar");

            Assert.Equal(PurityValue.Unknown, lookupTable.GetPurity(foo));
            Assert.Equal(PurityValue.Pure, lookupTable.GetPurity(bar));
        }
    }

    public class LookupTableTest
    {
        [Fact]
        public void TestCalculateDependencies()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        return bar() + C2.baz();
                    }

                    int bar()
                    {
                        return 42 + C2.baz();
                    }
                }

                class C2
                {
                    public static int baz()
                    {
                        return 42;
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            var lt = new LookupTable(tree);

            var foo = HelpMethods.GetMethodDeclaration("foo", root);
            var bar = HelpMethods.GetMethodDeclaration("bar", root);
            var baz = HelpMethods.GetMethodDeclaration("baz", root);
            var fooDependencies = lt.CalculateDependencies(foo);
            var barDependencies = lt.CalculateDependencies(bar);
            var bazDependencies = lt.CalculateDependencies(baz);
            var expectedFooDependencies = new List<CSharpMethod> {bar, baz};
            var expectedBarDependencies = new List<CSharpMethod> {baz};
            var expectedBazDependencies = new List<CSharpMethod>();

            var foo2 = HelpMethods.GetMethodDeclaration("foo", root);
            var eq = foo2.Equals(foo);
            var eq2 = foo.Equals(foo2);

            Assert.True(eq);
            Assert.True(eq2);

            Assert.True(
                HelpMethods.HaveEqualElements(fooDependencies, expectedFooDependencies)
            );

            Assert.True(
                HelpMethods.HaveEqualElements(barDependencies, expectedBarDependencies)
            );

            Assert.True(
                HelpMethods.HaveEqualElements(bazDependencies, expectedBazDependencies)
            );
        }

        [Fact]
        public void TestGettingMultipleDependencies()
        {
            var file = (@"
                class C1
                {
                    string foo()
                    {
                        baz();
                        return bar();
                    }

                    string bar()
                    {
                        return ""bar"";
                    }

                    void baz()
                    {
                        return ""baz"";
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();

            var fooDeclaration = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var lt = new LookupTable(tree);
            var fooDependencies = lt.CalculateDependencies(new CSharpMethod(fooDeclaration));
            var expectedResults = root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.ToString() != "foo")
                .Select(m => new CSharpMethod(m));

            Assert.True(
                HelpMethods.HaveEqualElements(
                    fooDependencies,
                    expectedResults
                )
            );
        }

        [Fact]
        public void TestGettingNestedDependencies()
        {
            var file = (@"
                class C1
                {
                    string foo()
                    {
                        return bar();
                    }

                    string bar()
                    {
                        return baz();
                    }

                    void baz()
                    {
                        return ""baz"";
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var lt = new LookupTable(tree);

            var foo = lt.GetMethodByName("foo");
            var bar = lt.GetMethodByName("bar");
            var baz = lt.GetMethodByName("baz");

            var fooDependencies = lt.CalculateDependencies(foo);
            var expectedFooDependencies = new List<CSharpMethod> {bar};

            var barDependencies = lt.CalculateDependencies(bar);
            var expectedBarDependencies = new List<CSharpMethod> {baz};

            var bazDependencies = lt.CalculateDependencies(baz);
            var expectedBazDependencies = new List<CSharpMethod> { };

            Assert.True(
                HelpMethods.HaveEqualElements(
                    fooDependencies,
                    expectedFooDependencies
                )
            );
            Assert.True(
                HelpMethods.HaveEqualElements(
                    barDependencies,
                    expectedBarDependencies
                )
            );
            Assert.True(
                HelpMethods.HaveEqualElements(
                    bazDependencies,
                    expectedBazDependencies
                )
            );
        }

        [Fact]
        public void TestGettingMultipleNestedDependencies()
        {
            var file = (@"
                class C1
                {
                    string foo()
                    {
                        return bar() + baz();
                    }

                    string bar()
                    {
                        return far() + faz();
                    }

                    void baz()
                    {
                        return ""baz"";
                    }

                    void far()
                    {
                        return ""far"";
                    }

                    void faz()
                    {
                        return ""faz"";
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var lt = new LookupTable(tree);

            var foo = lt.GetMethodByName("foo");
            var bar = lt.GetMethodByName("bar");
            var baz = lt.GetMethodByName("baz");
            var far = lt.GetMethodByName("far");
            var faz = lt.GetMethodByName("faz");

            var fooDependencies = lt.CalculateDependencies(foo);
            var barDependencies = lt.CalculateDependencies(bar);
            var bazDependencies = lt.CalculateDependencies(baz);
            var farDependencies = lt.CalculateDependencies(far);
            var fazDependencies = lt.CalculateDependencies(faz);

            var expectedFooDependencies = new List<CSharpMethod> {bar, baz};
            var expectedBarDependencies = new List<CSharpMethod> {far, faz};
            var expectedBazDependencies = new List<CSharpMethod> { };
            var expectedFarDependencies = new List<CSharpMethod> { };
            var expectedFazDependencies = new List<CSharpMethod> { };

            Assert.True(
                HelpMethods.HaveEqualElements(
                    fooDependencies,
                    expectedFooDependencies
                )
            );
            Assert.True(
                HelpMethods.HaveEqualElements(
                    barDependencies,
                    expectedBarDependencies
                )
            );
            Assert.True(
                HelpMethods.HaveEqualElements(
                    bazDependencies,
                    expectedBazDependencies
                )
            );
            Assert.True(
                HelpMethods.HaveEqualElements(
                    farDependencies,
                    expectedFarDependencies
                )
            );
            Assert.True(
                HelpMethods.HaveEqualElements(
                    fazDependencies,
                    expectedFazDependencies
                )
            );
        }

        [Fact]
        public void TestGettingMethodDependency()
        {
            var file = (@"
                class C1
                {
                    string foo()
                    {
                        C2 c2 = new C2();
                        return c2.bar();
                    }
                }

                class C2
                {
                    public string bar()
                    {
                        return ""bar"";
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            var model = PurityAnalyzer.GetSemanticModel(tree);

            var fooDeclaration = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var lt = new LookupTable(tree);
            var fooDependencies = lt.CalculateDependencies(new CSharpMethod(fooDeclaration));
            var expectedResults = root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.ToString() != "foo")
                .Select(m => new CSharpMethod(m));

            Assert.True(
                HelpMethods.HaveEqualElements(
                    fooDependencies,
                    expectedResults
                )
            );
        }

        [Fact]
        public void TestGettingDependenciesWithSameNames()
        {
            var file = (@"
                class C1
                {
                    string foo()
                    {
                        return C2.bar() + bar();
                    }

                    string bar()
                    {
                        return ""bar"";
                    }
                }

                class C2
                {
                    public static string bar()
                    {
                        return ""bar"";
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            var model = PurityAnalyzer.GetSemanticModel(tree);

            var fooDeclaration = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var lt = new LookupTable(tree);
            var fooDependencies = lt.CalculateDependencies(new CSharpMethod(fooDeclaration));
            var expectedResults = root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.ToString() != "foo")
                .Select(m => new CSharpMethod(m));

            Assert.True(
                HelpMethods.HaveEqualElements(
                    fooDependencies,
                    expectedResults
                )
            );
        }

        [Fact]
        public void TestGettingMultipleIdenticalDependencies()
        {
            var file = (@"
                class C1
                {
                    string foo()
                    {
                        return bar() + baz();
                    }

                    string bar()
                    {
                        return ""bar"" + baz() + baz();
                    }

                    void baz()
                    {
                        return ""baz"";
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            var model = PurityAnalyzer.GetSemanticModel(tree);

            var fooDeclaration = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var lt = new LookupTable(tree);
            var fooDependencies = lt.CalculateDependencies(new CSharpMethod(fooDeclaration));
            var expectedResults = root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.ToString() != "foo")
                .Select(m => new CSharpMethod(m));

            Assert.True(
                HelpMethods.HaveEqualElements(
                    fooDependencies,
                    expectedResults
                )
            );
        }

        [Fact]
        public void TestGettingBuiltInMethod()
        {
            var file = (@"
                class C1
                {
                    void foo()
                    {
                        Console.WriteLine(""foo"");
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();

            var fooDeclaration = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var lt = new LookupTable(tree);
            var fooDependencies = lt.CalculateDependencies(new CSharpMethod(fooDeclaration));
            var cwlInvocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            SemanticModel model = PurityAnalyzer.GetSemanticModel(new List<SyntaxTree> {tree}, tree);
            var expectedResultList = new List<CSharpMethod> {new CSharpMethod(cwlInvocation, model)};

            Assert.True(
                HelpMethods.HaveEqualElements(
                    fooDependencies,
                    expectedResultList
                )
            );
        }

        [Fact]
        public void TestBuildLookupTable1()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        return bar();
                    }

                    int bar()
                    {
                        return 42;
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();

            LookupTable lookupTable1 = new LookupTable(tree);

            LookupTable lookupTable2 = new LookupTable();
            lookupTable2.AddMethod(HelpMethods.GetMethodDeclaration("foo", root));
            lookupTable2.AddMethod(HelpMethods.GetMethodDeclaration("bar", root));
            lookupTable2.AddDependency(
                HelpMethods.GetMethodDeclaration("foo", root),
                HelpMethods.GetMethodDeclaration("bar", root)
            );

            Assert.True(HelpMethods.TablesAreEqual(lookupTable2.Table, lookupTable1.Table));
        }

        [Fact]
        public void TestBuildLookupTable2()
        {
            var file = (@"
                class C2
                {
                    int foo()
                    {
                        C2 c2 = new C2();
                        return c2.bar();
                    }
                }

                class C2
                {
                    int bar()
                    {
                        return 1;
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();

            LookupTable lookupTable1 = new LookupTable(tree);

            LookupTable lookupTable2 = new LookupTable();
            lookupTable2.AddMethod(HelpMethods.GetMethodDeclaration("foo", root));
            lookupTable2.AddMethod(HelpMethods.GetMethodDeclaration("bar", root));
            lookupTable2.AddDependency(
                HelpMethods.GetMethodDeclaration("foo", root),
                HelpMethods.GetMethodDeclaration("bar", root)
            );

            Assert.True(HelpMethods.TablesAreEqual(lookupTable2.Table, lookupTable1.Table));
        }

        [Fact]
        public void TestHasMethod()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        return 42;
                    }
                }
            ");
            LookupTable lookupTable = new LookupTable();
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            var methodDeclaration = HelpMethods.GetMethodDeclaration("foo", root);

            lookupTable.AddMethod(methodDeclaration);

            Assert.True(lookupTable.HasMethod(methodDeclaration));
        }

        [Fact]
        public void TestRemoveMethod()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        return 42;
                    }

                    int bar()
                    {
                        return 1;
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();

            LookupTable lookupTable = new LookupTable(tree);

            var foo = HelpMethods.GetMethodDeclaration("foo", root);
            var bar = HelpMethods.GetMethodDeclaration("bar", root);

            Assert.True(lookupTable.HasMethod(foo));
            Assert.True(lookupTable.HasMethod(bar));

            lookupTable.RemoveMethod(foo);

            Assert.False(lookupTable.HasMethod(foo));
            Assert.True(lookupTable.HasMethod(bar));

            lookupTable.RemoveMethod(bar);

            Assert.False(lookupTable.HasMethod(foo));
            Assert.False(lookupTable.HasMethod(bar));
        }

        [Fact]
        public void TestStripMethodsNotDeclaredInAnalyzedFiles()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        Console.WriteLine();
                        return 42;
                    }

                    int bar()
                    {
                        return 1;
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            LookupTable lookupTable = new LookupTable(tree);

            var foo = HelpMethods.GetMethodDeclaration("foo", root);
            var bar = HelpMethods.GetMethodDeclaration("bar", root);
            var cwl = new CSharpMethod("Console.WriteLine");

            Assert.True(lookupTable.HasMethod(foo));
            Assert.True(lookupTable.HasMethod(bar));
            Assert.True(lookupTable.HasMethod(cwl));

            lookupTable = lookupTable.StripMethodsNotDeclaredInAnalyzedFiles();

            Assert.True(lookupTable.HasMethod(foo));
            Assert.True(lookupTable.HasMethod(bar));
            Assert.False(lookupTable.HasMethod(cwl));
        }

        [Fact]
        public void TestStripInterfaceMethods()
        {
            var file = (@"
                class C1 : I1
                {
                    public int Foo()
                    {
                        return 42;
                    }
                }

                public interface I1
                {
                    int Foo();
                }
            ");
            LookupTable lt = new PurityAnalyzer(file).Analyze();

            var foo = new CSharpMethod(lt
                .Trees
                .Single()
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First(m => m.Identifier.Text == "Foo")
            );

            lt = lt.StripInterfaceMethods();
            Assert.Equal(1, lt.Table.Rows.Count);
            Assert.True(lt.HasMethod(foo));
        }

        [Fact]
        public void TestAddDependency()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        return bar();
                    }

                    int bar()
                    {
                        return 42;
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            var fooDeclaration = HelpMethods.GetMethodDeclaration("foo", root);
            var barDeclaration = HelpMethods.GetMethodDeclaration("bar", root);

            LookupTable lookupTable = new LookupTable();
            lookupTable.AddMethod(fooDeclaration);
            lookupTable.AddDependency(fooDeclaration, barDeclaration);

            Assert.True(lookupTable.HasDependency(fooDeclaration, barDeclaration));
        }

        [Fact]
        public void TestRemoveDependency()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        return bar() + C2.baz();
                    }

                    int bar()
                    {
                        return 42 + C2.baz();
                    }
                }

                class C2
                {
                    public static int baz()
                    {
                        return 42;
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            var fooDeclaration = HelpMethods.GetMethodDeclaration("foo", root);
            var barDeclaration = HelpMethods.GetMethodDeclaration("bar", root);
            var bazDeclaration = HelpMethods.GetMethodDeclaration("baz", root);

            LookupTable lookupTable = new LookupTable();
            lookupTable.AddMethod(fooDeclaration);
            lookupTable.AddDependency(fooDeclaration, barDeclaration);
            lookupTable.AddDependency(fooDeclaration, bazDeclaration);
            lookupTable.AddDependency(barDeclaration, bazDeclaration);
            Assert.True(lookupTable.HasDependency(fooDeclaration, barDeclaration));
            Assert.True(lookupTable.HasDependency(fooDeclaration, bazDeclaration));
            Assert.True(lookupTable.HasDependency(barDeclaration, bazDeclaration));

            lookupTable.RemoveDependency(fooDeclaration, barDeclaration);
            Assert.False(lookupTable.HasDependency(fooDeclaration, barDeclaration));
            Assert.True(lookupTable.HasDependency(fooDeclaration, bazDeclaration));
            Assert.True(lookupTable.HasDependency(barDeclaration, bazDeclaration));

            lookupTable.RemoveDependency(fooDeclaration, bazDeclaration);
            Assert.False(lookupTable.HasDependency(fooDeclaration, barDeclaration));
            Assert.False(lookupTable.HasDependency(fooDeclaration, bazDeclaration));
            Assert.True(lookupTable.HasDependency(barDeclaration, bazDeclaration));

            lookupTable.RemoveDependency(barDeclaration, bazDeclaration);
            Assert.False(lookupTable.HasDependency(fooDeclaration, barDeclaration));
            Assert.False(lookupTable.HasDependency(fooDeclaration, bazDeclaration));
            Assert.False(lookupTable.HasDependency(barDeclaration, bazDeclaration));

            LookupTable lookupTable2 = new LookupTable(tree);

            Assert.True(lookupTable2.HasDependency(fooDeclaration, barDeclaration));
            Assert.True(lookupTable2.HasDependency(fooDeclaration, bazDeclaration));
            Assert.True(lookupTable2.HasDependency(barDeclaration, bazDeclaration));
        }

        [Fact]
        public void TestCalculateWorkingSet()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        return bar() + C2.baz();
                    }

                    int bar()
                    {
                        return 42 + C2.baz();
                    }
                }

                class C2
                {
                    public static int baz()
                    {
                        return 42;
                    }

                    int foz() {
                        return 1;
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            var bazDeclaration = HelpMethods.GetMethodDeclaration("baz", root);
            var fozDeclaration = HelpMethods.GetMethodDeclaration("foz", root);

            LookupTable lookupTable = new LookupTable(tree);

            var expectedResult = new List<CSharpMethod>() {bazDeclaration, fozDeclaration};

            Assert.True(
                HelpMethods.HaveEqualElements(
                    expectedResult,
                    lookupTable.WorkingSet
                )
            );

            lookupTable.WorkingSet.Calculate();

            Assert.True(
                HelpMethods.HaveEqualElements(
                    new List<MethodDeclarationSyntax>(),
                    lookupTable.WorkingSet
                )
            );
        }

        [Fact]
        public void TestGetSetPurity()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        return bar() + C2.baz();
                    }

                    int bar()
                    {
                        return 42 + C2.baz();
                    }
                }

                class C2
                {
                    public static int baz()
                    {
                        return 42;
                    }

                    int foz() {
                        return 1;
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            var fooDeclaration = HelpMethods.GetMethodDeclaration("foo", root);
            var barDeclaration = HelpMethods.GetMethodDeclaration("bar", root);
            var bazDeclaration = HelpMethods.GetMethodDeclaration("baz", root);
            var fozDeclaration = HelpMethods.GetMethodDeclaration("foz", root);

            LookupTable lookupTable = new LookupTable(tree);

            Assert.Equal(PurityValue.Pure, lookupTable.GetPurity(fooDeclaration));
            Assert.Equal(PurityValue.Pure, lookupTable.GetPurity(barDeclaration));
            Assert.Equal(PurityValue.Pure, lookupTable.GetPurity(bazDeclaration));
            Assert.Equal(PurityValue.Pure, lookupTable.GetPurity(fozDeclaration));

            lookupTable.SetPurity(fooDeclaration, PurityValue.Impure);
            lookupTable.SetPurity(barDeclaration, PurityValue.Pure);
            lookupTable.SetPurity(bazDeclaration, PurityValue.ParametricallyImpure);

            Assert.Equal(PurityValue.Impure, lookupTable.GetPurity(fooDeclaration));
            Assert.Equal(PurityValue.Pure, lookupTable.GetPurity(barDeclaration));
            Assert.Equal(PurityValue.ParametricallyImpure, lookupTable.GetPurity(bazDeclaration));
            Assert.Equal(PurityValue.Pure, lookupTable.GetPurity(fozDeclaration));

            lookupTable.SetPurity(fooDeclaration, PurityValue.Impure);
            lookupTable.SetPurity(barDeclaration, PurityValue.Pure);
            lookupTable.SetPurity(bazDeclaration, PurityValue.ParametricallyImpure);

            Assert.Equal(PurityValue.Impure, lookupTable.GetPurity(fooDeclaration));
            Assert.Equal(PurityValue.Pure, lookupTable.GetPurity(barDeclaration));
            Assert.Equal(PurityValue.ParametricallyImpure, lookupTable.GetPurity(bazDeclaration));
            Assert.Equal(PurityValue.Pure, lookupTable.GetPurity(fozDeclaration));
        }

        [Fact]
        public void TestGetAllImpureMethods()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        return bar() + C2.baz();
                    }

                    int bar()
                    {
                        return 42 + C2.baz();
                    }
                }

                class C2
                {
                    public static int baz()
                    {
                        return 42;
                    }

                    int foz() {
                        return 1;
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            var fooDeclaration = HelpMethods.GetMethodDeclaration("foo", root);
            var barDeclaration = HelpMethods.GetMethodDeclaration("bar", root);
            var bazDeclaration = HelpMethods.GetMethodDeclaration("baz", root);
            var fozDeclaration = HelpMethods.GetMethodDeclaration("foz", root);

            LookupTable lookupTable = new LookupTable(tree);

            lookupTable.SetPurity(fooDeclaration, PurityValue.Impure);
            lookupTable.SetPurity(barDeclaration, PurityValue.Impure);
            var workingSet = new List<CSharpMethod>
            {
                fooDeclaration,
                barDeclaration,
                bazDeclaration,
                fozDeclaration
            };
            var expected = new List<CSharpMethod>
            {
                fooDeclaration,
                barDeclaration
            };
            Assert.True(
                HelpMethods.HaveEqualElements(
                    expected, lookupTable.GetAllImpureMethods(workingSet)
                )
            );
        }

        [Fact]
        public void TestGetCallers()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        return bar() + C2.baz();
                    }

                    int bar()
                    {
                        return 42 + C2.baz();
                    }
                }

                class C2
                {
                    public static int baz()
                    {
                        return 42;
                    }

                    int foz() {
                        return 1;
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();
            var fooDeclaration = HelpMethods.GetMethodDeclaration("foo", root);
            var barDeclaration = HelpMethods.GetMethodDeclaration("bar", root);
            var bazDeclaration = HelpMethods.GetMethodDeclaration("baz", root);
            var fozDeclaration = HelpMethods.GetMethodDeclaration("foz", root);

            LookupTable lookupTable = new LookupTable(tree);

            var result = lookupTable.GetCallers(bazDeclaration);
            var expected = new List<CSharpMethod> {fooDeclaration, barDeclaration};
            Assert.True(HelpMethods.HaveEqualElements(result, expected));
            Assert.Equal(0, lookupTable.GetCallers(fozDeclaration).Count());

            result = lookupTable.GetCallers(barDeclaration);
            expected = new List<CSharpMethod> {fooDeclaration};
            Assert.True(HelpMethods.HaveEqualElements(result, expected));
        }

        [Fact]
        public void TestCountMethods()
        {
            var file1 = (@"
                class C1
                {
                    int foo()
                    {
                        return bar() + C2.baz();
                    }

                    int bar()
                    {
                        return 42 + C2.baz();
                    }
                }

                class C2
                {
                    public static int baz()
                    {
                        return 42;
                    }

                    int foz() {
                        return 1;
                    }
                }
            ");

            var file2 = "";

            LookupTable lt1 = new PurityAnalyzer(file1).Analyze();
            LookupTable lt2 = new PurityAnalyzer(file2).Analyze();

            Assert.Equal(4, lt1.CountMethods());
            Assert.Equal(0, lt2.CountMethods());
        }

        [Fact]
        public void TestCountMethodsWithPurity()
        {
            var file = (@"
                class C1
                {
                    int foo()
                    {
                        C2 c2 = new C2();
                        return c2.foz();
                    }

                    int bar()
                    {
                        C2.baz();
                        return 42;
                    }
                }

                class C2
                {
                    public static int value = 42;

                    public static void baz()
                    {
                        value = 3;
                        value++;
                    }

                    public int foz() {
                        return 1;
                    }
                }

                class C2
                {
                    public static void faz()
                    {
                        UnknownFunction(""faz"");
                    }
                }
            ");
            LookupTable lt = new PurityAnalyzer(file).Analyze();

            Assert.Equal(2, lt.CountMethodsWithPurity(PurityValue.Pure));
            Assert.Equal(2, lt.CountMethodsWithPurity(PurityValue.Impure));
            Assert.Equal(2, lt.CountMethodsWithPurity(PurityValue.Unknown));
        }

        [Fact]
        public void TestCountMethodsWithPureAttribute()
        {
            var file = (@"
                using System.Diagnostics.Contracts;

                class Class2
                {
                    [Pure]
                    public string Foo()
                    {
                        return ""foo"";
                    }

                    [Foo]
                    public string Bar()
                    {
                        return ""bar"";
                    }

                    public string Baz()
                    {
                        return ""baz"";
                    }
                }
            ");
            LookupTable lt = new PurityAnalyzer(file).Analyze();

            Assert.Equal(1, lt.CountMethods(true));
            Assert.Equal(2, lt.CountMethods(false));
        }

        [Fact]
        public void TestCountMethodsWithPurityHasPurity()
        {
            var file = (@"
                using System.Diagnostics.Contracts;

                class Class2
                {
                    static int global = 0;

                    [Pure]
                    public string PureWithPureAttribute()
                    {
                        return ""foo"";
                    }

                    [Foo]
                    public string PureWithNoPureAttribute()
                    {
                        return ""bar"";
                    }

                    public string PureWithNoAttribute()
                    {
                        return ""baz"";
                    }

                    public void ImpureWithNoAttribute()
                    {
                        global ++;
                    }

                    [Pure]
                    public void ImpureWithPureAttribute()
                    {
                        global += 10;
                    }

                    [Foo]
                    public void ImpureWithNoPureAttribute()
                    {
                        global += 12;
                    }
                }
            ");
            LookupTable lt = new PurityAnalyzer(file).Analyze();

            Assert.Equal(1, lt.CountMethodsWithPurity(PurityValue.Pure, true));
            Assert.Equal(2, lt.CountMethodsWithPurity(PurityValue.Pure, false));
            Assert.Equal(1, lt.CountMethodsWithPurity(PurityValue.Impure, true));
            Assert.Equal(2, lt.CountMethodsWithPurity(PurityValue.Impure, false));
        }

        [Fact]
        public void TestCountFalsePositivesAndNegatives()
        {
            var file = (@"
                using System.Diagnostics.Contracts;

                class Class2
                {
                    static int global = 0;

                    [Pure]
                    public string PureWithPureAttribute()
                    {
                        return ""foo"";
                    }

                    [Foo]
                    public string PureWithNoPureAttribute()
                    {
                        return ""bar"";
                    }

                    public string PureWithNoAttribute()
                    {
                        return ""baz"";
                    }

                    public void ImpureWithNoAttribute()
                    {
                        global ++;
                    }

                    [Pure]
                    public void ImpureWithPureAttribute()
                    {
                        global += 10;
                    }

                    [Foo]
                    public void ImpureWithNoPureAttribute()
                    {
                        global += 12;
                    }
                }
            ");
            LookupTable lt = new PurityAnalyzer(file).Analyze();

            Assert.Equal(2, lt.CountFalsePositives());
            Assert.Equal(1, lt.CountFalseNegatives());
        }

        [Fact]
        public void TestGetBaseIdentifier()
        {
            var file = (@"
                class Class1
                {
                    int val = 0;
					Class2 c2 = new Class2();
                    int[] arr = new int[3];

                    public class Class2
                    {
                        public Class3 c3 = new Class3();
                        public int val2 = 10;
                        public int[] arr2 = new int[2];

                        public class Class3
                        {
                            public int val3 = 3;
                        }
                    }

                    public void Foo()
                    {
                        Class1 c1 = new Class1();
                        c1.c2.val2 = 1;
                        c2.val2++;
                        c1.c2.c3.val3 = 33;
                        ((c2).c3).val3 = 34;
                        val--;
                        arr[0] = 1;
                        c2.arr2[0] = 2;
                        this.c1.c2.c3.val3 = 35;

                        // Found in nodatime
                        Unsafe.AsRef(this) = new Interval(newStart, newEnd);

                        Foo.Bar.Unsafe.AsRef(this) = new Interval(newStart, newEnd);
                    }

                    public void Bar()
                    {
                        int a = 1, b = 2;
                        int c = 2, d = 3;
                        (a, b) = (c, d);
                    }

                    public void Baz()
                    {
                        int e = 1, f = 2;
                        int g = 2, h = 3;
                        (e, ((f, g), h)) = (1, ((2, 3), 4));
                        (int j, int k) = (1, 2);
                    }

                    public void Faz()
                    {
                        (int j, int k) = (1, 2);
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();

            var foo = HelpMethods.GetMethodDeclaration("Foo", root);
            var bar = HelpMethods.GetMethodDeclaration("Bar", root);
            var baz = HelpMethods.GetMethodDeclaration("Baz", root);
            var faz = HelpMethods.GetMethodDeclaration("Faz", root);
            var assignees1 = foo.GetAssignees().Union(foo.GetUnaryAssignees());
            var assignees2 = bar.GetAssignees().Union(bar.GetUnaryAssignees());
            var assignees3 = baz.GetAssignees().Union(baz.GetUnaryAssignees());
            var assignees4 = faz.GetAssignees().Union(faz.GetUnaryAssignees());

            Assert.Equal(assignees1.Count(), 10);
            Assert.Equal(3, ContainsAmountOfIdentifiers(assignees1, "c1"));
            Assert.Equal(3, ContainsAmountOfIdentifiers(assignees1, "c2"));
            Assert.Equal(1, ContainsAmountOfIdentifiers(assignees1, "val"));
            Assert.Equal(1, ContainsAmountOfIdentifiers(assignees1, "arr"));

            Assert.Equal(assignees2.Count(), 2);
            Assert.Equal(1, ContainsAmountOfIdentifiers(assignees2, "a"));
            Assert.Equal(1, ContainsAmountOfIdentifiers(assignees2, "b"));

            Assert.Equal(assignees3.Count(), 4);
            Assert.Equal(1, ContainsAmountOfIdentifiers(assignees3, "e"));
            Assert.Equal(1, ContainsAmountOfIdentifiers(assignees3, "f"));
            Assert.Equal(1, ContainsAmountOfIdentifiers(assignees3, "g"));
            Assert.Equal(1, ContainsAmountOfIdentifiers(assignees3, "h"));

            Assert.Equal(assignees4.Count(), 0);
            Assert.Equal(1, ContainsAmountOfIdentifiers(assignees3, "e"));

            int ContainsAmountOfIdentifiers(
                IEnumerable<IdentifierNameSyntax> assignees,
                string identifier
            )
            {
                return assignees
                    .Where(a =>
                        CSharpMethod
                            .GetBaseIdentifiers(a)
                            .ToString()
                            .Equals(identifier)
                    ).Count();
            }
        }

        [Fact]
        public void TestGetAssignees()
        {
            var file = (@"
                class Class1
                {
                    int val = 0;

                    public string Foo(int baz)
                    {
                        val = 1;
                        bar = 42;
                        val++;
                        val--;
                        ++val;
                        --val;
                        baz = val;
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();

            var foo = HelpMethods.GetMethodDeclaration("Foo", root);

            var assignments = foo.GetAssignees();
            Assert.Equal(3, assignments.Count());
            Assert.True(assignments.Where(a => a.ToString().Equals("val")).Any());
            Assert.True(assignments.Where(a => a.ToString().Equals("bar")).Any());
            Assert.True(assignments.Where(a => a.ToString().Equals("baz")).Any());
        }

        [Fact]
        public void TestGetUnaryAssignees()
        {
            var file = (@"
                class Class1
                {
                    int val1 = 0;
                    int val2 = 0;
                    int val3 = 0;
                    int val4 = 0;

                    public void Foo(int baz)
                    {
                        int val = 1;
                        baz = baz + 42;
                        val1++;
                        val2--;
                        ++val3;
                        --val4;
                        baz = val;
                    }

                    public int Bar()
                    {
                        int bar = 0;
                        return bar++;
                    }

                    public void Baz()
                    {
                        bool b = true;
                        // Logical negator is also a
                        // PrefixUnaryExpressionSyntax, but isn't an assignment
                        !b;
                    }
                }
            ");
            PurityAnalyzer PurityAnalyzer = new PurityAnalyzer(file);
            var tree = PurityAnalyzer.LookupTable.Trees.First();
            var root = (CompilationUnitSyntax) tree.GetRoot();

            var foo = HelpMethods.GetMethodDeclaration("Foo", root);
            var bar = HelpMethods.GetMethodDeclaration("Bar", root);
            var baz = HelpMethods.GetMethodDeclaration("Baz", root);

            var assignments1 = foo.GetUnaryAssignees();
            var assignments2 = bar.GetUnaryAssignees();
            var assignments3 = baz.GetUnaryAssignees();

            Assert.Equal(4, assignments1.Count());
            Assert.True(assignments1.Where(a => a.ToString().Equals("val1")).Any());
            Assert.True(assignments1.Where(a => a.ToString().Equals("val2")).Any());
            Assert.True(assignments1.Where(a => a.ToString().Equals("val3")).Any());
            Assert.True(assignments1.Where(a => a.ToString().Equals("val4")).Any());

            Assert.Equal(1, assignments2.Count());
            Assert.True(assignments2.Where(a => a.ToString().Equals("bar")).Any());

            Assert.Equal(0, assignments3.Count());
        }

        [Fact]
        public void TestIdentifierIsFresh()
        {
            var file = (@"
                namespace ConsoleApp2
                {
                    class Class1
                    {
                        int val = 0;

                        public void Foo()
                        {
                            var bar = 42;
                            bar = 43;
                            val = 1;
                        }

                        public void Bar(int baz)
                        {
                            var bar = 42;
                            val = 1;
                            val++;
                            baz = 9;
                        }
                    }
                }
            ");
            PurityAnalyzer PurityAnalyzer = new PurityAnalyzer(file);
            var tree = PurityAnalyzer.LookupTable.Trees.First();
            var root = (CompilationUnitSyntax) tree.GetRoot();

            var foo = HelpMethods.GetMethodDeclaration("Foo", root);
            var bar = HelpMethods.GetMethodDeclaration("Bar", root);

            var valAssignment = HelpMethods.GetAssignmentByName("val", foo);
            var barAssignment = HelpMethods.GetAssignmentByName("bar", foo);
            var valAssignment2 = HelpMethods.GetAssignmentByName("val", bar);
            var bazAssignment = HelpMethods.GetAssignmentByName("baz", bar);


            Assert.False(PurityAnalyzer.IdentifierIsFresh(valAssignment, foo));
            Assert.True(PurityAnalyzer.IdentifierIsFresh(barAssignment, foo));
            Assert.False(PurityAnalyzer.IdentifierIsFresh(valAssignment2, foo));
            Assert.False(PurityAnalyzer.IdentifierIsFresh(bazAssignment, bar));
        }

        [Fact]
        public void TestModifiesNonFreshIdentifier()
        {
            var file = (@"
                namespace ConsoleApp2
                {
                    class Class1
                    {
                        public int val = 0;
                        public Class2 c2 = new Class2();

                        public void Foo()
                        {
                            var bar = 42;
                            bar = 43;
                            val = 1;
                        }

                        public void Bar(int baz)
                        {
                            var bar = 42;
                            val = 1;
                            val++;
                            baz = 9;
                        }

                        public int Square(int val)
                        {
                            return val * val;
                        }

                        public class Class2
                        {
                            public int val2 = 10;
                        }
                    }

                    class Class3
                    {
                        public void Baz()
                        {
                            Class1 c1 = new Class1();
                            c1.c2.val2 = 1;
                        }
                    }
                }
            ");
            PurityAnalyzer PurityAnalyzer = new PurityAnalyzer(file);
            var tree = PurityAnalyzer.LookupTable.Trees.First();
            var root = (CompilationUnitSyntax) tree.GetRoot();

            var foo = HelpMethods.GetMethodDeclaration("Foo", root);
            var bar = HelpMethods.GetMethodDeclaration("Bar", root);
            var square = HelpMethods.GetMethodDeclaration("Square", root);
            var baz = HelpMethods.GetMethodDeclaration("Baz", root);

            Assert.True(PurityAnalyzer.ModifiesNonFreshIdentifier(foo) ?? false);
            Assert.True(PurityAnalyzer.ModifiesNonFreshIdentifier(bar) ?? false);
            Assert.False(PurityAnalyzer.ModifiesNonFreshIdentifier(square) ?? false);
            Assert.False(PurityAnalyzer.ModifiesNonFreshIdentifier(baz) ?? false);
        }
    }

    public static class HelpMethods
    {
        public static CSharpMethod GetMethodDeclaration(
            string name,
            SyntaxNode root
        )
        {
            var methodDeclaration = root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.Text == name)
                .Single();
            return new CSharpMethod(methodDeclaration);
        }

        public static ExpressionSyntax GetAssignmentByName(string name, CSharpMethod cSharpMethod)
        {
            return cSharpMethod.GetAssignees().Where(a => a.ToString() == name).Single();
        }

        // Rows need to be in the same order in both tables
        public static bool TablesAreEqual(DataTable table1, DataTable table2)
        {
            if (table1.Rows.Count != table1.Rows.Count) return false;

            for (int i = 0; i < table1.Rows.Count; i++)
            {
                if (!RowsAreEqual(table1.Rows[i], table2.Rows[i])) return false;
            }

            return true;

            // Dependency fields can be in different order
            static bool RowsAreEqual(DataRow row1, DataRow row2)
            {
                return
                    row1.Field<CSharpMethod>("identifier").Equals(row2.Field<CSharpMethod>("identifier")) &&
                    row1.Field<PurityValue>("purity").Equals(row2.Field<PurityValue>("purity")) &&
                    HaveEqualElements(
                        row1.Field<List<CSharpMethod>>("dependencies"),
                        row2.Field<List<CSharpMethod>>("dependencies")
                    );
            }
        }

        public static bool HaveEqualElements(IEnumerable<Object> list1, IEnumerable<Object> list2)
        {
            if (list1.Count() != list2.Count()) return false;
            foreach (var item in list1)
            {
                if (!list2.Contains(item)) return false;
            }

            return true;
        }
    }

    public class MethodTest
    {
        [Fact]
        public void TestMethod()
        {
            var file = (@"
                class C1
                {
                    void foo()
                    {
                        Console.WriteLine();
                        C2.bar();
                    }
                }

                class C2
                {
                    public static int bar()
                    {
                        return 2;
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();

            var clwInvocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>().First();
            var barInvocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>().Last();

            SemanticModel model = PurityAnalyzer.GetSemanticModel(new List<SyntaxTree> {tree}, tree);

            var clwMethod = new CSharpMethod(clwInvocation, model);
            var barMethod = new CSharpMethod(barInvocation, model);

            Assert.Equal("Console.WriteLine", clwMethod.Identifier);
            Assert.Null(clwMethod.Declaration);
            Assert.Null(barMethod.Identifier);
            Assert.Equal(
                barMethod,
                HelpMethods.GetMethodDeclaration("bar", root)
            );
        }

        [Fact]
        public void TestIsUnsafe()
        {
            var file = (@"
                class C1
                {
                    unsafe int Foo()
                    {
                        return 1;
                    }

                    public int Bar() => 3;
                }

                unsafe class C2
                {
                    int Baz()
                    {
                        return 1;
                    }
                }

                class C3
                {
                    int Buz()
                    {
                        return 1;
                    }
                }

                unsafe struct S1
                {
                    int Faz()
                    {
                        return 1;
                    }
                }


                struct S2
                {
                    int Fuz()
                    {
                        return 1;
                    }
                }
            ");
            LookupTable resultTable = new PurityAnalyzer(file).Analyze();
            var fooDeclaration = resultTable.GetMethodByName("Foo");
            var barDeclaration = resultTable.GetMethodByName("Bar");
            var bazDeclaration = resultTable.GetMethodByName("Baz");
            var buzDeclaration = resultTable.GetMethodByName("Buz");
            var fazDeclaration = resultTable.GetMethodByName("Faz");
            var fuzDeclaration = resultTable.GetMethodByName("Fuz");

            Assert.True(fooDeclaration.IsUnsafe());
            Assert.False(barDeclaration.IsUnsafe());
            Assert.True(bazDeclaration.IsUnsafe());
            Assert.False(buzDeclaration.IsUnsafe());
            Assert.True(fazDeclaration.IsUnsafe());
            Assert.False(fuzDeclaration.IsUnsafe());
        }

        [Fact]
        public void TestFlattenTuple()
        {
            var file = (@"
                class Class1
                {
                    public void Baz()
                    {
                        var t = (99, 98, 97);

                        int e = 1, f = 2;
                        int g = 2, h = 3;
                        (e, ((f, g), h)) = (1, ((2, 3), 4));
                    }
                }
            ");
            var tree = CSharpSyntaxTree.ParseText(file);
            var root = (CompilationUnitSyntax) tree.GetRoot();

            var i = CSharpMethod.FlattenTuple(
                root.DescendantNodes().OfType<TupleExpressionSyntax>().First()
            );

            var smallTuple = root
                .DescendantNodes()
                .OfType<TupleExpressionSyntax>()
                .First();

            var largeTuple = root
                .DescendantNodes()
                .OfType<TupleExpressionSyntax>()
                .ElementAt(1);

            var flatSmallTuple = CSharpMethod.FlattenTuple(smallTuple);
            Assert.Equal(3, flatSmallTuple.Count());
            Assert.Equal(1, ContainsAmountOfIdentifiers(flatSmallTuple, "99"));
            Assert.Equal(1, ContainsAmountOfIdentifiers(flatSmallTuple, "98"));
            Assert.Equal(1, ContainsAmountOfIdentifiers(flatSmallTuple, "97"));

            var flatLargeTuple = CSharpMethod.FlattenTuple(largeTuple);
            Assert.Equal(4, flatLargeTuple.Count());
            Assert.Equal(1, ContainsAmountOfIdentifiers(flatLargeTuple, "e"));
            Assert.Equal(1, ContainsAmountOfIdentifiers(flatLargeTuple, "f"));
            Assert.Equal(1, ContainsAmountOfIdentifiers(flatLargeTuple, "g"));
            Assert.Equal(1, ContainsAmountOfIdentifiers(flatLargeTuple, "h"));

            int ContainsAmountOfIdentifiers(
                IEnumerable<ExpressionSyntax> expressions, string identifier
            )
            {
                return expressions
                    .Where(e => e
                        .ToString()
                        .Equals(identifier))
                    .Count();
            }
        }
    }
}