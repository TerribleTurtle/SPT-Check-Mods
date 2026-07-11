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
- ✨ **Improved Security**: Mod files are now analyzed safely without executing their underlying code, protecting your system from potential security vulnerabilities.
- ✨ **Faster Scans**: The app checks your mods in parallel while safely staying within the Forge API's network limits, making scans noticeably faster for large mod lists.
- ✨ **Clearer Error Messages**: When the network drops or a mod has an invalid version number, the app displays an explicit warning explaining the exact issue.
- ✨ **New Offline Commands**: You can now use terminal commands like `check-mods list` and `check-mods ignore` to instantly manage your local mods without needing an internet connection.
- ✨ **Safer File Handling**: Improved how the app reads and writes to your configuration folders to ensure it works flawlessly across different hard drives and operating systems.
- ✨ **More Reliable Links**: If a mod is missing from the Forge API, the app will now try to extract a fallback GitHub link directly from the mod's local files so you can still find its page.

## Requirements

- A valid SPT 4.0+ installation

## Installation

### Option 1: Download Release
Download the latest release for your operating system (`CheckModsExtended-win-x64.exe` or `CheckModsExtended-linux-x64`) from the [Releases](https://github.com/TerribleTurtle/CheckModsExtended/releases) page.

- **Windows**: Move the `.exe` into the root of your SPT installation directory.
- **Linux**: Move the binary into your SPT directory and make it executable: `chmod +x CheckModsExtended-linux-x64`.

Running the application from the root directory will automatically check the mods in that installation.

### Option 2: Build from Source
```bash
git clone https://github.com/TerribleTurtle/CheckModsExtended.git
cd CheckModsExtended
dotnet build
```

## Usage

### Basic Usage

If you downloaded the release executable and placed it in your SPT installation directory, run it from there:

```bash
# Windows
CheckModsExtended-win-x64.exe

# Linux
./CheckModsExtended-linux-x64
```

It checks the mods in the current directory. You can also point it at an SPT installation elsewhere by passing the path:

```bash
# Windows
CheckModsExtended-win-x64.exe "C:\path\to\spt"

# Linux
./CheckModsExtended-linux-x64 "/path/to/spt"
```

If you built from source, use `dotnet run` instead. The `--` passes the path through to the application rather than to the .NET CLI:

```bash
# Run from your SPT installation directory
dotnet run

# Or specify the SPT path as an argument
dotnet run -- /path/to/spt
```

### Headless Mode

You can run the tool in headless mode using the `-y` or `--no-prompt` flags. This will bypass all interactive prompts (like the end-of-run menu or pause before exit), making it ideal for automated scripting or CI environments:

```bash
CheckModsExtended-win-x64.exe --no-prompt
```

### CLI Commands

The application supports additional commands for specific tasks:

- **List Local Mods**: Instantly prints a table of locally installed client and server mods without checking the internet for updates.
  ```bash
  CheckModsExtended-win-x64.exe list [SptPath]
  ```

- **Ignore Updates**: Manually manage the list of ignored updates to suppress false-positive "update available" notifications.
  - **List ignored updates**: Prints all currently ignored updates.
    ```bash
    CheckModsExtended-win-x64.exe ignore list
    ```
  - **Add ignored update**: Manually ignores an update.
    ```bash
    CheckModsExtended-win-x64.exe ignore add <ApiModId> <LocalVersion> <LatestVersion>
    ```
  - **Remove ignored update**: Removes an ignored update.
    ```bash
    CheckModsExtended-win-x64.exe ignore remove <ApiModId>
    ```

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

# Linux Bash
LoggingOptions__MinimumLogLevel=Debug ./CheckModsExtended-linux-x64
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
