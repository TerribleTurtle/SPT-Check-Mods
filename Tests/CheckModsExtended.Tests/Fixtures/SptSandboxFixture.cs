using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CheckModsExtended.Tests.Fixtures;

public sealed class SptSandboxFixture : IDisposable
{
    public string SandboxPath { get; }
    public FakeFileSystem FileSystem { get; } = new();

    public SptSandboxFixture()
    {
        SandboxPath = Path.Combine(Path.GetTempPath(), "SptSandbox_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(SandboxPath);
        FileSystem.CreateDirectory(SandboxPath);
    }

    public string CompileDummyDll(string relativePath, string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var assemblyName = Path.GetFileNameWithoutExtension(relativePath);

        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var fullPath = Path.Combine(SandboxPath, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
            FileSystem.CreateDirectory(dir);
        }

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var errors = string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString()));
            throw new InvalidOperationException($"Failed to compile dummy DLL:\n{errors}");
        }

        FileSystem.Files[fullPath] = ms.ToArray();
        File.WriteAllBytes(fullPath, ms.ToArray());
        return fullPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(SandboxPath))
        {
            Directory.Delete(SandboxPath, recursive: true);
        }
    }
}


