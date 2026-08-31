namespace Plugin.Maui.CommunityToolkitPlus.Tests;

public sealed class FacadeTests : IDisposable
{
    public FacadeTests() => CommunityToolkitPlus.Reset();

    public void Dispose() => CommunityToolkitPlus.Reset();

    [Fact]
    public void Default_Throws_Before_Registration()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => _ = CommunityToolkitPlus.Default);

        Assert.Contains("UseMauiCommunityToolkitPlus", exception.Message);
    }

    [Fact]
    public void SetDefault_Throws_When_Implementation_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => CommunityToolkitPlus.SetDefault(null!));
    }

    [Fact]
    public void SetDefault_Replaces_Shared_Instance()
    {
        var first = TestHarness.CreateFacade(options => options.AppIntegrity.Enabled = true);
        var second = TestHarness.CreateFacade(options => options.WalletPasses.Enabled = true);

        CommunityToolkitPlus.SetDefault(first);
        CommunityToolkitPlus.SetDefault(second);

        Assert.Same(second, CommunityToolkitPlus.Default);
        Assert.True(CommunityToolkitPlus.Default.IsEnabled(CommunityToolkitPlusFeature.WalletPasses));
        Assert.False(CommunityToolkitPlus.Default.IsEnabled(CommunityToolkitPlusFeature.AppIntegrity));
    }

    [Fact]
    public void Reset_Clears_Shared_Instance()
    {
        CommunityToolkitPlus.SetDefault(TestHarness.CreateFacade());
        CommunityToolkitPlus.Reset();

        Assert.Throws<InvalidOperationException>(() => _ = CommunityToolkitPlus.Default);
    }

    [Fact]
    public void IsEnabled_Returns_False_For_Unknown_Feature()
    {
        var plus = TestHarness.CreateFacade(options => options.TrustedTime.Enabled = true);

        Assert.False(plus.IsEnabled((CommunityToolkitPlusFeature)255));
    }

    [Fact]
    public void EnabledFeatures_Matches_IsEnabled()
    {
        var plus = TestHarness.CreateFacade(options =>
        {
            options.StateRestoration.Enabled = true;
            options.UpgradeGuard.Enabled = true;
        });

        Assert.Equal(plus.EnabledFeatures.Count, TestHarness.AllFeatures.Count(plus.IsEnabled));
        Assert.All(plus.EnabledFeatures, feature => Assert.True(plus.IsEnabled(feature)));
    }

    [Fact]
    public void Facade_Does_Not_Enable_Modules_Implicitly()
    {
        var plus = TestHarness.CreateFacade();

        Assert.Empty(plus.EnabledFeatures);
        Assert.All(TestHarness.AllFeatures, feature => Assert.False(plus.IsEnabled(feature)));
    }
}
