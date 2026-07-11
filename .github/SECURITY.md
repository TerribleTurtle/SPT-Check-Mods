# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| Latest  | :white_check_mark: |

## Reporting a Vulnerability

If you discover a security vulnerability in CheckModsExtended, please report it responsibly by following these steps:

1. **Do NOT** create a public GitHub issue for security vulnerabilities
2. Send an email to the project maintainers with details about the vulnerability
3. Include the following information:
   - Description of the vulnerability
   - Steps to reproduce the issue
   - Potential impact
   - Suggested fix (if available)

## Security Considerations

CheckModsExtended handles:
- File system access for mod scanning
- Network requests to `forge.sp-tarkov.com` API

## Release Integrity

To ensure the safety and integrity of the binaries distributed via GitHub Releases, the project employs several automated supply chain security measures:
1. **GitHub Artifact Attestations**: Releases are cryptographically signed using GitHub Actions OIDC tokens, providing verifiable build provenance. This guarantees the `.exe` was built by the official CI workflow and has not been maliciously modified.
2. **VirusTotal Scanning**: The CI workflow automatically submits release binaries to VirusTotal. A link to the comprehensive scan report (checking against 70+ antivirus engines) is included in the release notes.
3. **SHA256 Checksums**: Checksums are generated for all artifacts, allowing users to manually verify the downloaded files match the built artifacts exactly.

## Disclosure Policy

We will acknowledge receipt of your vulnerability report within 48 hours and provide regular updates on our progress. Once the vulnerability is resolved, we will coordinate with you on the disclosure timeline.
