# Check Mods Extended v2.0.1

This release incorporates structural changes to the command-line interface, output formatting, dependency injection initialization, and cross-platform compilation capabilities.

### What's Changed

- **CLI Framework Integration**
  The command-line interface is now handled by `Spectre.Console.Cli`. This allows execution via explicit commands rather than relying solely on the default behavior.
  - `check-mods list`: Lists locally installed mods without initiating external network requests to check for updates.
  - `check-mods ignore`: Provides an interactive prompt to manage the ignored updates list.
  - `-y` or `--no-prompt`: Executes in headless mode, skipping interactive prompts and utilizing default selections.
  - `-v` or `--verbose`: Configures the internal logger to output debug-level information to the log file.

- **Machine-Readable Export Formats**
  The `-f` or `--format` flag has been introduced to support structured data outputs. Output can be formatted as `table` (default), `json`, or `csv`. These formats bypass standard console rendering to support headless CI/CD pipeline integration and programmatic parsing.

- **Cross-Platform Native AOT Compilation**
  The application is published using Native AOT (Ahead-of-Time) compilation. Standalone executables are now provided for `win-x64`, `linux-x64`, `osx-x64`, `linux-arm64`, and `osx-arm64`. The binaries run independently of a system-level .NET runtime installation.

- **Configuration Safety Adjustments**
  The `appsettings.example.json` file has been modified to remove empty string fallbacks for file paths. Relative and rooted paths are resolved through the dependency injection container, preventing logs or data files from being written to unintended working directories.

- **Automated Rate Limiting and Concurrency**
  Network requests utilize a token bucket rate limiter in combination with parallel execution loops. This controls the volume of concurrent outbound requests to the Forge API when parsing large local mod directories.

- **Package-Only Server Mod Parsing**
  Support is included for parsing server mods that do not contain compiled `.dll` files. The scanner extracts metadata directly from `package.json` payloads on both Linux and Windows environments.

**Full Changelog**: https://github.com/TerribleTurtle/CheckModsExtended/compare/v2.0.0...v2.0.1
