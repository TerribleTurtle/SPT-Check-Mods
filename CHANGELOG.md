# Changelog

All notable changes to this project will be documented in this file.

## [2.0.1] - 2026-07-11

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
