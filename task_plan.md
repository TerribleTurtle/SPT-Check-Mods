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
- [ ] Step 6.1 — Persistent Disk Cache (Backend)
- [ ] Step 6.2 — CLI End-of-Run Flow
- [ ] Step 6.3 — CLI Orchestration (CheckModsCommand.cs)
- [ ] Step 6.4 — Web GUI Cold Start (Cache-then-Network)
- **Status:** pending

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Add `Rescan` and `LaunchWebGui` options to CLI | Prevent users from closing and reopening terminal to re-run the tool or launch the GUI. |
| Cache-then-Network Web GUI | Show stale data instantly with a timestamp and background scan. Avoids double-scanning on handoff from CLI. |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
|       | 1       |            |
