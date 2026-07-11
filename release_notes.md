# Check Mods Extended v2.0.0

Welcome to the first official release of **Check Mods Extended**! 

First and foremost, a massive thank you to Refringe for their incredible foundational work on the original SPT-Check-Mods. This fork builds directly upon their excellent vision, expanding it with new architectural changes and security features.

### What's Changed

- **Security & Integrity Enhancements**
  We've transitioned the mod scanning engine from dynamic loading (`AssemblyLoadContext`) to Static IL Bytecode Analysis (`Mono.Cecil`). This ensures third-party mod metadata is read safely without executing any code.
- **Verifiable Builds**
  All releases are now cryptographically signed via GitHub Artifact Attestations (Build Provenance), hashed with SHA256, and automatically scanned by VirusTotal to guarantee download safety.
- **Performance Adjustments**
  The update checker has been updated to use `Parallel.ForEachAsync` to speed up dependency fetching for large mod lists.
- **API Rate Limiting**
  Added native `.NET 9` Resilience and Polly `TokenBucketRateLimiter` to gracefully handle API rate limits and keep connections stable.
- **Pipeline Architecture**
  The core mod-checking logic has been restructured into discrete, testable workflow steps to make the codebase easier to maintain and extend over time.
- **Project Rename**
  Officially renamed to Check Mods Extended to reflect the massive divergence and distinguish it as a standalone, independent fork.
- **Official Linux Support**
  The application is now officially cross-platform! Releases include a pre-compiled, trimmed Linux binary (`linux-x64`) for running natively on Linux servers, WSL, and Steam Deck. Both Windows and Linux builds have feature parity and will run flawlessly.

Enjoy the update!

**Full Changelog**: https://github.com/TerribleTurtle/CheckModsExtended/compare/v1.2.1...v2.0.0
