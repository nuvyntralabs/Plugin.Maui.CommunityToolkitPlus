# Changelog

## 1.0.0

- First stable release (no preview suffix)
- Reuse one `HttpClient` per Trusted Time source
- Cache the privacy-consent ledger and load it once during SDK activation
- Partition atomic store locks per document
- Complete the iOS Wallet add-pass controller lifecycle
- Degrade or resynchronize stale trusted-time anchors
- Sample covers every module API; unit tests cover DESIGN.md required cases

## 0.1.0-preview.2

- Re-release with corrected NuGet package scope and metadata; no functional changes from preview.1

## 0.1.0-preview.1

- Unofficial CommunityToolkit.Maui companion for .NET MAUI on Android and iOS
- Ordered registration: `UseMauiCommunityToolkit()` then `UseMauiCommunityToolkitPlus`
- Opt-in modules, all disabled by default: Accessibility Audit, State Restoration, Upgrade Guard, Trusted Time, App Integrity, Wallet Passes, Privacy Consent
- Atomic versioned JSON store under `FileSystem.AppDataDirectory/community-toolkit-plus/`
- Accessibility visual-tree rules with JSON and SARIF export
- State checkpoints, contributor registration, schema migration, and expiry
- Journaled upgrade migrations, resume, and startup-loop safe mode
- Trusted time from HTTP(S) sources with outlier rejection and wall-clock jump detection
- Integrity challenge/proof contracts; iOS App Attest adapter; replaceable Android adapter
- Wallet capability mapping; iOS PassKit add; Android Google Wallet save URL
- Purpose-based consent ledger with revocation, expiry, policy renewal, and SDK gates
- Sample Shell app with one page per module and unit tests on `net10.0`
