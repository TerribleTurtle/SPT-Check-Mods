# SPT-Check-Mods Architecture Plan (V3: Documentation & Drift Resolution)

This document outlines the next phases of development for `SPT-Check-Mods`. Previous phases (V1/V2 - RCE Patch, UI Decoupling, Error Handling, and Object Deconstruction) are complete. 

The current focus is resolving **Documentation Drift** (where the code has outpaced the documentation) and fixing underlying code anomalies discovered during the documentation audit.

All phases must maintain the strict `AGENTS.md` guidelines:
- Strict `.editorconfig` formatting.
- `OneOf` error handling.
- `xUnit` testing with hand-crafted fakes (no mocking frameworks).

---

## Phase 5: Core Infrastructure & Logging Synchronization

This phase resolves severe discrepancies where the infrastructure code contradicts the documentation and project state.

### 1. Serilog Migration Completion
- **Issue:** `README.md` and `findings.md` state the app uses Serilog, but `ServiceCollectionExtensions.cs` still uses `builder.AddFileLogger()` (the custom `FileLogger.cs`).
- **Action:** Wire up Serilog in Dependency Injection and deprecate/remove the 311-line custom `FileLogger.cs`.

### 2. Upstream References & User-Agents
- **Issue:** `User-Agent` HTTP headers in `ForgeApiService` and `RemoteIgnoreFileClient` still hardcode the upstream `refringe/SPT-Check-Mods` URL.
- **Action:** Update all hardcoded references to the `TerribleTurtle` fork.

### 3. README.md & Transparency Updates
- **Action:** Update the README to reflect the accurate .NET version.
- **Action:** Document the remote telemetry network call to `forge-static.sp-tarkov.com` for transparency (`ignored-updates.json`).
- **Action:** Add CLI troubleshooting commands (enabling debug logs, overriding rate limits).
- **Action:** Explain the "Magic Termination Logic" (`!Console.IsInputRedirected`) in `Program.cs` via comments.

---

## Phase 6: Models & Configuration Documentation

This phase focuses on documenting the newly decoupled data models and configuration classes.

### 1. Extracted Composition Records
- **Action:** Add class and property-level XML documentation to `LocalModIdentity.cs`, `ForgeApiMetadata.cs`, and `ModUpdateState.cs`.
- **Action:** Document the new `Local`, `Api`, and `Update` properties in `Mod.cs`.

### 2. Primary Constructors & Properties
- **Action:** Add missing `<param>` tags for primary constructors in `ApiResponses.cs`, `PluginDll.cs`, and `IgnoredUpdate.cs`.
- **Action:** Add missing `<summary>` tags in `DependencyChange.cs`, `SptVersionResponse.cs`, `ForgeApiOptions.cs`, and `LoggingOptions.cs`.

### 3. Magic Constants Context
- **Action:** Document the "why" behind thresholds in `MatchingConstants.cs` (`70` and `80`) and limits in `ModScannerOptions.cs` (`100MB`).
- **Action:** Explicitly document how `RateLimitOptions.cs` maps to Forge API constraints.

---

## Phase 7: Services & API Enhancements

This phase addresses missing documentation in the core business logic and fixes a remaining silent error swallow.

### 1. Error Handling Fix
- **Issue:** `ModDependencyService.cs` silently swallows `SemVer.TryParse` errors by returning `0.0.0`, violating Phase 4 error handling rules.
- **Action:** Refactor the dependency version comparison to bubble up `InvalidSemVer` natively via `OneOf`.

### 2. API Usage Examples
- **Action:** Add concrete usage examples to the `IForgeApiService` endpoint documentation.
- **Action:** Remove outdated XML documentation on `ForgeApiService.GetJsonAsync` that incorrectly claims it caches responses by URL.

### 3. Internal Service Documentation
- **Action:** Rewrite the `TableRenderer.cs` class summary to explain its architectural shift into a Façade pattern delegating to 4 UI renderers.
- **Action:** Add missing `<param>` documentation to utility methods in `ApplicationService.cs` and `ModScannerService.cs`.
- **Action:** Review the unused `sptDirectory` parameter in `IServerModExtractor.cs` and remove it or document it for future use.
