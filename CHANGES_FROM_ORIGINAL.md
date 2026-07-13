# Architectural & Feature Additions

This project is a fork of the original [SPT-Check-Mods](https://github.com/refringe/SPT-Check-Mods) repository by Refringe. The original application established a robust foundation for mod management, including API integration, server/client mod reconciliation, dependency analysis, and update detection.

This fork builds upon that foundation by modifying the internal architecture and introducing several new features. Below is a factual list of the structural and functional changes introduced in this repository.

## Architecture & Build Pipeline

- **.NET Standardization:** The project targets `.NET 9.0` with `LangVersion 13`.
- **Modular Pipeline:** The core scanning logic has been decoupled into discrete modules (`JsonManifestParser`, `PluginMetadataExtractor`, `ServerModExtractor`).
- **Static Security Analysis:** The original `.NET MetadataLoadContext` implementation has been replaced with `Mono.Cecil` (`BinaryParser.cs`). This allows metadata extraction to occur statically without dynamically loading mod DLLs into memory.
- **Embedded Web Server:** The application references `Microsoft.AspNetCore.App` as a framework dependency and embeds the `wwwroot` directory into the single-file executable, enabling it to host a local Kestrel web server without external assets.
- **Automated Zipping:** Custom MSBuild `RoslynCodeTaskFactory` tasks automatically bundle the executable and a Web Manager `.bat` shortcut into a compressed release zip during the publish phase.

## Reliability & Error Handling

- **Error Handling Architecture:** Standard exception-based control flow for API communication has been replaced with Discriminated Unions using the `OneOf` library, establishing strictly typed API errors.
- **Global API Safeguards:** All embedded web endpoints utilize global exception handling to map internal pipeline failures (e.g., `IOException` or network timeouts) to structured HTTP 500 JSON payloads, ensuring the frontend renders clean error toasts instead of crashing.
- **Network Resilience:** The application utilizes `Microsoft.Extensions.Http.Resilience` for managing network retries and circuit-breaking. It handles malformed version numbers explicitly and displays warnings upon network disconnection.
- **Graceful File Locks:** Diagnostic tools and caching services proactively detect and gracefully handle file locks (`UnauthorizedAccessException` or `IOException`) by creating temporary copies or safely skipping operations without interrupting the user experience.
- **Structured Logging:** Standard console logging has been replaced with `Serilog` file sinks, directing diagnostic logs to a persistent file rather than the user interface.
- **Fallback Link Extraction:** If Forge API data is unavailable, the application attempts to extract fallback GitHub repository links directly from local mod configuration files.

## Performance & File Management

- **Concurrent Processing:** Sequential mod fetching has been replaced with background concurrent processing (`ModPartitioner.cs`, `UpdateOrchestrationService.cs`) that aligns with Forge API rate limits.
- **Local Caching System:** A file-backed cache (`ScanCacheService.cs`, `PluginScanCache.cs`) stores previous scan results, eliminating the need to re-scan unmodified files on subsequent startups.
- **File System Abstraction:** File access is managed through a decoupled `IFileSystem` interface.

## User Interface & Commands

- **Web GUI (`gui` mode):** A locally hosted web dashboard available at `localhost:37194` powered by a cache-then-network architecture for immediate loading.
- **Offline CLI Commands:** Built with `Spectre.Console.Cli`, the application supports commands that function without an internet connection:
  - `list`: View installed mods locally.
  - `ignore`: Manage the ignore list manually.
  - `clean`: Clear local caches and overrides.
  - `diag`: Zip and export application logs for debugging.
