# Task Plan: Web UI Redesign & Backend Enrichment

## Goal
Redesign the Check Mods Extended web UI into a tokenized, responsive interface and enrich backend data endpoints to provide a "3-second health check" scanability.

## Current Stage
Phase 3

## Stages
### Phase 2: Backend Data Enrichment
- [x] Step 2.1 — Expand DTOs
- [x] Step 2.2 — New Endpoints & Mapping
- **Status:** completed

### Phase 3: HTML Structure & Information Hierarchy
- [x] Step 3.1 — Header, Health Banner & Stats Panel
- [x] Step 3.2 — Search Toolbar & Table Structure
- [x] Step 3.3 — Console & Footer
- **Status:** completed

### Phase 4: JavaScript Refactor, Interactivity & System Integration
- [x] Step 4.1 — State Management & Master-Detail Render Architecture
- [x] Step 4.2 — Search, Filter & Sort
- [x] Step 4.3 — Overview Dashboard & Bulk Actions
- [x] Step 4.4 — Auto-Scan, Export & Ignore Persistence
- [x] Step 4.5 — Detail Pane Enhancements (ZIPs, Folders, Color versions)
- [x] Step 4.6 — Backend System Integration (Open Folder API & Settings)
- [x] Step 4.7 — Frontend Modularization (ES Modules)
- **Status:** completed

### Phase 5: Polish, Accessibility & Final Verification
- [x] Step 5.1 — Animations & Transitions
- [x] Step 5.2 — Accessibility Audit
- [x] Step 5.3 — Full Integration Test (Passed, Publish skipped per user request)
- **Status:** completed

### Phase 6: Cache-then-Network Architecture
- [x] Step 6.1 — Persistent Disk Cache (Backend)
  - [x] Create Shared Mapper (`ScanResponseMapper.cs`) and verify via xUnit
  - [x] Define Cache Data Model (`ScanCacheRecord.cs`) and verify Native AOT compat
  - [x] Implement Cache Service (`ScanCacheService.cs`) and verify via FakeFileSystem xUnit
  - [x] Add Cache Pipeline Step (`CacheResultsStep.cs`) and verify via xUnit
- [x] Step 6.2 — CLI End-of-Run Flow
  - [x] Expand `EndOfRunChoice` enum
  - [x] Update `InteractivePromptService` prompts
  - [x] Refactor `IIgnoredUpdateWorkflow.RunAsync` return type and verify compilation
- [x] Step 6.3 — CLI Orchestration (CheckModsCommand.cs)
  - [x] Wrap execution in `while (true)` loop and route `Rescan`
  - [x] Implement startup cache loading prompt and `Rehydrate` bypass
  - [x] Implement `LaunchWebGui` DI handoff and verify manually in CLI
- [x] Step 6.4 — Web GUI Cold Start (Cache-then-Network)
  - [x] New Cache Endpoint (`GET /api/cache`) and verify via curl
  - [x] Frontend API (`fetchCache()`)
  - [x] Frontend Bootstrapping & UI indicators (`main.js` & CSS) and verify manually
- **Status:** completed

### Phase 7: QA Audit & Flakiness Resolution (Parallel Subagents A & B)
- [x] Step 7.1 — Subagent A (Flakiness Terminator)
  - [x] Fix WireMock global environment variable leak (`ForgeApiOptions__BaseUrl` vs `ForgeApi__BaseUrl`) in E2E tests.
  - [x] Replace `DateTimeOffset.UtcNow` with `TimeProvider.System` in `ScanCacheServiceTests` and `WebEndpointsTests`.
  - [x] Remove Playwright hardcoded `Timeout = 5000` magic numbers.
- [x] Step 7.2 — Subagent B (API Boundary Enforcer)
  - [x] Refactor Command tests (`CleanCommandTests`, etc.) to remove `System.Reflection` (Implementation Leakage).
  - [x] Fix weak assertions (`Assert.Null(exception)`) in UI renderer tests to validate actual HTML output.
- **Status:** completed

### Phase 8: Closing the Coverage Gaps (Parallel Subagents C & D)
- [x] Step 8.1 — Subagent C (Pipeline Engineer)
  - [x] Write test suites for the 13 untested pipeline steps in `Services/Pipeline/Steps/`.
- [x] Step 8.2 — Subagent D (Network & Error Handling Expert)
  - [x] Write tests for core network clients (`ForgeApiClient`, `ModSearchClient`, `ModUpdateClient`).
  - [x] Implement missing filesystem error paths (`UnauthorizedAccessException` / `IOException`) in `ModScannerServiceTests` and `SptInstallationServiceTests`.
- **Status:** completed

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Add `Rescan` and `LaunchWebGui` options to CLI | Prevent users from closing and reopening terminal to re-run the tool or launch the GUI. |
| Cache-then-Network Web GUI | Show stale data instantly with a timestamp and background scan. Avoids double-scanning on handoff from CLI. |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
|       | 1       |            |
