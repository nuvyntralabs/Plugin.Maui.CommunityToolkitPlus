namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// Identifies an opt-in CommunityToolkitPlus module.
/// </summary>
public enum CommunityToolkitPlusFeature
{
    /// <summary>Google Play Integrity and Apple App Attest integration.</summary>
    AppIntegrity,

    /// <summary>Runtime accessibility inspection and report export.</summary>
    AccessibilityAudit,

    /// <summary>Transient workflow and UI-state restoration.</summary>
    StateRestoration,

    /// <summary>Crash-safe application upgrade migrations.</summary>
    UpgradeGuard,

    /// <summary>Tamper-aware time synchronization.</summary>
    TrustedTime,

    /// <summary>Apple Wallet and Google Wallet handoff.</summary>
    WalletPasses,

    /// <summary>Purpose-based privacy consent orchestration.</summary>
    PrivacyConsent
}
