# SPT-Check-Mods Architecture Plan (V2: Deep Decoupling)

This document outlines the next phases of refactoring and architectural decoupling for `SPT-Check-Mods`. The original foundational work (RCE patch, initial test coverage, and high-level class breakdown) is complete. This next iteration targets the remaining "God Objects" and bloated files.

All phases must maintain the strict `AGENTS.md` guidelines:
- Strict `.editorconfig` formatting.
- `OneOf` error handling.
- `xUnit` testing with hand-crafted fakes (no mocking frameworks).

---

## Phase 1: ApplicationService Deep Decoupling

`ApplicationService.cs` is currently a 740-line pipeline orchestrator that still contains too much low-level domain logic. It should be reduced to a strict workflow coordinator without any business logic of its own.

### 1. Extract `IModResolutionService`
**Goal:** Handle all fuzzy search and Forge API resolution logic.
- Extract `SearchModByNameAsync`.
- Extract `FetchSourceCodeUrlForPairAsync` and `FetchSourceCodeUrlForModAsync`.
- **Testing Requirement:** Add comprehensive `xUnit` tests for the new service to cover exact matches, fuzzy threshold matches, and missing mods.

### 2. Extract `ICompatibilityValidationService`
**Goal:** Isolate SPT version constraint parsing and verification.
- Extract `CheckModSptCompatibility` and `CheckModVersionCompatibility`.
- **Testing Requirement:** Ensure the `SemanticVersioning` logic is correctly evaluated within this dedicated service using targeted unit tests.

### 3. Extract `IUpdateOrchestrationService`
**Goal:** Manage update checks for SPT and Check Mods.
- Extract `CheckForSptUpdatesAsync` and `CheckForCheckModsUpdateAsync`.
- Extract update suppression mapping (`ApplyIgnoredUpdates`).
- **Testing Requirement:** Add tests validating that ignored updates successfully suppress warnings without mutating other mod state.

---

## Phase 2: UI Presentation Decoupling

`TableRenderer.cs` (678 lines) violates the Single Responsibility Principle by rendering entirely distinct UI components (version tables, dependency trees, and misplaced mod warnings).

### 1. Split into Specialized Renderers
- `DependencyUiRenderer`: Renders `DependencyTree`, `DependencyConflicts`, and `MissingDependencies`.
- `MisplacedModUiRenderer`: Renders `MisplacedMods` and `PrintCrossInstalledDirectory`.
- `VersionTableUiRenderer`: Renders `VersionTable` and `VersionCompatibilityResults`.
- `ReconciliationUiRenderer`: Renders `ReconciliationResults` and `LoadingWarnings`.

### 2. Maintain Façade
- Retain `TableRenderer` (or consolidate back to `SpectreModCheckReporter`) purely as a facade that delegates calls to these specialized renderers to avoid breaking the `IApplicationService` interface.
- **Testing Requirement:** Update `TableRendererTests.cs` and build test coverage for each new individual UI renderer, ensuring AnsiConsole output formatting is not compromised.

---

## Phase 3: Forge API Client Breakdown

`ForgeApiService.cs` (500+ lines) handles all API endpoints (Updates, Search, Validation, Dependencies) in a single service. 

### 1. Split into Distinct API Clients
- `IForgeUpdateClient`: Handles `/mods/updates`.
- `IForgeSearchClient`: Handles `/mods?query=` and `/mod/{id}`.
- `IForgeValidationClient`: Handles `/spt/versions`.
- `IForgeDependencyClient`: Handles `/mods/dependencies`.

### 2. Implementation details
- Standardize the error translation (HTTP 404, 500, network timeouts) into the defined `ApiError`, `NotFound` types using `OneOf`.
- **Testing Requirement:** Port the existing `FakeForgeApiService` logic to mirror this new structure. Ensure each client is tested for rate-limiting, retries, and network failure resiliency.

---

## Phase 4: Client Mod Extractor Optimization

`PluginMetadataExtractor.cs` (540 lines) mixes file I/O, raw Reflection (`MetadataLoadContext`), and heuristic directory merging.

### 1. Extract `BepinReflectionHelper`
- Pull out the pure reflection logic (`CreateMetadataLoadContext`, `ScanAssemblyForBepInPluginAttribute`) into a static helper or dedicated singleton.
- **Testing Requirement:** Add file-less reflection tests using in-memory byte arrays.

### 2. Extract `ClientModConsolidator`
- Move the heuristic union-find logic (`PartitionByRelatedness`, `SameAuthorNamespace`) that groups related DLLs into a single mod entity into its own isolated service.
- **Testing Requirement:** Test the consolidation logic extensively with dummy Mod dependency graphs to ensure unrelated mods aren't incorrectly merged.
