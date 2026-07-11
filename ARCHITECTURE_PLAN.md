# CheckMods Architecture Plan

This document outlines the upcoming foundational, architectural, and performance improvements for the CheckMods project. 
These tasks are grouped logically to ensure stability and strict rule adherence are established before performance optimizations are made.

## Phase 1: Build & Quality Enforcement (Shift Left)
Before we make any further code modifications, we must guarantee that all code strictly adheres to quality and style guidelines natively during the build process.
- **Enforce Code Style on Build:** Update `CheckMods.csproj` to include `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`, `<EnableNETAnalyzers>true</EnableNETAnalyzers>`, and `<AnalysisLevel>latest-All</AnalysisLevel>`.
- **Treat Warnings as Errors:** Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to the `.csproj`. This ensures that any introduced warnings or `.editorconfig` violations will instantly fail the local `dotnet build`.

## Phase 2: Network Resilience & Reliability
The application relies heavily on external resources (the Forge API and community ignore-lists). We need to protect the application from transient network failures and rate limits.
- **Implement Polly Resilience:** Install the `Microsoft.Extensions.Http.Resilience` package.
- **Configure HTTP Clients:** Append `.AddStandardResilienceHandler()` to the `ForgeApi` and `IRemoteIgnoreFileClient` registrations in `ServiceCollectionExtensions.cs`. This provides industry-standard retries, exponential backoff, and circuit breakers out-of-the-box.

## Phase 3: Performance Optimization
Extracting metadata from DLLs is an I/O-heavy operation. The client (BepInEx) plugins are currently scanned in parallel, but the server (`user/mods`) mods are scanned sequentially.
- **Parallelize Server Mod Scanning:** Refactor `ModScannerService.ScanServerModsAsync` to replace the synchronous `foreach` loop with `Parallel.ForEachAsync`.
- **Thread Safety:** Utilize a `ConcurrentBag<Mod>` to ensure thread-safe accumulation of discovered mods during the parallel scan.

## Phase 4: Rule Adherence & Cleanup
The project `AGENTS.md` rule states: *"All classes public sealed — composition over inheritance."* While core models and services comply, the test project contains several unsealed stubs.
- **Seal Test Classes:** Navigate through `Tests/CheckMods.Tests/` and apply the `sealed` modifier to all stub and fake classes (e.g., `TestServerMod`, `TestClientPlugin`, `BepInPluginAttribute`, `WrongServerMod`). This achieves 100% codebase-wide compliance.
