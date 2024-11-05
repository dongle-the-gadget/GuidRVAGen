using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace GuidRVAGen.Tests;

public class AnalyzerTests
{
    [Fact]
    public async Task UnknownReturnTypeDescriptor()
    {
        GuidAttribute _ = new("");

        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(f => !f.IsDynamic && !string.IsNullOrEmpty(f.Location) && (f.FullName?.StartsWith("GuidRVAGen.Attributes")).GetValueOrDefault());

        var references = assemblies
            .Select(x => MetadataReference.CreateFromFile(x.Location));

        CSharpAnalyzerTest<GuidRVAAnalyzer, DefaultVerifier> test = new CSharpAnalyzerTest<GuidRVAAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources =
                {
                    """
                    namespace Test;

                    public partial class TestClass
                    {
                        [GuidRVAGen.Guid("00000000-0000-0000-0000-000000000000")]
                        public static partial System.Guid Guid { get; }
                    }
                    """
                },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("GUIDGEN0002").WithSpan(5, 6, 5, 21)
                }
            },
            CompilerDiagnostics = CompilerDiagnostics.None,
        };
        test.TestState.AdditionalReferences.AddRange(references);
        await test.RunAsync();
    }

    [Fact]
    public async Task InvalidGuidDescriptor()
    {
        GuidAttribute _ = new("");

        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(f => !f.IsDynamic && !string.IsNullOrEmpty(f.Location) && (f.FullName?.StartsWith("GuidRVAGen.Attributes")).GetValueOrDefault());

        var references = assemblies
            .Select(x => MetadataReference.CreateFromFile(x.Location));

        CSharpAnalyzerTest<GuidRVAAnalyzer, DefaultVerifier> test = new CSharpAnalyzerTest<GuidRVAAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources =
                {
                    """
                    namespace Test;

                    public partial class TestClass
                    {
                        [GuidRVAGen.Guid("invalid")]
                        public static partial ref System.Guid Guid { get; }
                    }
                    """
                },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("GUIDGEN0003").WithSpan(5, 22, 5, 31)
                }
            },
            CompilerDiagnostics = CompilerDiagnostics.None
        };
        test.TestState.AdditionalReferences.AddRange(references);
        await test.RunAsync();
    }

    [Fact]
    public async Task PropertyHasSetterDescriptor()
    {
        GuidAttribute _ = new("");

        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(f => !f.IsDynamic && !string.IsNullOrEmpty(f.Location) && (f.FullName?.StartsWith("GuidRVAGen.Attributes")).GetValueOrDefault());

        var references = assemblies
            .Select(x => MetadataReference.CreateFromFile(x.Location));

        CSharpAnalyzerTest<GuidRVAAnalyzer, DefaultVerifier> test = new CSharpAnalyzerTest<GuidRVAAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources =
                {
                    """
                    namespace Test;

                    public partial class TestClass
                    {
                        [GuidRVAGen.Guid("00000000-0000-0000-0000-000000000000")]
                        public static partial ref System.Guid Guid { get; set; }
                    }
                    """
                },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("GUIDGEN0004").WithSpan(5, 6, 5, 21)
                }
            },
            CompilerDiagnostics = CompilerDiagnostics.None
        };
        test.TestState.AdditionalReferences.AddRange(references);
        await test.RunAsync();
    }

    [Fact]
    public async Task PropertyNotPartialDescriptor()
    {
        GuidAttribute _ = new("");

        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(f => !f.IsDynamic && !string.IsNullOrEmpty(f.Location) && (f.FullName?.StartsWith("GuidRVAGen.Attributes")).GetValueOrDefault());

        var references = assemblies
            .Select(x => MetadataReference.CreateFromFile(x.Location));

        CSharpAnalyzerTest<GuidRVAAnalyzer, DefaultVerifier> test = new CSharpAnalyzerTest<GuidRVAAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources =
                {
                    """
                    namespace Test;

                    public partial class TestClass
                    {
                        [GuidRVAGen.Guid("00000000-0000-0000-0000-000000000000")]
                        public static ref System.Guid Guid { get; }
                    }
                    """
                },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("GUIDGEN0005").WithSpan(5, 6, 5, 21)
                }
            },
            CompilerDiagnostics = CompilerDiagnostics.None
        };
        test.TestState.AdditionalReferences.AddRange(references);
        await test.RunAsync();
    }

}
