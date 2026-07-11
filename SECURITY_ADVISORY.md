# Security Advisory: Arbitrary Code Execution (RCE) via AssemblyLoadContext

## Overview
During an architectural review of the `SPT-Check-Mods` codebase, a severe Arbitrary Code Execution (RCE) vulnerability was discovered in how third-party server mods were scanned for metadata.

## The Vulnerability
The original upstream implementation used `AssemblyLoadContext` alongside `Activator.CreateInstance` to dynamically load and inspect third-party `.dll` files in the `user/mods` directory. 

Because `AssemblyLoadContext` actively loads the assembly into the application domain, any malicious code placed inside a module initializer or a constructor would be immediately executed simply by running the `SPT-Check-Mods` scanner. This effectively turned the mod scanner into an execution vector for untrusted code.

### Vulnerable Code Pattern
```csharp
// DANGEROUS: Executes code during instantiation
var context = new CustomAssemblyLoadContext();
var assembly = context.LoadFromAssemblyPath(dllPath);
var instance = Activator.CreateInstance(type); // RCE Trigger
```

## The Fix
To eliminate this vulnerability without sacrificing the ability to read metadata, we replaced dynamic assembly loading with **static IL (Intermediate Language) bytecode analysis** using `Mono.Cecil`.

`Mono.Cecil` parses the `.dll` as a raw file stream and inspects its Abstract Syntax Tree (AST) rather than loading it into the runtime. 

### Secured Code Pattern
```csharp
// SECURE: Static analysis, no execution
using var stream = File.OpenRead(dllPath);
using var assembly = AssemblyDefinition.ReadAssembly(stream);

// Search IL instructions for properties like ModGuid and Version
foreach (var instruction in constructor.Body.Instructions) 
{
    // Extract metadata strings safely
}
```

## Recommendation for Upstream/Forks
If you maintain a fork of this project or upstream tools that scan third-party `.dll` mods, you **must** migrate away from `AssemblyLoadContext` and `Activator.CreateInstance`. Switch to a static analysis library like `System.Reflection.MetadataLoadContext` or `Mono.Cecil` to safely extract metadata without triggering execution.
