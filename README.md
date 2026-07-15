# SPT Check Mods Extended

A .NET 9 console application that validates Single Player Tarkov (SPT) mod compatibility using the Forge API.

<img width="1434" height="870" alt="image" src="https://github.com/user-attachments/assets/d671c4c6-6b92-4dff-ad0f-615f6f87cee2" />
<img width="1013" height="314" alt="image" src="https://github.com/user-attachments/assets/00878387-024c-4961-b66f-b977f4e550c0" />

## Acknowledgments

This project is a fork of the original [SPT Check Mods](https://github.com/refringe/SPT-Check-Mods) by Refringe. We are incredibly grateful for their foundational work and continued contributions to the SPT modding community. 

For a detailed list of architectural changes and features introduced in this fork, please read [Architectural & Feature Additions](CHANGES_FROM_ORIGINAL.md).

## Features

- **Forge API Integration**: Verifies mods against the official SPT Forge database
- **Version Compatibility**: Checks installed mod versions against SPT version requirements
- **Update Detection**: Identifies mods with available updates and provides download links
- **Dependency Analysis**: Builds dependency trees, identifies missing dependencies, and detects conflicts
- **Installation Checks**: Detects mods installed in the wrong folder and excludes them from the rest of the run
- **Dismissable Update Prompts**: Lets you ignore false-positive "update available" prompts for mods whose files are already current, with an optional shared community list
- **SPT & Self-Update Checking**: Notifies you when new SPT versions or Check Mods app versions are available
- **Web Manager GUI**: A locally hosted web dashboard that caches previously scanned mods for local loading.
- **Offline CLI Commands**: Terminal commands allowing you to view and manage your mod list without an internet connection.
- **Security Analysis**: Statically extracts metadata without executing any underlying mod code.
- **Parallel Scanning**: Checks mods concurrently while safely respecting Forge API network limits.
- **Fallback Links**: Attempts to extract fallback GitHub links directly from local files if a mod is missing from the Forge API.

## Requirements

- A valid SPT 4.0+ installation
- .NET 9.0 SDK

## Installation

### Option 1: Download Release
Download the latest release for your operating system (`CheckModsExtended-win-x64.exe` or `CheckModsExtended-linux-x64`) from the [Releases](https://github.com/TerribleTurtle/CheckModsExtended/releases) page.

- **Windows**: Move the `.exe` into the root of your SPT installation directory.
- **Linux**: Download `CheckModsExtended-linux-x64`, move it into your SPT directory, and make it executable: `chmod +x CheckModsExtended-linux-x64`.

### Option 2: Build from Source
```bash
git clone https://github.com/TerribleTurtle/CheckModsExtended.git
cd CheckModsExtended
dotnet build
```

## Usage

### Web Manager GUI (Default)

The **Web Manager GUI** is the default execution mode. Simply **double-click** the executable. This will automatically open the beautiful dashboard in your default browser on `http://localhost:37194`.

```bash
# Windows
CheckModsExtended-win-x64.exe

# Linux
./CheckModsExtended-linux-x64
```

### Command Line Interface (CLI)

The executable acts as a seamless hybrid. If you pass **any** arguments (or explicitly pass `cli`), it will run in terminal mode without launching the web server.

To run the default update check in the current directory:
```bash
CheckModsExtended-win-x64.exe cli
```

You can also point it at an SPT installation elsewhere by passing the path:
```bash
CheckModsExtended-win-x64.exe "/path/to/spt"
```
```bash
./CheckModsExtended-linux-x64 "/path/to/spt"
```

If you built from source, use `dotnet run` instead. You must run this command from the `CheckModsExtended` source directory where the `.csproj` is located. The `--` passes the path through to the application rather than to the .NET CLI:
```bash
# Run the CLI and point to your SPT installation directory
dotnet run -- /path/to/spt
```

To run non-interactively (e.g. for CI or scripts), use `--no-prompt`:
```bash
CheckModsExtended-win-x64.exe --no-prompt
```

### CLI Commands

The application supports additional commands for specific tasks:

- **List Local Mods**: Prints a table of locally installed client and server mods without checking the internet for updates.
  ```bash
  CheckModsExtended-win-x64.exe list [SptPath]
  ```
  Options:
  - `-t`, `--type <TYPE>`: Filter by type (e.g., server, client)
  - `-s`, `--status <STATUS>`: Filter by status
  - `--sort <SORT>`: Sort by field (e.g., name, author, version)
  - `-l`, `--limit <LIMIT>`: Limit the number of results
  - `--search <SEARCH>`: Search by text

- **Ignore List Management**: View or manage mods you don't want to receive update notifications for.
  ```bash
    CheckModsExtended-win-x64.exe ignore list
  ```
  Options:
  - `-t`, `--type <TYPE>`: Filter by type (e.g., server, client)
  - `-s`, `--status <STATUS>`: Filter by status
  - `--sort <SORT>`: Sort by field (e.g., name, author, version)
  - `-l`, `--limit <LIMIT>`: Limit the number of results
  - `--search <SEARCH>`: Search by text

  ```bash
    CheckModsExtended-win-x64.exe ignore add <ApiModId> <LocalVersion> <LatestVersion>
  ```
  ```bash
    CheckModsExtended-win-x64.exe ignore remove <ApiModId>
  ```

- **Clean Up**: Manage local app data (clears configuration overrides, ignored updates, and logs).
  ```bash
  CheckModsExtended-win-x64.exe clean
  ```

- **Diagnostics**: Zip and export the application's log files from AppData for sharing.
  ```bash
  CheckModsExtended-win-x64.exe diag
  ```

- **License**: Display the application license.
  ```bash
  CheckModsExtended-win-x64.exe license
  ```

**Global Options:**
- `-v` or `--verbose`: Enables verbose logging output.
- `-d` or `--debug`: Enables debug logging output (includes stack traces).
- `-f` or `--format <TYPE>`: Sets output format (e.g., `table`, `json`, `csv`).
- `-y` or `--no-prompt`: Runs headless without interactive prompts.

## Troubleshooting

You can override application settings using environment variables. This is particularly useful for debugging or circumventing network issues.

**Enable Debug Logging:**
```bash
# Windows CLI
set LoggingOptions__MinimumLogLevel=Debug
CheckModsExtended-win-x64.exe

# Windows PowerShell
$env:LoggingOptions__MinimumLogLevel="Debug"
.\CheckModsExtended-win-x64.exe

# Linux CLI with custom log level
LoggingOptions__MinimumLogLevel=Debug ./CheckModsExtended-linux-x64
```


## Configuration

### Local Storage
Check Mods keeps its data under `%APPDATA%\SptCheckModsExtended` (Windows) or `~/.config/SptCheckModsExtended` (Linux):

- **Logs**: `logs\checkmod.log` (within the app data directory)
- **Ignored updates**: `ignored-updates.json` (within the app data directory)

> **Note on Telemetry**: When the community shared ignore list is enabled, Check Mods makes a remote HTTP GET request to `https://forge-static.sp-tarkov.com/check-mods/ignored-updates.json` to fetch the latest suppressed updates. You can opt-out by setting the environment variable `IgnoredUpdateOptions__RemoteUrl=""`.

### Supported Mod Formats
- **Server Mods**: SPT mods with `AbstractModMetadata` in `SPT/user/mods`
- **Client Mods**: BepInEx plugins with `BepInPlugin` attribute in `BepInEx/plugins`

## Issues & Feedback

Bug reports, issues, and suggestions are always welcome via the GitHub Issue tracker! 

However, please note that this is primarily a personal fork. While I appreciate all feedback and community input, I do not maintain a highly active community presence and may not be highly responsive to pull requests or feature ideas. Feel free to use and modify the code as you see fit!

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
