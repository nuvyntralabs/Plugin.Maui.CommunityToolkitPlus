namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// Reports the modules enabled for the current application.
/// </summary>
public interface ICommunityToolkitPlus
{
    /// <summary>Gets the enabled modules.</summary>
    IReadOnlySet<CommunityToolkitPlusFeature> EnabledFeatures { get; }

    /// <summary>Returns whether a module is enabled.</summary>
    bool IsEnabled(CommunityToolkitPlusFeature feature);
}
