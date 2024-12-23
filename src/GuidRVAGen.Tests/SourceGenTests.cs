﻿using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace GuidRVAGen.Tests;

public class SourceGenTests
{
    public static string GetGeneratedSource(string source, string fileName, bool allowUnsafeBlocks, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp13);
        var driver = CSharpGeneratorDriver.Create(
            [new GuidRVAGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None))
            .WithUpdatedParseOptions(parseOptions);

        // Dummy variable to make sure GuidAttribute is referenced correctly.
        GuidAttribute _ = new("");

        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(f => !f.IsDynamic && !string.IsNullOrEmpty(f.Location));

        var references = assemblies
            .Select(x => MetadataReference.CreateFromFile(x.Location));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "Test",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(outputKind, allowUnsafe: allowUnsafeBlocks));

        var runResult = driver.RunGenerators(compilation).GetRunResult();
        return runResult.Results[0].GeneratedSources.First(f => f.HintName == fileName).SourceText.ToString();
    }

    [Fact]
    public void NamespacePublicStaticKeywordAndRefReadonlyWithoutUnsafeTest()
    {
        const string source = """
            namespace Test;

            public partial class TestClass
            {
                [GuidRVAGen.Guid("00112233-4455-6677-8899-AABBCCDDEEFF")]
                public static partial ref readonly System.Guid TestGuid { get; }
            }
            """;

        const string expectedTemplate = """
            // <auto-generated/>
            #pragma warning disable

            namespace Test
            {
                /// <inheritdoc cref="TestClass"/>
                partial class TestClass
                {
                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("GuidRVAGen.GuidRVAGenerator", "<ASSEMBLY_VERSION>")]
                    [global::System.Diagnostics.DebuggerNonUserCode]
                    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                    public static partial ref readonly global::System.Guid TestGuid
                    {
                        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                        get
                        {
                            global::System.ReadOnlySpan<byte> guidBytes = [ 0x33, 0x22, 0x11, 0x00, 0x55, 0x44, 0x77, 0x66, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF ];
                            return ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(guidBytes));
                        }
                    }
                }
            }
            """;

        string generated = GetGeneratedSource(source, "Test.TestClass.TestGuid.g.cs", allowUnsafeBlocks: false);
        string expected = expectedTemplate.Replace("<ASSEMBLY_VERSION>", typeof(GuidRVAGenerator).Assembly.GetName().Version!.ToString());

        Assert.Equal(expected, generated, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void NamespacePublicStaticKeywordAndRefReadonlyWithUnsafeTest()
    {
        const string source = """
            namespace Test;

            public partial class TestClass
            {
                [GuidRVAGen.Guid("00112233-4455-6677-8899-AABBCCDDEEFF")]
                public static partial ref readonly System.Guid TestGuid { get; }
            }
            """;

        const string expectedTemplate = """
            // <auto-generated/>
            #pragma warning disable

            namespace Test
            {
                /// <inheritdoc cref="TestClass"/>
                unsafe partial class TestClass
                {
                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("GuidRVAGen.GuidRVAGenerator", "<ASSEMBLY_VERSION>")]
                    [global::System.Diagnostics.DebuggerNonUserCode]
                    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                    public static partial ref readonly global::System.Guid TestGuid
                    {
                        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                        get
                        {
                            global::System.ReadOnlySpan<byte> guidBytes = [ 0x33, 0x22, 0x11, 0x00, 0x55, 0x44, 0x77, 0x66, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF ];
                            return ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(guidBytes));
                        }
                    }
                }
            }
            """;

        string generated = GetGeneratedSource(source, "Test.TestClass.TestGuid.g.cs", allowUnsafeBlocks: true);
        string expected = expectedTemplate.Replace("<ASSEMBLY_VERSION>", typeof(GuidRVAGenerator).Assembly.GetName().Version!.ToString());

        Assert.Equal(expected, generated, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void NamespacePublicStaticKeywordAndRefWithoutUnsafeTest()
    {
        const string source = """
            namespace Test;

            public partial class TestClass
            {
                [GuidRVAGen.Guid("00112233-4455-6677-8899-AABBCCDDEEFF")]
                public static partial ref System.Guid TestGuid { get; }
            }
            """;

        const string expectedTemplate = """
            // <auto-generated/>
            #pragma warning disable

            namespace Test
            {
                /// <inheritdoc cref="TestClass"/>
                partial class TestClass
                {
                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("GuidRVAGen.GuidRVAGenerator", "<ASSEMBLY_VERSION>")]
                    [global::System.Diagnostics.DebuggerNonUserCode]
                    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                    public static partial ref global::System.Guid TestGuid
                    {
                        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                        get
                        {
                            global::System.ReadOnlySpan<byte> guidBytes = [ 0x33, 0x22, 0x11, 0x00, 0x55, 0x44, 0x77, 0x66, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF ];
                            return ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(guidBytes));
                        }
                    }
                }
            }
            """;

        string generated = GetGeneratedSource(source, "Test.TestClass.TestGuid.g.cs", allowUnsafeBlocks: false);
        string expected = expectedTemplate.Replace("<ASSEMBLY_VERSION>", typeof(GuidRVAGenerator).Assembly.GetName().Version!.ToString());

        Assert.Equal(expected, generated, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void NamespacePublicStaticKeywordAndRefWithUnsafeTest()
    {
        const string source = """
            namespace Test;

            public partial class TestClass
            {
                [GuidRVAGen.Guid("00112233-4455-6677-8899-AABBCCDDEEFF")]
                public static partial ref System.Guid TestGuid { get; }
            }
            """;

        const string expectedTemplate = """
            // <auto-generated/>
            #pragma warning disable

            namespace Test
            {
                /// <inheritdoc cref="TestClass"/>
                unsafe partial class TestClass
                {
                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("GuidRVAGen.GuidRVAGenerator", "<ASSEMBLY_VERSION>")]
                    [global::System.Diagnostics.DebuggerNonUserCode]
                    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                    public static partial ref global::System.Guid TestGuid
                    {
                        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                        get
                        {
                            global::System.ReadOnlySpan<byte> guidBytes = [ 0x33, 0x22, 0x11, 0x00, 0x55, 0x44, 0x77, 0x66, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF ];
                            return ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(guidBytes));
                        }
                    }
                }
            }
            """;

        string generated = GetGeneratedSource(source, "Test.TestClass.TestGuid.g.cs", allowUnsafeBlocks: true);
        string expected = expectedTemplate.Replace("<ASSEMBLY_VERSION>", typeof(GuidRVAGenerator).Assembly.GetName().Version!.ToString());

        Assert.Equal(expected, generated, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void NamespacePublicStaticKeywordAndPointerTest()
    {
        const string source = """
            namespace Test;

            public unsafe partial class TestClass
            {
                [GuidRVAGen.Guid("00112233-4455-6677-8899-AABBCCDDEEFF")]
                public static partial System.Guid* TestGuid { get; }
            }
            """;

        const string expectedTemplate = """
            // <auto-generated/>
            #pragma warning disable

            namespace Test
            {
                /// <inheritdoc cref="TestClass"/>
                unsafe partial class TestClass
                {
                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("GuidRVAGen.GuidRVAGenerator", "<ASSEMBLY_VERSION>")]
                    [global::System.Diagnostics.DebuggerNonUserCode]
                    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                    public static partial global::System.Guid* TestGuid
                    {
                        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                        get
                        {
                            global::System.ReadOnlySpan<byte> guidBytes = [ 0x33, 0x22, 0x11, 0x00, 0x55, 0x44, 0x77, 0x66, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF ];
                            return (global::System.Guid*)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(guidBytes)));
                        }
                    }
                }
            }
            """;

        string generated = GetGeneratedSource(source, "Test.TestClass.TestGuid.g.cs", allowUnsafeBlocks: true);
        string expected = expectedTemplate.Replace("<ASSEMBLY_VERSION>", typeof(GuidRVAGenerator).Assembly.GetName().Version!.ToString());

        Assert.Equal(expected, generated, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void TopLevelPublicStaticKeywordAndRefReadonlyWithoutUnsafeTest()
    {
        const string source = """
            public partial class TestClass
            {
                [GuidRVAGen.Guid("00112233-4455-6677-8899-AABBCCDDEEFF")]
                public static partial ref readonly System.Guid TestGuid { get; }
            }
            """;

        const string expectedTemplate = """
            // <auto-generated/>
            #pragma warning disable

            /// <inheritdoc cref="TestClass"/>
            partial class TestClass
            {
                /// <inheritdoc/>
                [global::System.CodeDom.Compiler.GeneratedCode("GuidRVAGen.GuidRVAGenerator", "<ASSEMBLY_VERSION>")]
                [global::System.Diagnostics.DebuggerNonUserCode]
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                public static partial ref readonly global::System.Guid TestGuid
                {
                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        global::System.ReadOnlySpan<byte> guidBytes = [ 0x33, 0x22, 0x11, 0x00, 0x55, 0x44, 0x77, 0x66, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF ];
                        return ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(guidBytes));
                    }
                }
            }
            """;

        string generated = GetGeneratedSource(source, "TestClass.TestGuid.g.cs", allowUnsafeBlocks: false, OutputKind.ConsoleApplication);
        string expected = expectedTemplate.Replace("<ASSEMBLY_VERSION>", typeof(GuidRVAGenerator).Assembly.GetName().Version!.ToString());

        Assert.Equal(expected, generated, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void TopLevelPublicStaticKeywordAndRefReadonlyWithUnsafeTest()
    {
        const string source = """
            public partial class TestClass
            {
                [GuidRVAGen.Guid("00112233-4455-6677-8899-AABBCCDDEEFF")]
                public static partial ref readonly System.Guid TestGuid { get; }
            }
            """;

        const string expectedTemplate = """
            // <auto-generated/>
            #pragma warning disable

            /// <inheritdoc cref="TestClass"/>
            unsafe partial class TestClass
            {
                /// <inheritdoc/>
                [global::System.CodeDom.Compiler.GeneratedCode("GuidRVAGen.GuidRVAGenerator", "<ASSEMBLY_VERSION>")]
                [global::System.Diagnostics.DebuggerNonUserCode]
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                public static partial ref readonly global::System.Guid TestGuid
                {
                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        global::System.ReadOnlySpan<byte> guidBytes = [ 0x33, 0x22, 0x11, 0x00, 0x55, 0x44, 0x77, 0x66, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF ];
                        return ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(guidBytes));
                    }
                }
            }
            """;

        string generated = GetGeneratedSource(source, "TestClass.TestGuid.g.cs", allowUnsafeBlocks: true);
        string expected = expectedTemplate.Replace("<ASSEMBLY_VERSION>", typeof(GuidRVAGenerator).Assembly.GetName().Version!.ToString());

        Assert.Equal(expected, generated, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void TopLevelPublicStaticKeywordAndRefWithoutUnsafeTest()
    {
        const string source = """
            public partial class TestClass
            {
                [GuidRVAGen.Guid("00112233-4455-6677-8899-AABBCCDDEEFF")]
                public static partial ref System.Guid TestGuid { get; }
            }
            """;

        const string expectedTemplate = """
            // <auto-generated/>
            #pragma warning disable

            /// <inheritdoc cref="TestClass"/>
            partial class TestClass
            {
                /// <inheritdoc/>
                [global::System.CodeDom.Compiler.GeneratedCode("GuidRVAGen.GuidRVAGenerator", "<ASSEMBLY_VERSION>")]
                [global::System.Diagnostics.DebuggerNonUserCode]
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                public static partial ref global::System.Guid TestGuid
                {
                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        global::System.ReadOnlySpan<byte> guidBytes = [ 0x33, 0x22, 0x11, 0x00, 0x55, 0x44, 0x77, 0x66, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF ];
                        return ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(guidBytes));
                    }
                }
            }
            """;

        string generated = GetGeneratedSource(source, "TestClass.TestGuid.g.cs", allowUnsafeBlocks: false);
        string expected = expectedTemplate.Replace("<ASSEMBLY_VERSION>", typeof(GuidRVAGenerator).Assembly.GetName().Version!.ToString());

        Assert.Equal(expected, generated, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void TopLevelPublicStaticKeywordAndRefWithUnsafeTest()
    {
        const string source = """
            public partial class TestClass
            {
                [GuidRVAGen.Guid("00112233-4455-6677-8899-AABBCCDDEEFF")]
                public static partial ref System.Guid TestGuid { get; }
            }
            """;

        const string expectedTemplate = """
            // <auto-generated/>
            #pragma warning disable

            /// <inheritdoc cref="TestClass"/>
            unsafe partial class TestClass
            {
                /// <inheritdoc/>
                [global::System.CodeDom.Compiler.GeneratedCode("GuidRVAGen.GuidRVAGenerator", "<ASSEMBLY_VERSION>")]
                [global::System.Diagnostics.DebuggerNonUserCode]
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                public static partial ref global::System.Guid TestGuid
                {
                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        global::System.ReadOnlySpan<byte> guidBytes = [ 0x33, 0x22, 0x11, 0x00, 0x55, 0x44, 0x77, 0x66, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF ];
                        return ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(guidBytes));
                    }
                }
            }
            """;

        string generated = GetGeneratedSource(source, "TestClass.TestGuid.g.cs", allowUnsafeBlocks: true);
        string expected = expectedTemplate.Replace("<ASSEMBLY_VERSION>", typeof(GuidRVAGenerator).Assembly.GetName().Version!.ToString());

        Assert.Equal(expected, generated, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void TopLevelPublicStaticKeywordAndPointerTest()
    {
        const string source = """
            public unsafe partial class TestClass
            {
                [GuidRVAGen.Guid("00112233-4455-6677-8899-AABBCCDDEEFF")]
                public static partial System.Guid* TestGuid { get; }
            }
            """;

        const string expectedTemplate = """
            // <auto-generated/>
            #pragma warning disable

            /// <inheritdoc cref="TestClass"/>
            unsafe partial class TestClass
            {
                /// <inheritdoc/>
                [global::System.CodeDom.Compiler.GeneratedCode("GuidRVAGen.GuidRVAGenerator", "<ASSEMBLY_VERSION>")]
                [global::System.Diagnostics.DebuggerNonUserCode]
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                public static partial global::System.Guid* TestGuid
                {
                    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    get
                    {
                        global::System.ReadOnlySpan<byte> guidBytes = [ 0x33, 0x22, 0x11, 0x00, 0x55, 0x44, 0x77, 0x66, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF ];
                        return (global::System.Guid*)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(guidBytes)));
                    }
                }
            }
            """;

        string generated = GetGeneratedSource(source, "TestClass.TestGuid.g.cs", allowUnsafeBlocks: true);
        string expected = expectedTemplate.Replace("<ASSEMBLY_VERSION>", typeof(GuidRVAGenerator).Assembly.GetName().Version!.ToString());

        Assert.Equal(expected, generated, ignoreLineEndingDifferences: true);
    }
}