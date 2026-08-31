namespace Plugin.Maui.CommunityToolkitPlus.Tests;

public sealed class OptionsTests
{
    [Fact]
    public void Modules_Are_Disabled_By_Default()
    {
        var options = new CommunityToolkitPlusOptions();

        Assert.False(options.AppIntegrity.Enabled);
        Assert.False(options.AccessibilityAudit.Enabled);
        Assert.False(options.StateRestoration.Enabled);
        Assert.False(options.UpgradeGuard.Enabled);
        Assert.False(options.TrustedTime.Enabled);
        Assert.False(options.WalletPasses.Enabled);
        Assert.False(options.PrivacyConsent.Enabled);
        Assert.Empty(options.GetEnabledFeatures());
    }

    [Fact]
    public void FeatureOptions_Default_Is_Disabled()
    {
        Assert.False(new FeatureOptions().Enabled);
    }

    [Theory]
    [InlineData(CommunityToolkitPlusFeature.AppIntegrity)]
    [InlineData(CommunityToolkitPlusFeature.AccessibilityAudit)]
    [InlineData(CommunityToolkitPlusFeature.StateRestoration)]
    [InlineData(CommunityToolkitPlusFeature.UpgradeGuard)]
    [InlineData(CommunityToolkitPlusFeature.TrustedTime)]
    [InlineData(CommunityToolkitPlusFeature.WalletPasses)]
    [InlineData(CommunityToolkitPlusFeature.PrivacyConsent)]
    public void Enabling_One_Module_Does_Not_Enable_Others(CommunityToolkitPlusFeature feature)
    {
        var options = new CommunityToolkitPlusOptions();
        Enable(options, feature);

        var enabled = options.GetEnabledFeatures();

        Assert.Single(enabled);
        Assert.Contains(feature, enabled);
        Assert.All(
            TestHarness.AllFeatures.Where(candidate => candidate != feature),
            other => Assert.DoesNotContain(other, enabled));
    }

    [Fact]
    public void Enabling_All_Modules_Returns_Complete_Set()
    {
        var options = new CommunityToolkitPlusOptions();
        foreach (var feature in TestHarness.AllFeatures)
            Enable(options, feature);

        var enabled = options.GetEnabledFeatures();

        Assert.Equal(TestHarness.AllFeatures.Count, enabled.Count);
        Assert.All(TestHarness.AllFeatures, feature => Assert.Contains(feature, enabled));
    }

    [Fact]
    public void Enabled_Features_Are_Read_Only()
    {
        var options = new CommunityToolkitPlusOptions();
        options.UpgradeGuard.Enabled = true;

        Assert.Throws<NotSupportedException>(
            () => ((ISet<CommunityToolkitPlusFeature>)options.GetEnabledFeatures())
                .Add(CommunityToolkitPlusFeature.TrustedTime));
    }

    [Fact]
    public void Disabling_A_Module_Removes_It_From_Enabled_Set()
    {
        var options = new CommunityToolkitPlusOptions();
        options.PrivacyConsent.Enabled = true;
        options.PrivacyConsent.Enabled = false;

        Assert.Empty(options.GetEnabledFeatures());
    }

    static void Enable(CommunityToolkitPlusOptions options, CommunityToolkitPlusFeature feature)
    {
        switch (feature)
        {
            case CommunityToolkitPlusFeature.AppIntegrity:
                options.AppIntegrity.Enabled = true;
                break;
            case CommunityToolkitPlusFeature.AccessibilityAudit:
                options.AccessibilityAudit.Enabled = true;
                break;
            case CommunityToolkitPlusFeature.StateRestoration:
                options.StateRestoration.Enabled = true;
                break;
            case CommunityToolkitPlusFeature.UpgradeGuard:
                options.UpgradeGuard.Enabled = true;
                break;
            case CommunityToolkitPlusFeature.TrustedTime:
                options.TrustedTime.Enabled = true;
                break;
            case CommunityToolkitPlusFeature.WalletPasses:
                options.WalletPasses.Enabled = true;
                break;
            case CommunityToolkitPlusFeature.PrivacyConsent:
                options.PrivacyConsent.Enabled = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(feature), feature, null);
        }
    }
}
