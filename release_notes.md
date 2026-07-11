# Check Mods Extended v2.0.0

Welcome to the first release of **Check Mods Extended**. 

First, a sincere thank you to Refringe for the foundational work on the original SPT-Check-Mods. This fork builds directly upon that project, expanding it with new architectural changes, performance improvements, and security features.

### What's Changed

- **Modernized Mod Scanning**
  We've updated how mod data is read. The tool now analyzes mod files statically rather than dynamically loading them into memory, providing an extra layer of security when checking large mod lists.
  
- **Verifiable & Scanned Releases**
  All releases are now automatically scanned by over 70 antivirus engines via VirusTotal and cryptographically signed using GitHub Artifact Attestations (Build Provenance). You can view the scan reports directly at the bottom of these release notes.
  
- **Parallel Update Checks**
  The update checker has been refactored to fetch mod updates concurrently, which reduces the total time required to process large mod lists.
  
- **API Rate Limiting**
  Added automated rate-limiting. When checking hundreds of mods, the tool now paces its API requests to prevent the host servers from blocking the connection.
  
- **Pipeline Architecture**
  The core codebase has been restructured into discrete, testable workflow steps to make it easier to maintain and extend.
  
- **Package-Only Server Mod Parsing**
  Added full support for scanning "package-only" server mods. Pure JavaScript mods (which only contain a `package.json` and no `.dll` file) are now correctly parsed and checked for updates on Linux and Windows.
  
- **Linux & Steam Deck Support**
  Releases continue to provide a pre-compiled Linux binary for running natively on Linux servers, WSL, and the Steam Deck.

**Full Changelog**: https://github.com/TerribleTurtle/CheckModsExtended/compare/v1.2.1...v2.0.0
