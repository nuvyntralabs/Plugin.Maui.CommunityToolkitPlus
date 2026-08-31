namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// Configures the opt-in modules provided by CommunityToolkitPlus.
/// </summary>
public sealed class CommunityToolkitPlusOptions
{
    /// <summary>Gets the app-integrity module options.</summary>
    public AppIntegrityOptions AppIntegrity { get; } = new();

    /// <summary>Gets the accessibility-audit module options.</summary>
    public AccessibilityAuditOptions AccessibilityAudit { get; } = new();

    /// <summary>Gets the state-restoration module options.</summary>
    public StateRestorationOptions StateRestoration { get; } = new();

    /// <summary>Gets the upgrade-guard module options.</summary>
    public UpgradeGuardOptions UpgradeGuard { get; } = new();

    /// <summary>Gets the trusted-time module options.</summary>
    public TrustedTimeOptions TrustedTime { get; } = new();

    /// <summary>Gets the wallet-passes module options.</summary>
    public WalletPassOptions WalletPasses { get; } = new();

    /// <summary>Gets the privacy-consent module options.</summary>
    public PrivacyConsentOptions PrivacyConsent { get; } = new();

    /// <summary>
    /// Gets or sets an override directory for module persistence.
    /// When unset, files are stored under <c>FileSystem.AppDataDirectory/community-toolkit-plus</c>.
    /// </summary>
    public string? StorageDirectory { get; set; }

    /// <summary>Gets or sets the clock used by enabled modules. Defaults to <see cref="TimeProvider.System"/>.</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>Gets or sets an optional HTTP handler used by Trusted Time sources.</summary>
    public HttpMessageHandler? HttpMessageHandler { get; set; }

    /// <summary>Gets or sets an optional data protector for persisted payloads.</summary>
    public IPlusDataProtector? DataProtector { get; set; }

    internal IReadOnlySet<CommunityToolkitPlusFeature> GetEnabledFeatures()
    {
        var features = new HashSet<CommunityToolkitPlusFeature>();
        AddIfEnabled(features, CommunityToolkitPlusFeature.AppIntegrity, AppIntegrity);
        AddIfEnabled(features, CommunityToolkitPlusFeature.AccessibilityAudit, AccessibilityAudit);
        AddIfEnabled(features, CommunityToolkitPlusFeature.StateRestoration, StateRestoration);
        AddIfEnabled(features, CommunityToolkitPlusFeature.UpgradeGuard, UpgradeGuard);
        AddIfEnabled(features, CommunityToolkitPlusFeature.TrustedTime, TrustedTime);
        AddIfEnabled(features, CommunityToolkitPlusFeature.WalletPasses, WalletPasses);
        AddIfEnabled(features, CommunityToolkitPlusFeature.PrivacyConsent, PrivacyConsent);
        return features.ToFrozenSet();
    }

    internal void Validate()
    {
        if (AppIntegrity.Enabled && AppIntegrity.ChallengeLifetime <= TimeSpan.Zero)
            throw new InvalidOperationException("AppIntegrity.ChallengeLifetime must be greater than zero.");

        if (StateRestoration.Enabled && StateRestoration.DefaultTimeToLive <= TimeSpan.Zero)
            throw new InvalidOperationException("StateRestoration.DefaultTimeToLive must be greater than zero.");

        if (UpgradeGuard.Enabled)
        {
            if (UpgradeGuard.SafeModeFailureThreshold < 1)
                throw new InvalidOperationException("UpgradeGuard.SafeModeFailureThreshold must be at least 1.");
            if (string.IsNullOrWhiteSpace(UpgradeGuard.CurrentVersion))
                throw new InvalidOperationException("UpgradeGuard.CurrentVersion is required when Upgrade Guard is enabled.");
        }

        if (TrustedTime.Enabled)
        {
            if (TrustedTime.MaxClockSkew <= TimeSpan.Zero)
                throw new InvalidOperationException("TrustedTime.MaxClockSkew must be greater than zero.");
            if (TrustedTime.OfflineGracePeriod < TimeSpan.Zero)
                throw new InvalidOperationException("TrustedTime.OfflineGracePeriod cannot be negative.");
            foreach (var source in TrustedTime.Sources)
            {
                if (!source.IsAbsoluteUri || source.Scheme is not ("https" or "http"))
                    throw new InvalidOperationException($"TrustedTime source '{source}' must be an absolute HTTP(S) URI.");
            }
        }

        if (PrivacyConsent.Enabled && string.IsNullOrWhiteSpace(PrivacyConsent.Policy.Version))
            throw new InvalidOperationException("PrivacyConsent.Policy.Version is required when Privacy Consent is enabled.");
    }

    static void AddIfEnabled(
        ISet<CommunityToolkitPlusFeature> features,
        CommunityToolkitPlusFeature feature,
        FeatureOptions options)
    {
        if (options.Enabled)
            features.Add(feature);
    }
}

/// <summary>
/// Controls whether a CommunityToolkitPlus module is registered.
/// </summary>
public class FeatureOptions
{
    /// <summary>
    /// Gets or sets whether the module is enabled. Modules are disabled by default.
    /// </summary>
    public bool Enabled { get; set; }
}

/// <summary>Configures Google Play Integrity and Apple App Attest.</summary>
public sealed class AppIntegrityOptions : FeatureOptions
{
    /// <summary>Gets or sets how long a challenge remains valid. Default is two minutes.</summary>
    public TimeSpan ChallengeLifetime { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Gets or sets the optional backend URL that issues integrity challenges.</summary>
    public Uri? ChallengeEndpoint { get; set; }
}

/// <summary>Configures runtime accessibility inspection.</summary>
public sealed class AccessibilityAuditOptions : FeatureOptions
{
    /// <summary>Gets or sets the minimum interactive target size in device-independent pixels. Default is 44.</summary>
    public double MinimumTargetSize { get; set; } = 44;

    /// <summary>Gets or sets the font scale used when checking clipped text. Default is 1.3.</summary>
    public double AccessibilityFontScale { get; set; } = 1.3;

    /// <summary>
    /// Gets or sets whether a debug overlay may be shown after a scan.
    /// Automatic scanning remains host-controlled and is intended for DEBUG builds.
    /// </summary>
    public bool ShowDebugOverlay { get; set; } = true;
}

/// <summary>Configures transient UI and workflow restoration.</summary>
public sealed class StateRestorationOptions : FeatureOptions
{
    /// <summary>Gets or sets how long a checkpoint remains valid. Default is seven days.</summary>
    public TimeSpan DefaultTimeToLive { get; set; } = TimeSpan.FromDays(7);
}

/// <summary>Configures crash-safe upgrade migrations.</summary>
public sealed class UpgradeGuardOptions : FeatureOptions
{
    /// <summary>Gets or sets the running application version recorded in the upgrade journal.</summary>
    public string CurrentVersion { get; set; } = "0.0.0";

    /// <summary>Gets or sets how many failed startups trigger safe mode. Default is 3.</summary>
    public int SafeModeFailureThreshold { get; set; } = 3;
}

/// <summary>Configures tamper-aware time synchronization.</summary>
public sealed class TrustedTimeOptions : FeatureOptions
{
    /// <summary>Gets HTTPS or HTTP sources used to establish a UTC anchor.</summary>
    public IList<Uri> Sources { get; } = new List<Uri>();

    /// <summary>Gets or sets the largest accepted difference between sources. Default is 30 seconds.</summary>
    public TimeSpan MaxClockSkew { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets how long a persisted anchor may be used while offline. Default is 24 hours.</summary>
    public TimeSpan OfflineGracePeriod { get; set; } = TimeSpan.FromHours(24);
}

/// <summary>Configures Apple Wallet and Google Wallet handoff.</summary>
public sealed class WalletPassOptions : FeatureOptions;

/// <summary>Configures purpose-based privacy consent.</summary>
public sealed class PrivacyConsentOptions : FeatureOptions
{
    /// <summary>Gets or sets the active consent policy.</summary>
    public ConsentPolicy Policy { get; set; } = new("1", []);
}
