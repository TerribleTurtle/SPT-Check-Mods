using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CheckMods.Tests.Fixtures;

/// <summary>
/// A test fixture that creates a temporary directory for tests to use as an SPT sandbox.
/// </summary>
public sealed class SptSandboxFixture : IDisposable
{
    /// <summary>
    /// Gets the path to the temporary sandbox directory.
    /// </summary>
    public string SandboxPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SptSandboxFixture"/> class.
    /// </summary>
    public SptSandboxFixture()
    {
        SandboxPath = Path.Combine(Path.GetTempPath(), "SptSandbox_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(SandboxPath);
    }

    /// <summary>
    /// Compiles a C# code string into a DLL at the specified path within the sandbox.
    /// </summary>
    /// <param name="relativePath">The relative path within the sandbox where the DLL will be created.</param>
    /// <param name="code">The C# code to compile.</param>
    /// <returns>The absolute path to the compiled DLL.</returns>
    public string CompileDummyDll(string relativePath, string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var assemblyName = Path.GetFileNameWithoutExtension(relativePath);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var fullPath = Path.Combine(SandboxPath, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        using var fileStream = File.Create(fullPath);
        var result = compilation.Emit(fileStream);

        if (!result.Success)
        {
            var errors = string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString()));
            throw new InvalidOperationException($"Failed to compile dummy DLL:\n{errors}");
        }

        return fullPath;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(SandboxPath))
        {
            Directory.Delete(SandboxPath, recursive: true);
        }
    }
}






