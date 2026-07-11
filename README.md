# SPT Check Mods Extended

A .NET 9 console application (utilizing .NET 10.0 preview extensions) that validates Single Player Tarkov (SPT) mod compatibility using the Forge API.

<img width="1013" height="314" alt="image" src="https://github.com/user-attachments/assets/00878387-024c-4961-b66f-b977f4e550c0" />

## Acknowledgments

This project is a fork of the original [SPT Check Mods](https://github.com/refringe/SPT-Check-Mods) by Refringe. We are incredibly grateful for their foundational work and continued contributions to the SPT modding community.

## Features

> ✨ Indicates a feature added or improved in this fork.

- **Forge API Integration**: Verifies mods against the official SPT Forge database
- **Version Compatibility**: Checks installed mod versions against SPT version requirements
- **Update Detection**: Identifies mods with available updates and provides download links
- **Bulk Update Pages**: Opens every out-of-date mod's Forge page in your browser from the end-of-run menu
- **Dependency Analysis**: Builds dependency trees, identifies missing dependencies, and detects conflicts
- **Dependency Change Notices**: When an update is available, shows the dependencies it adds or removes, flagging ones you'll need to download or update
- **Installation Checks**: Detects mods installed in the wrong folder and excludes them from the rest of the run
- **Dismissable Update Prompts**: Lets you ignore false-positive "update available" prompts for mods whose files are already current, with an optional shared community list
- **SPT Update Checking**: Notifies you when a new SPT version is available
- **Self-Update Checking**: Notifies you when a newer version of Check Mods is available
- ✨ **Pipeline Architecture Rewrite**: Tearing down the monolith into 13 decoupled `IWorkflowStep` classes
- ✨ **High-Performance Concurrency**: Using `Parallel.ForEachAsync` instead of bounded `Task.WhenAll` to drastically speed up dependency fetching and protect against rate limits
- ✨ **Trimming-Safe UI**: Using `WindowsConsoleHelper` to force VT mode, preserving Spectre.Console colors even in heavily trimmed binaries
- ✨ **Robust Error Handling**: Implemented `OneOf` error boundaries to explicitly catch and surface API network failures instead of relying on silent exceptions
- ✨ **Enhanced Data Models**: Restructured the domain models to explicitly separate remote API states from local filesystem states
- ✨ **Strict Semantic Versioning**: Improved version parsing to explicitly capture and surface Mod Load Warnings, helping users understand exactly why a mod failed a version check
- ✨ **Standardized Logging**: Transitioned to Serilog for the internal logging engine, providing robust, high-concurrency log rotation

## Requirements

- A valid SPT 4.0+ installation

## Installation

### Option 1: Download Release
Download the latest release (`CheckModsExtended-win-x64.exe`) from the [Releases](https://github.com/TerribleTurtle/CheckModsExtended/releases) page, then move it into the root of your SPT installation directory. Running it from there checks the mods in that installation.

### Option 2: Build from Source
```bash
git clone https://github.com/TerribleTurtle/CheckModsExtended.git
cd CheckModsExtended
dotnet build
```

## Usage

If you downloaded the release executable and placed it in your SPT installation directory, run it from there:

```bash
CheckModsExtended-win-x64.exe
```

It checks the mods in the current directory. You can also point it at an SPT installation elsewhere by passing the path:

```bash
CheckModsExtended-win-x64.exe "C:\path\to\spt"
```

If you built from source, use `dotnet run` instead. The `--` passes the path through to the application rather than to the .NET CLI:

```bash
# Run from your SPT installation directory
dotnet run

# Or specify the SPT path as an argument
dotnet run -- /path/to/spt
```

## Troubleshooting

You can override application settings using environment variables. This is particularly useful for debugging or circumventing network issues.

**Enable Debug Logging:**
```bash
# Windows CLI
set LoggingOptions__MinimumLogLevel=Debug
CheckModsExtended-win-x64.exe

# PowerShell
$env:LoggingOptions__MinimumLogLevel="Debug"
.\CheckModsExtended-win-x64.exe
```


## Configuration

### Local Storage
Check Mods keeps its data under `%APPDATA%\SptCheckModsExtended`:

- **Logs**: `%APPDATA%\SptCheckModsExtended\logs\checkmod.log`
- **Ignored updates**: `%APPDATA%\SptCheckModsExtended\ignored-updates.json`

> **Note on Telemetry**: When the community shared ignore list is enabled, Check Mods makes a remote HTTP GET request to `https://forge-static.sp-tarkov.com/check-mods/ignored-updates.json` to fetch the latest suppressed updates.

### Supported Mod Formats
- **Server Mods**: SPT mods with `AbstractModMetadata` in `SPT/user/mods`
- **Client Mods**: BepInEx plugins with `BepInPlugin` attribute in `BepInEx/plugins`

## Contributing

Please read [CONTRIBUTING.md](.github/CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## Security

**Release Integrity**  
All binary releases attached to this repository are secured through automated CI checks to guarantee they have not been tampered with:
- **Build Provenance**: Every release includes a GitHub Artifact Attestation that cryptographically signs the binary, verifying it was built directly from this repository's verified actions.
- **VirusTotal Scanning**: Every release executable is automatically uploaded to VirusTotal and scanned by 70+ antivirus engines. A link to the clean scan report is automatically appended to the release notes.
- **Checksums**: SHA256 checksums are generated and provided alongside the releases for manual verification.

For general security concerns, please review our [Security Policy](.github/SECURITY.md).

For details regarding the historical `AssemblyLoadContext` vulnerability and our static analysis patch, please read our [Security Advisory](SECURITY_ADVISORY.md).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
