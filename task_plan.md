# Task Plan: Web UI Redesign & Backend Enrichment

## Goal
Redesign the Check Mods Extended web UI into a tokenized, responsive interface and enrich backend data endpoints to provide a "3-second health check" scanability.

## Current Stage
Phase 9

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

### Phase 9: Documentation Health & Drift Remediation (Parallel Subagents)
- [ ] Step 9.1 — Subagent A (Web UI & Global Docs Fixer)
  - [ ] Fix `README.md` drift (`.bat` file names, bash formatting, document `CheckMods-CLI.sh`).
  - [ ] Add complete JSDoc annotations to all `wwwroot/js/*.js` modules (documenting API return shapes and UI heuristic "magic logic").
- [ ] Step 9.2 — Subagent B (CLI & Orchestration Fixer)
  - [ ] Rewrite `Program.cs` argument parser documentation.
  - [ ] Document process inception (`CheckModsCommand`) and implicit headless side-effects (`CheckModsInterceptor`).
  - [ ] Add missing XML docs (`<summary>`, `<param>`, `<returns>`) to all Command classes.
- [ ] Step 9.3 — Subagent C (Backend Services Fixer)
  - [ ] Fix missing `<param>` tags in `IUpdateOrchestrationService` and remove duplicate summaries in `DependencyGraphBuilder`.
  - [ ] Document reconciliation heuristics in `ModReconciliationService`.
  - [ ] Add `<example>` blocks demonstrating `OneOf` destructuring for all typed API clients (`IForgeApiClient`, `IModSearchClient`, etc.).
- [ ] Step 9.4 — Subagent D (Models & Config Fixer)
  - [ ] Synthesize `appsettings.example.json` from the `Configuration/` mappings.
  - [ ] Fix `<returns>` tags on `SemVer.TryParse`, `ModNameNormalizer`, and `QueryBuilder`.
  - [ ] Explain the download link fallback heuristics in `ModExtensions`.
### Phase 10: Multi-Agent UI/UX Refactoring (Parallel Subagents)
- [x] Step 10.1 — Track 1: Design System & Styling (Design Agent)
  - [x] Add CSS variables for typography (`--text-xs`, `--text-sm`, `--text-lg`) to `base.css`.
  - [x] Add CSS variables for shadows and overlays (`--shadow-md`, `--overlay-bg`) to `base.css`.
  - [x] Introduce missing spacing variables in `base.css`.
  - [x] Replace hardcoded padding (`8px 16px`) and heights with spacing variables in `components.css`.
  - [x] Standardize duplicate/warning badges (`.status-pill`, `.badge`) instead of inline styles in `components.css`.
  - [x] Replace performance-heavy `transition: all 50ms linear;` with optimized transitions in `components.css`.
  - [x] Add utility CSS classes (`.flex`, `.gap-sm`, `.text-right`, `.hidden`) to `layout.css`.
  - [x] **Verification:** Ensure zero inline layout styles (`style="..."`) exist and badges/status dots are identical but powered by utility classes.
- [x] Step 10.2 — Track 2: Responsive & Adaptive Architecture (Layout Agent)
  - [x] Convert the existing desktop-first breakpoints (`max-width: 1024px`, etc.) into a mobile-first architecture (`min-width: 768px`) in `layout.css`.
  - [x] Replace `.detail-pane` `min-width: 350px` with `min(350px, 100vw)` in `layout.css`.
  - [x] Enforce intrinsic standard touch targets (`min-height: 44px`) across the board in `layout.css`.
  - [x] Add `overflow-x: auto;` to `.table-container` in `table.css`.
  - [x] Remove horizontal shifting (`transform: translateX(4px)`) on `.mod-card:hover` in `table.css`.
  - [x] **Verification:** Verify `.table-container` scrolls horizontally at 320px width and `.detail-pane` does not break layout. Verify elements compute to at least 44x44px.
- [x] Step 10.3 — Track 3: Usability & State Management (Usability Agent)
  - [x] Update `renderEmptyState` to use `<td colspan="5">` in `components.js`.
  - [x] Refactor DOM string generation to use semantic component classes in `components.js`.
  - [x] Add `<div id="toast-container" class="toast-container"></div>` to `index.html`.
  - [x] Update button SVG padding configurations to ensure minimum hit areas in `index.html`.
  - [x] Implement vanilla JS `showToast(message, type)` function and plumb API catch blocks/success dispatches in JS files.
  - [x] Implement `.is-loading` states for action buttons (Ignore, Remove) during `await` calls.
  - [x] Immediately show `#ignore-modal` with a loading skeleton before `fetchIgnores()`.
  - [x] **Verification:** Verify Toast system surfaces intentionally triggered API errors. Verify skeleton loader appears on slow networks.
- **Status:** completed

### Phase 11: Comprehensive Audit Remediation (Parallel Fixers)
- [x] Step 11.1 — Track 1: Design System Enforcer
  - [x] Implement missing CSS variables (`--text-xs`, `--text-sm`, `--text-lg`, `--shadow-md`) in `base.css`.
  - [x] Strip all `style="..."` attributes from JS files (`details.js`, `dashboard.js`, `main.js`) and replace with utility classes (`.status-pill`, `.badge`, etc.).
  - [x] Remove hardcoded paddings (e.g., `15px`, `2px 6px`) in `components.css`.
- [x] Step 11.2 — Track 2: State & Accessibility Architect
  - [x] Add strict setter functions to `state.js` and refactor the UI to use them instead of mutating state directly.
  - [x] Fix duplicate `class` attributes in `index.html`.
  - [x] Add `role="status"` and `aria-live="polite"` to the Toast DOM constructor.
  - [x] Add `aria-label` to table checkboxes for screen readers.
- [x] Step 11.3 — Track 3: Playwright Testing Engineer
  - [x] Rewrite E2E tests in `GuiFrontendEndToEndTests.cs` to mock the Cache-then-Network flow and assert the "CACHED" badge.
  - [x] Add assertions to verify Toast visibility during simulated scan/error events.
  - [x] Remove the brittle "double-scan" click mechanism (await auto-scan instead).
- **Status:** completed

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Add `Rescan` and `LaunchWebGui` options to CLI | Prevent users from closing and reopening terminal to re-run the tool or launch the GUI. |
| Cache-then-Network Web GUI | Show stale data instantly with a timestamp and background scan. Avoids double-scanning on handoff from CLI. |
| **Mobile-First CSS Refactor** | Prevent CSS bloat and UI overflow bugs on small viewports by adopting industry standard `min-width` rules. |
| **Vanilla JS Toast System** | Provide transient feedback for API actions/errors cleanly without the visual weight of a persistent console drawer. |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
|       | 1       |            |
