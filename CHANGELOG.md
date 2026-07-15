# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Changed
- **Web UI Detail Pane**: Overhauled the mod detail slider (right-side pane) with a premium aesthetic, including deeper shadows, gradient typography, modernized alert boxes, and elevated dependency cards.

## [2.0.4] - 2026-07-13

### Added
- **Instant Cache-then-Network Loading**: Added a persistent disk cache that stores scan results locally. The Web GUI and CLI now load cached data instantly on startup while seamlessly triggering a fresh scan in the background, eliminating long cold-start load times.
- **End-of-Run Routing**: The CLI now prompts you after a scan finishes, allowing you to instantly launch the Web GUI or trigger a rescan without having to close and reopen the application.
- **Strict Error Handling**: Enforced strict API response destructuring and specific exception filters across the new Web Endpoints and GitHub Release client to prevent exceptions from being swallowed silently.

- **Settings Modal**: Added a dedicated settings modal to the Web GUI, allowing users to configure application settings seamlessly.
- **Robust IO Operations**: The settings persistence layer was rewritten to use atomic file swaps, preventing file corruption if the application exits unexpectedly during a write.
- **Codebase Health Check**: A swarm of audit agents successfully swept the codebase to enforce `public sealed` composition, explicit typing, and strict exception filtering per the `.editorconfig` standards.

### Changed
- **Web UI Polish & Accessibility**: Refactored the Web GUI using a mobile-first responsive architecture. Standardized CSS styling, added non-intrusive Toast notifications for background actions, and improved screen reader accessibility.

### Fixed
- **Web UI Cache Invalidation**: Fixed an issue where clicking the "Scan Local Mods" button in the Web UI would instantly refresh using stale data. The backend memory cache is now properly cleared when a manual scan is triggered.
- **False Positive Drift Detection**: Fixed an issue where the Web GUI's "Drift Detection" would show false positives. The local scan now properly reconciles matched mod APIs so the hash properties (LocalName and LocalVersion) are symmetrically verified between cache and local disk.
- **UI Sorting**: Assured the default UI sort places "Status" (needs updating) on top, with a fallback to alphabetical name sort.
- **Cache Scoping**: Fixed an issue where the global mod cache would prompt you to load results from a different SPT installation when running the CLI. The cache is now strictly bound to the specific SPT installation path it was scanned from.
- **Immediate Exit**: Fixed an annoyance where selecting "Close Check Mods" at the end of a CLI run would unnecessarily prompt you to "Press any key to exit..." before actually closing.

## [2.0.3] - 2026-07-12

### Added
- **Web Manager GUI**: Added a comprehensive, fully-featured browser-based Graphical User Interface for managing mods. 
  - To launch, run `Start Web Manager.bat` on Windows, or execute `CheckModsExtended.exe gui` from the terminal.
  - Features a Tarkov-themed brutalist aesthetic built with modern HTML/CSS/JS.
  - Safely served over a dynamically bound local Kestrel web server.
  - Allows easy ignoring of false-positive updates with a single click.
  - Includes integrated links to download mod ZIP files and view mod pages directly.

### Changed
- **CLI Default Execution**: Executing the binary without arguments now defaults to the traditional Command Line Interface (`cli`), preserving the standard workflow for power users.
- **Removed Helper Scripts**: The `CheckMods-CLI.bat` and `CheckMods-CLI.sh` wrapper scripts no longer pass the `cli` argument since it is now the default behavior.

## [2.0.2] - 2026-07-11

### Added
- **SPT Hub Packaging**: The CI pipeline now automatically packages an `SptHub.zip` containing the Windows executable, README, LICENSE, and CHANGELOG, ready for direct upload to the SPT Hub.
- **Custom Icon**: Handcrafted a new SVG/ICO icon for the executable (a dark tactical shield with a green checkmark).

### Fixed
- **Metadata & Licensing**: Added `TerribleTurtle` alongside `Refringe` to the assembly metadata and copyright headers to correctly attribute the fork.

---

## [2.0.0] - 2026-07-11

This release incorporates structural changes to the command-line interface, output formatting, dependency injection initialization, and cross-platform compilation capabilities.

### Added
- **CLI Framework Integration**: The command-line interface is now handled by `Spectre.Console.Cli`. This allows execution via explicit commands rather than relying solely on the default behavior.
  - `check-mods list`: Lists locally installed mods without initiating external network requests to check for updates.
  - `check-mods ignore`: Provides an interactive prompt to manage the ignored updates list.
  - `-y` or `--no-prompt`: Executes in headless mode, skipping interactive prompts and utilizing default selections.
  - `-v` or `--verbose`: Configures the internal logger to output debug-level information to the log file.
- **Machine-Readable Export Formats**: The `-f` or `--format` flag has been introduced to support structured data outputs. Output can be formatted as `table` (default), `json`, or `csv`. These formats bypass standard console rendering to support headless CI/CD pipeline integration and programmatic parsing.
- **Cross-Platform Native AOT Compilation**: The application is published using Native AOT (Ahead-of-Time) compilation. Standalone executables are now provided for `win-x64`, `linux-x64`, `osx-x64`, `linux-arm64`, and `osx-arm64`. The binaries run independently of a system-level .NET runtime installation.
- **Package-Only Server Mod Parsing**: Support is included for parsing server mods that do not contain compiled `.dll` files. The scanner extracts metadata directly from `package.json` payloads on both Linux and Windows environments.

### Fixed
- **Configuration Safety Adjustments**: The `appsettings.example.json` file has been modified to remove empty string fallbacks for file paths. Relative and rooted paths are resolved through the dependency injection container, preventing logs or data files from being written to unintended working directories.

### Changed
- **Automated Rate Limiting and Concurrency**: Network requests utilize a token bucket rate limiter in combination with parallel execution loops. This controls the volume of concurrent outbound requests to the Forge API when parsing large local mod directories.

---

## [2.0.0] - 2026-07-09

Welcome to the first release of **Check Mods Extended**. 

First, a sincere thank you to Refringe for the foundational work on the original SPT-Check-Mods. This fork builds directly upon that project, expanding it with new architectural changes, performance improvements, and security features.

### Added
- **Linux & Steam Deck Support**: Releases continue to provide a pre-compiled Linux binary for running natively on Linux servers, WSL, and the Steam Deck.
- **Verifiable & Scanned Releases**: All releases are now automatically scanned by over 70 antivirus engines via VirusTotal and cryptographically signed using GitHub Artifact Attestations (Build Provenance). You can view the scan reports directly at the bottom of these release notes.

### Changed
- **Modernized Mod Scanning**: We've updated how mod data is read. The tool now analyzes mod files statically rather than dynamically loading them into memory, providing an extra layer of security when checking large mod lists.
- **Parallel Update Checks**: The update checker has been refactored to fetch mod updates concurrently, which reduces the total time required to process large mod lists.
- **API Rate Limiting**: Added automated rate-limiting. When checking hundreds of mods, the tool now paces its API requests to prevent the host servers from blocking the connection.
- **Pipeline Architecture**: The core codebase has been restructured into discrete, testable workflow steps to make it easier to maintain and extend.
