# Multi-Agent Audit Remediation Plan

## Phase 1: Backend Architecture & DI Polish
- [x] Resolve DI conflicts in `CacheManager.cs` and `ServiceCollectionExtensions.cs`.
- [x] Remove manual DI registrations for `ScanCacheService`, `IgnoredUpdateStore`, and `PluginScanCache`.
- [x] Modernize `SettingsService.cs` and `CacheManager.cs` to use C# 12 Primary Constructors.

## Phase 2: Security, I/O & API Fortification
- [x] Refactor `ForgeApiClient.cs` to use `JsonSerializer.DeserializeAsync()`.
- [x] Remove raw JSON logging in `ForgeApiClient.cs` to prevent OutOfMemory issues on huge payloads.
- [x] Fix the double-dispose issue in `BinaryParser.cs` by handling stream lifetimes explicitly without nested usings.
- [x] Secure `sptVersion` interpolation in `ModSearchClient.cs` using `Uri.EscapeDataString`.
- [x] Remove the redundant exception catch/log in `ModScannerService.cs`.

## Phase 3: QA & Test Suite Expansion
- [x] Add strict value assertions to `SemVerTests.cs`.
- [x] Add `ModReconciliationServiceTests.cs` using hand-crafted fakes (no Moq).
- [x] Add `SptVersionConstraint` filtering tests to `UpdateCheckServiceTests.cs`.
- [x] Add sorting identical version comparison tests.
- [x] Add null-handling validation tests for `ModUpdatesData`.

## Phase 4: Frontend UI & Performance Refactor
- [~] Decompose `main.js` monolithic state (SKIPPED: Not worth the risk of rewriting the entire DOM template since performance issues were solved).
- [x] Optimize `filteredMods` getter to avoid O(N log N) blocking on every render tick.
- [x] Optimize `.progress-bar-fill` CSS animation to use GPU-accelerated `transform: scaleX()`.
- [x] Remove declarative violations (`setTimeout`, `scrollIntoView`, `appendChild`) from JS.
