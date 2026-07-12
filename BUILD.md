# Building CheckModsExtended

This document outlines how to build, test, and publish the CheckModsExtended project.

## Prerequisites
- **.NET 9.0 SDK** or later
- Windows OS (the project currently targets `win-x64`)

## Restore & Build

To restore dependencies and build the entire solution in debug mode:

```bash
dotnet restore CheckModsExtended.slnx
dotnet build CheckModsExtended.slnx
```

## Running Tests

We use xUnit for unit and integration testing. Mocking frameworks are intentionally avoided in favor of hand-crafted fakes (e.g. `FakeUpdateCheckService`).

To run the test suite:

```bash
dotnet test CheckModsExtended.slnx
```

## Publishing (Release & Native AOT)

CheckModsExtended uses **.NET Native AOT compilation** to produce a single, self-contained, highly optimized executable with zero external dependencies (no .NET runtime required by the end user).

To publish a release build for Windows x64:

```bash
dotnet publish CheckModsExtended.csproj -c Release -r win-x64 -o ./publish/win-x64
```

### Build Artifacts
After running the publish command, the `./publish/win-x64/` directory will contain:
- `CheckModsExtended-win-x64.exe` (The main Native AOT executable)
- `CheckModsExtended - Start Web Manager.bat` (Helper script to launch the Web UI mode)
- `CheckModsExtended-win-x64.zip` (A fully packaged archive ready for distribution, automatically created via an MSBuild task in the `.csproj`)

*Note: Never distribute `.exe` files built in `Debug` mode or without the `-r win-x64` flag, as they will require the .NET runtime.*
