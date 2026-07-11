# Check Mods Extended v2.0.0

Welcome to the first official release of **Check Mods Extended**! 

First and foremost, a massive thank you to Refringe for their incredible foundational work on the original SPT-Check-Mods. This fork builds directly upon their excellent vision, expanding it with new features, better performance, and enhanced security.

### What's Changed

- **Modernized Mod Scanning**
  We've updated how mod data is read under the hood. The tool now analyzes mod files statically rather than dynamically loading them into memory, providing an extra layer of security and peace of mind when checking large mod lists.
  
- **Guaranteed Safe Downloads**
  Every release is now automatically scanned by over 70 antivirus engines (via VirusTotal) and cryptographically signed. You can download with 100% confidence that the files are safe and untampered.
  
- **Much Faster Update Checks**
  Checking massive mod lists is now significantly faster because the application fetches updates for multiple mods at the exact same time rather than one by one.
  
- **No More Connection Drops**
  We added smart, automated rate-limiting. If you are checking hundreds of mods, the tool will gracefully pace itself so it doesn't get blocked by the servers for asking too quickly.
  
- **More Reliable Foundations**
  The underlying code has been completely rewritten from the ground up to be far more stable, testable, and easier to update in the future.
  
- **New Name**
  Officially renamed to "Check Mods Extended" to reflect the massive divergence and distinguish it as a major standalone upgrade from the original tool.
  
- **Linux & Steam Deck Support**
  The application is now officially cross-platform! Releases include a native Linux version so you can easily run it on Linux servers, WSL, or your Steam Deck.

Enjoy the update!

**Full Changelog**: https://github.com/TerribleTurtle/CheckModsExtended/compare/v1.2.1...v2.0.0
