using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace GuidRVAGen.Tests;

public class IncrementalTests
{
    public static List<Dictionary<string, string>> GetIncrementalGeneratorTrackedStepsReasons(params IEnumerable<string> sources)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp13);
        var driver = CSharpGeneratorDriver.Create(
            [new GuidRVAGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true))
            .WithUpdatedParseOptions(parseOptions);

        // Dummy variable to make sure GuidAttribute is referenced correctly.
        GuidAttribute _ = new("");

        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(f => !f.IsDynamic && !string.IsNullOrEmpty(f.Location));

        var references = assemblies
            .Select(x => MetadataReference.CreateFromFile(x.Location));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "Test",
            [],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        List<Dictionary<string, string>> results = new(sources.Count());

        foreach (string source in sources)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, parseOptions);
            Compilation newCompilation = compilation.AddSyntaxTrees(tree);
            driver = driver.RunGenerators(newCompilation);
            var reasons = driver.GetRunResult().Results[0].TrackedSteps
                            .Where(x => x.Key.StartsWith("GuidRVAGenerator.") || x.Key == "SourceOutput")
                            .Select(x =>
                            {
                                if (x.Key == "SourceOutput")
                                {
                                    var values = x.Value.Where(x => x.Inputs[0].Source.Name?.StartsWith("GuidRVAGenerator.") ?? false);
                                    return (
                                        x.Key,
                                        Reasons: string.Join(", ", values.SelectMany(x => x.Outputs).Select(x => x.Reason).ToArray())
                                    );
                                }
                                else
                                {
                                    return (
                                        Key: x.Key.Substring("GuidRVAGenerator.".Length),
                                        Reasons: string.Join(", ", x.Value.SelectMany(x => x.Outputs).Select(x => x.Reason).ToArray())
                                    );
                                }
                            });
            Dictionary<string, string> reasonsDictionary = new(2); 
            foreach (var reason in reasons)
            {
                reasonsDictionary.Add(reason.Key, reason.Reasons);
            }
            results.Add(reasonsDictionary);
        }

        return results;
    }

    [Fact]
    public void TestUnrelatedChange()
    {
        string step1 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static partial ref readonly System.Guid SampleGuid { get; }
            }
            """;

        string step2 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static partial ref readonly System.Guid SampleGuid { get; }
                
                public void UnrelatedChange()
                {}
            }
            """;

        var results = GetIncrementalGeneratorTrackedStepsReasons(step1, step2);
        Assert.Equal("New", results[0]["CollectProperties"]);
        Assert.Equal("New", results[0]["CollectPropertiesFiltered"]);
        Assert.Equal("New", results[0]["SourceOutput"]);
        Assert.Equal("Unchanged", results[1]["CollectProperties"]);
        Assert.Equal("Cached", results[1]["CollectPropertiesFiltered"]);
        Assert.Equal("Cached", results[1]["SourceOutput"]);
    }

    [Fact]
    public void TestGuidChange()
    {
        string step1 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static partial ref readonly System.Guid SampleGuid { get; }
            }
            """;

        string step2 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("11223344-5566-7788-9900-AABBCCDDEEFF")]
                public static partial ref readonly System.Guid SampleGuid { get; }
            }
            """;

        var results = GetIncrementalGeneratorTrackedStepsReasons(step1, step2);
        Assert.Equal("New", results[0]["CollectProperties"]);
        Assert.Equal("New", results[0]["CollectPropertiesFiltered"]);
        Assert.Equal("New", results[0]["SourceOutput"]);
        Assert.Equal("Modified", results[1]["CollectProperties"]);
        Assert.Equal("Modified", results[1]["CollectPropertiesFiltered"]);
        Assert.Equal("Modified", results[1]["SourceOutput"]);
    }

    [Fact]
    public void TestRefReadonlyToRefChange()
    {
        string step1 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static partial ref readonly System.Guid SampleGuid { get; }
            }
            """;

        string step2 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static partial ref System.Guid SampleGuid { get; }
            }
            """;

        var results = GetIncrementalGeneratorTrackedStepsReasons(step1, step2);
        Assert.Equal("New", results[0]["CollectProperties"]);
        Assert.Equal("New", results[0]["CollectPropertiesFiltered"]);
        Assert.Equal("New", results[0]["SourceOutput"]);
        Assert.Equal("Modified", results[1]["CollectProperties"]);
        Assert.Equal("Modified", results[1]["CollectPropertiesFiltered"]);
        Assert.Equal("Modified", results[1]["SourceOutput"]);
    }

    [Fact]
    public void TestRefReadonlyToPointerChange()
    {
        string step1 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static partial ref readonly System.Guid SampleGuid { get; }
            }
            """;

        string step2 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static unsafe partial System.Guid* SampleGuid { get; }
            }
            """;

        var results = GetIncrementalGeneratorTrackedStepsReasons(step1, step2);
        Assert.Equal("New", results[0]["CollectProperties"]);
        Assert.Equal("New", results[0]["CollectPropertiesFiltered"]);
        Assert.Equal("New", results[0]["SourceOutput"]);
        Assert.Equal("Modified", results[1]["CollectProperties"]);
        Assert.Equal("Modified", results[1]["CollectPropertiesFiltered"]);
        Assert.Equal("Modified", results[1]["SourceOutput"]);
    }

    [Fact]
    public void TestRefToRefReadonlyChange()
    {
        string step1 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static partial ref System.Guid SampleGuid { get; }
            }
            """;

        string step2 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static partial ref readonly System.Guid SampleGuid { get; }
            }
            """;

        var results = GetIncrementalGeneratorTrackedStepsReasons(step1, step2);
        Assert.Equal("New", results[0]["CollectProperties"]);
        Assert.Equal("New", results[0]["CollectPropertiesFiltered"]);
        Assert.Equal("New", results[0]["SourceOutput"]);
        Assert.Equal("Modified", results[1]["CollectProperties"]);
        Assert.Equal("Modified", results[1]["CollectPropertiesFiltered"]);
        Assert.Equal("Modified", results[1]["SourceOutput"]);
    }

    [Fact]
    public void TestRefToPointerChange()
    {
        string step1 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static partial ref System.Guid SampleGuid { get; }
            }
            """;

        string step2 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static unsafe partial System.Guid* SampleGuid { get; }
            }
            """;

        var results = GetIncrementalGeneratorTrackedStepsReasons(step1, step2);
        Assert.Equal("New", results[0]["CollectProperties"]);
        Assert.Equal("New", results[0]["CollectPropertiesFiltered"]);
        Assert.Equal("New", results[0]["SourceOutput"]);
        Assert.Equal("Modified", results[1]["CollectProperties"]);
        Assert.Equal("Modified", results[1]["CollectPropertiesFiltered"]);
        Assert.Equal("Modified", results[1]["SourceOutput"]);
    }

    [Fact]
    public void TestPointerToRefReadonlyChange()
    {
        string step1 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static unsafe partial System.Guid* SampleGuid { get; }
            }
            """;

        string step2 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static partial ref readonly System.Guid SampleGuid { get; }
            }
            """;

        var results = GetIncrementalGeneratorTrackedStepsReasons(step1, step2);
        Assert.Equal("New", results[0]["CollectProperties"]);
        Assert.Equal("New", results[0]["CollectPropertiesFiltered"]);
        Assert.Equal("New", results[0]["SourceOutput"]);
        Assert.Equal("Modified", results[1]["CollectProperties"]);
        Assert.Equal("Modified", results[1]["CollectPropertiesFiltered"]);
        Assert.Equal("Modified", results[1]["SourceOutput"]);
    }

    [Fact]
    public void TestPointerToRefChange()
    {
        string step1 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static unsafe partial System.Guid* SampleGuid { get; }
            }
            """;

        string step2 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static partial ref System.Guid SampleGuid { get; }
            }
            """;

        var results = GetIncrementalGeneratorTrackedStepsReasons(step1, step2);
        Assert.Equal("New", results[0]["CollectProperties"]);
        Assert.Equal("New", results[0]["CollectPropertiesFiltered"]);
        Assert.Equal("New", results[0]["SourceOutput"]);
        Assert.Equal("Modified", results[1]["CollectProperties"]);
        Assert.Equal("Modified", results[1]["CollectPropertiesFiltered"]);
        Assert.Equal("Modified", results[1]["SourceOutput"]);
    }

    [Fact]
    public void TestAccessibilityChange()
    {
        string step1 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static unsafe partial System.Guid* SampleGuid { get; }
            }
            """;

        string step2 = """
            namespace Test;
            
            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                internal static unsafe partial System.Guid* SampleGuid { get; }
            }
            """;

        var results = GetIncrementalGeneratorTrackedStepsReasons(step1, step2);
        Assert.Equal("New", results[0]["CollectProperties"]);
        Assert.Equal("New", results[0]["CollectPropertiesFiltered"]);
        Assert.Equal("New", results[0]["SourceOutput"]);
        Assert.Equal("Modified", results[1]["CollectProperties"]);
        Assert.Equal("Modified", results[1]["CollectPropertiesFiltered"]);
        Assert.Equal("Modified", results[1]["SourceOutput"]);
    }

    [Fact]
    public void TestIsStaticChange()
    {
        string step1 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static unsafe partial System.Guid* SampleGuid { get; }
            }
            """;

        string step2 = """
            namespace Test;
            
            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public unsafe partial System.Guid* SampleGuid { get; }
            }
            """;

        var results = GetIncrementalGeneratorTrackedStepsReasons(step1, step2);
        Assert.Equal("New", results[0]["CollectProperties"]);
        Assert.Equal("New", results[0]["CollectPropertiesFiltered"]);
        Assert.Equal("New", results[0]["SourceOutput"]);
        Assert.Equal("Modified", results[1]["CollectProperties"]);
        Assert.Equal("Modified", results[1]["CollectPropertiesFiltered"]);
        Assert.Equal("Modified", results[1]["SourceOutput"]);
    }

    [Fact]
    public void TestClassNameChange()
    {
        string step1 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static unsafe partial System.Guid* SampleGuid { get; }
            }
            """;

        string step2 = """
            namespace Test;
            
            public static partial class TestClassTwo
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public unsafe partial System.Guid* SampleGuid { get; }
            }
            """;

        var results = GetIncrementalGeneratorTrackedStepsReasons(step1, step2);
        Assert.Equal("New", results[0]["CollectProperties"]);
        Assert.Equal("New", results[0]["CollectPropertiesFiltered"]);
        Assert.Equal("New", results[0]["SourceOutput"]);
        Assert.Equal("Modified", results[1]["CollectProperties"]);
        Assert.Equal("Modified", results[1]["CollectPropertiesFiltered"]);
        Assert.Equal("Modified", results[1]["SourceOutput"]);
    }

    [Fact]
    public void TestPropertyNameChange()
    {
        string step1 = """
            namespace Test;

            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public static unsafe partial System.Guid* SampleGuid { get; }
            }
            """;

        string step2 = """
            namespace Test;
            
            public static partial class TestClass
            {
                [GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
                public unsafe partial System.Guid* SampleGuidTwo { get; }
            }
            """;

        var results = GetIncrementalGeneratorTrackedStepsReasons(step1, step2);
        Assert.Equal("New", results[0]["CollectProperties"]);
        Assert.Equal("New", results[0]["CollectPropertiesFiltered"]);
        Assert.Equal("New", results[0]["SourceOutput"]);
        Assert.Equal("Modified", results[1]["CollectProperties"]);
        Assert.Equal("Modified", results[1]["CollectPropertiesFiltered"]);
        Assert.Equal("Modified", results[1]["SourceOutput"]);
    }
}