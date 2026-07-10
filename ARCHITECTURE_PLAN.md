# SPT-Check-Mods Brainstorming & Architecture Documentation

This document serves as the comprehensive technical blueprint for a future implementation project. The goal is to clean up, refactor, and deeply test the `SPT-Check-Mods` codebase without introducing new feature requirements or bloat.

The work is broken down into sequential phases.

---

## Phase 1: CI/CD & Project Maintenance (High Priority)

The repository's automation has fallen out of sync with its branch structure and tooling ecosystem. This phase fixes the pipeline.

### 1. Fix Branch Desync
The `develop` branch was deleted and merged to `main`, but several files still incorrectly target it. This prevents tests and dependency updates from running.
**Required Actions:**
- `.github/PULL_REQUEST_TEMPLATE.md`: Update target branch instructions from `develop` to `main`.
- `.github/dependabot.yml`: Update `target-branch: "develop"` to `"main"`.
- `.github/workflows/format.yml`: Change `on: push: branches: [develop]` to `[main]`.
- `.github/workflows/test.yml`: Change `on: push: branches: [develop]` to `[main]`.
- `README.md`: Update any references instructing users to check out the `develop` branch.

### 2. Update Action Versions
All workflow files currently reference `actions/checkout@v7`. This version does not exist (the latest is `v4`), which causes the pipeline to silently fail or error out during the checkout phase.
**Required Action:** 
- Downgrade/fix to `actions/checkout@v4` across all `.yml` workflows.

### 3. Native Static Analysis over CSharpier
Currently, the project uses `CSharpier`, which is an opinionated aesthetic formatter that ignores the strict rules defined in `.editorconfig`. For AI-assisted development, strict syntactic constraints (like braces, explicit typing) provided by native Roslyn analyzers are much better context signals than purely aesthetic formatting.
**Required Actions:**
- Remove `CSharpier` from the project and CI pipeline.
- Enforce strict `.editorconfig` rules by adding a `dotnet format analyzers --verify-no-changes` (or `dotnet build -warnaserror`) step to `test.yml`.

> [!NOTE] 
> Per requirements, macOS builds will **never** be added to the release matrix.

---

## Phase 2: Testing Coverage (Critical Priority)

Test coverage must be expanded significantly. The algorithmic services are well tested, but the I/O and orchestration logic are completely blind spots.

### 1. Test Core Services
Tests must adhere to `AGENTS.md` (no mocking frameworks, hand-crafted fakes, `[Fact]` only).
**Required Actions:**
- **`ModScannerService`**: Create integration/file-system tests using a temporary directory structure mimicking an SPT install. Test valid mod parsing, missing files, cross-installed folders, and misplaced mods.
- **`ApplicationService`**: Test the orchestration pipeline using fake/stub implementations of the injected services.
- **`SptInstallationService`**: Test reading version info from the core SPT DLL.
- **`ModEnrichmentService`**: Test the enrichment mapping logic.

---

## Phase 3: Architectural Decoupling & Refactoring

Applying SOLID principles to decouple the largest and most complex services.

### 1. Refactor `ApplicationService`
`RunAsync` is >90 lines and mixes high-level orchestration with low-level details.
**Required Actions:**
- Extract low-level operations (e.g., `RemoveLegacyApiKeyFile()`, `GetValidatedSptPath()`) into a dedicated `IInitializationService` or private helper methods.
- Ensure `RunAsync` acts purely as a highly-readable pipeline coordinator.

### 2. Refactor `ModScannerService`
This service currently violates the Single Responsibility Principle by mixing file I/O, custom AssemblyLoadContext reflection, BepInPlugin parsing, and cross-installation detection heuristics.
**Required Actions:**
- Extract the BepInEx metadata reflection into a `PluginMetadataExtractor`.
- Extract the SPT Server Mod reflection into a `ServerModExtractor`.
- Extract the cross-installation union-find heuristics into a `MisplacedModDetector`.

### 3. Decouple `SpectreModCheckReporter`
The UI reporter is a massive 1,200+ line facade. While it strictly handles UI, it is difficult to maintain.
**Required Actions:**
- Break it into specialized renderers: `ITableRenderer`, `IProgressRenderer`, `ITextRenderer`.
- Maintain `SpectreModCheckReporter` as a facade that delegates to these smaller, specialized renderer classes.

### 4. Optimize `ModReconciliationService`
**Required Actions:**
- `MatchComponents()` currently uses `O(N*M)` nested loops to pair server and client mods. Optimize this using hashsets or dictionaries mapped by `Guid` and `AuthorNamespace` to ensure it scales safely for users with massive mod lists.

### 5. Decorator Caching
`ForgeApiService` and `ModDependencyService` manually manage `IMemoryCache` state inside their core business methods.
**Required Actions:**
- Remove internal caching logic.
- Implement the Decorator pattern (or `HttpMessageHandler`) for caching HTTP responses transparently, adhering to SRP.

---

## Phase 4: Models & Utilities Cleanup

Cleaning up domain models without introducing new requirements or changing external behavior.

### 1. Deconstruct `Mod.cs` God Object
`Mod.cs` is a 275-line God Object that holds state for every phase of the pipeline and contains multiple mutation methods.
**Required Actions:**
- Split `Mod.cs` into composed pieces without altering the data contract:
  - `LocalModIdentity`: Handles name, version, and file path.
  - `ForgeApiMetadata`: Handles API URL, remote version, and matching state.
  - `ModUpdateState`: Handles update warnings and status flags.

### 2. SemVer Edge Cases
**Required Actions:**
- Modify the custom `SemVer` wrapper to safely surface validation and parsing errors on invalid semantic version constraints, rather than failing silently and returning false.

### 3. (Optional) Replace Custom Logger
The custom `FileLogger.cs` handles its own text file rotation but silently swallows `IOException`s if a file is locked by a text editor.
**Required Actions (Low Priority):**
- If ROI permits during the implementation project, replace the custom logger with `Serilog` + `Serilog.Sinks.File` for safer, battle-tested logging.
