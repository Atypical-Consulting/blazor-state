using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TheBlazorState.Generators.Tests;

public static class TestHelper
{
    private static readonly Lazy<ImmutableArray<MetadataReference>> CachedReferences =
        new(BuildReferences);

    public static (ImmutableArray<Diagnostic> Diagnostics, string? GeneratedSource) RunGenerator(string source)
    {
        var result = RunGeneratorInternal(source);

        var generatedSource = result.GeneratedTrees.Length > 0
            ? result.GeneratedTrees[0].GetText().ToString()
            : null;

        return (result.Diagnostics, generatedSource);
    }

    public static ImmutableArray<Diagnostic> GetDiagnostics(string source)
    {
        var result = RunGeneratorInternal(source);
        return result.Diagnostics;
    }

    private static GeneratorDriverRunResult RunGeneratorInternal(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            CachedReferences.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new SliceIncrementalGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        return driver.GetRunResult();
    }

    private static ImmutableArray<MetadataReference> BuildReferences()
    {
        var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var metadataRefs = new List<MetadataReference>();

        void TryAdd(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path) && references.Add(path))
            {
                metadataRefs.Add(MetadataReference.CreateFromFile(path));
            }
        }

        // 1. Add all currently loaded assemblies (covers core runtime)
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
                continue;
            try
            {
                TryAdd(assembly.Location);
            }
            catch
            {
                // Skip
            }
        }

        // 2. Add the TheBlazorState library assembly
        TryAdd(typeof(TheBlazorState.Abstractions.IStateSlice<>).Assembly.Location);

        // 3. Add ASP.NET Core shared framework assemblies (ComponentBase, etc.)
        //    These are not loaded at runtime in a test, so we resolve them from disk.
        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        // runtimeDir is e.g. .../shared/Microsoft.NETCore.App/10.0.x/
        // We need .../shared/Microsoft.AspNetCore.App/10.0.x/
        var netCoreAppDir = new DirectoryInfo(runtimeDir);
        var sharedDir = netCoreAppDir.Parent?.Parent; // .../shared/
        if (sharedDir != null)
        {
            var aspNetDirs = sharedDir.GetDirectories("Microsoft.AspNetCore.App");
            if (aspNetDirs.Length > 0)
            {
                // Pick the version directory that matches our runtime major version
                var runtimeVersion = Environment.Version; // e.g. 10.0.x
                var versionDirs = aspNetDirs[0].GetDirectories($"{runtimeVersion.Major}.*");
                DirectoryInfo? aspNetDir = versionDirs.Length > 0
                    ? versionDirs.OrderByDescending(d => d.Name).First()
                    : aspNetDirs[0].GetDirectories().OrderByDescending(d => d.Name).FirstOrDefault();

                if (aspNetDir != null)
                {
                    foreach (var dll in aspNetDir.GetFiles("*.dll"))
                    {
                        TryAdd(dll.FullName);
                    }
                }
            }
        }

        return metadataRefs.ToImmutableArray();
    }
}
