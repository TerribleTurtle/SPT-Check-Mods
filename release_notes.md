# 🚀 Welcome to Check Mods Extended v2.0.0!

This is our biggest update ever! We have completely torn down the old monolithic architecture and rebuilt it from the ground up for **extreme speed, absolute security, and future-proof stability**. If you thought it was fast before, wait until you try this version! ⚡

### What's Changed

- **Massive Security Enhancements (RCE Vulnerability Patched) 🛡️**
  We completely removed the dangerous `AssemblyLoadContext` execution vector. We now use Static IL Bytecode Analysis to safely extract metadata from third-party mods without *ever* executing their code!
- **Cryptographic Trust & Validity 🔒**
  This release introduces GitHub Artifact Attestations (Build Provenance), SHA256 Checksums, and automated VirusTotal scanning. You can download with 100% confidence knowing your `.exe` was legitimately built by our CI and verified by 70+ antivirus engines.
- **Blazing Fast Performance 🏎️**
  We ripped out the old bounded concurrency pattern and implemented true unbounded `Parallel.ForEachAsync` processing to make checking mods incredibly fast.
- **Resilient Rate Limiting 🚦**
  Powered by native `.NET 9` Resilience and Polly `TokenBucketRateLimiter`, the app now automatically paces requests and gracefully handles API rate limits without dropping connections or timing out.
- **The Great Architecture Rewrite 🏗️**
  The massive central monolith has been split into 13 discrete, highly-testable workflow steps for maximum maintainability.
- **Strict Error Handling 🛑**
  We introduced strict immutability and error boundaries so the app handles network errors gracefully and loudly instead of silently crashing.
- **Renamed to CheckModsExtended ✨**
  To reflect the massive divergence, safety improvements, and speed upgrades from the original fork.

Enjoy the fastest and safest mod-checking experience yet! 💙🐢

**Full Changelog**: https://github.com/TerribleTurtle/CheckModsExtended/compare/v1.2.1...v2.0.0
