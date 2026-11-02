using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Grand.Infrastructure.Tests.Plugins.Fixtures;

/// <summary>
///     Compiles small assemblies to disk for plugin loading tests. The output is only ever inspected
///     through metadata - tests must not load it into the default AssemblyLoadContext.
/// </summary>
public static class TestPluginBuilder
{
    private static readonly Lazy<IReadOnlyList<MetadataReference>> PlatformReferences = new(() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(x => (MetadataReference)MetadataReference.CreateFromFile(x))
        .ToList());

    /// <summary>
    ///     Compiles <paramref name="source" /> into <paramref name="outputDir" />/<paramref name="assemblyName" />.dll
    ///     and returns the path. <paramref name="references" /> are additional assembly files to compile against.
    /// </summary>
    public static string Compile(string outputDir, string assemblyName, string source, Version version = null,
        params string[] references)
    {
        Directory.CreateDirectory(outputDir);

        var versionText = (version ?? new Version(1, 0, 0, 0)).ToString();
        var versionSource = $"""
                             [assembly: System.Reflection.AssemblyVersion("{versionText}")]
                             [assembly: System.Reflection.AssemblyFileVersion("{versionText}")]
                             """;

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source), CSharpSyntaxTree.ParseText(versionSource)],
            PlatformReferences.Value.Concat(references.Select(x => MetadataReference.CreateFromFile(x))),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var path = Path.Combine(outputDir, assemblyName + ".dll");
        var result = compilation.Emit(path);
        if (!result.Success)
            throw new InvalidOperationException(string.Join(Environment.NewLine,
                result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error)));

        return path;
    }
}
