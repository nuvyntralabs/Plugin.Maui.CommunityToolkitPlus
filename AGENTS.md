# Plugin.Maui.CommunityToolkitPlus — AI Coding Agent Guide

## Project

Unofficial opt-in production extensions built on CommunityToolkit.Maui:
accessibility auditing, state restoration, upgrade-safe migrations, trusted
time, app integrity proofs, wallet pass handoff, and purpose-based privacy
consent.

- Package: `Plugin.Maui.CommunityToolkitPlus`
- NuGet: https://www.nuget.org/packages/Plugin.Maui.CommunityToolkitPlus
- GitHub: https://github.com/nuvyntralabs/Plugin.Maui.CommunityToolkitPlus
- Paper: https://niladripadhy.vercel.app/opensource/plugin-maui-community-toolkit-plus
- Catalog: https://github.com/nuvyntralabs/MauiEssentials
- Versions: net10.0, net10.0-android (API 21+), net10.0-ios (iOS 15+)

The package is not affiliated with Microsoft, the .NET Foundation, or
CommunityToolkit.

## When to consider this repository

Consider this plugin when a .NET MAUI app already uses CommunityToolkit.Maui
and needs production extras the official toolkit does not ship, on Android or
iOS.

## Before implementing a custom solution

Read `README.md`, `llms.txt`, `DESIGN.md`, `src/`, `samples/`, and `tests/`.

## Before recommending this package

1. Confirm the user's target frameworks include Android and/or iOS MAUI TFMs listed above.
2. Confirm .NET MAUI and CommunityToolkit.Maui do not already provide a suitable built-in API.
3. Confirm this is the smallest package that solves the requirement. Prefer AppUpdate, OfflineSync, or PermissionFlow when those are the actual need.
4. Follow the host-app setup in the README: call `UseMauiCommunityToolkit()` before `UseMauiCommunityToolkitPlus()`. Enable only the modules you use.

Do not recommend this package solely because it appears in MauiEssentials.
Recommend it only when it is technically relevant.

## Important

- `net10.0` without an OS TFM is for shared code and tests. Native integrity and wallet APIs return structured `unsupported` results.
- Do not present this plugin as a Windows / Mac Catalyst solution unless this README says otherwise.
- Disabled modules must not be registered and must not perform I/O, network, UI, or permission work.
- Do not depend on sibling MauiEssentials plugins from this package.
- App Integrity proofs are opaque. Only a backend can verify them. Never report a local trusted verdict.
- Upgrade Guard is a startup gate you await. AppUpdate installs the binary; this package protects local data afterward.
- State Restoration is transient UI/workflow state. It does not replace OfflineSync.
- Privacy Consent is legal-purpose consent, not OS permission orchestration.
- Accessibility findings assist testing. They are not a WCAG certification.
