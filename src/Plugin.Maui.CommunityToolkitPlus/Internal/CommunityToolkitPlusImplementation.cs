namespace Plugin.Maui.CommunityToolkitPlus;

sealed class CommunityToolkitPlusImplementation : ICommunityToolkitPlus
{
    public CommunityToolkitPlusImplementation(CommunityToolkitPlusOptions options)
    {
        EnabledFeatures = options.GetEnabledFeatures();
    }

    public IReadOnlySet<CommunityToolkitPlusFeature> EnabledFeatures { get; }

    public bool IsEnabled(CommunityToolkitPlusFeature feature) =>
        EnabledFeatures.Contains(feature);
}
