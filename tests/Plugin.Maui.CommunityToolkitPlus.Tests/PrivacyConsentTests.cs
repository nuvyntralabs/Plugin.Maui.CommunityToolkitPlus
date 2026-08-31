namespace Plugin.Maui.CommunityToolkitPlus.Tests;

public sealed class PrivacyConsentTests : IDisposable
{
    readonly string _directory = TestHarness.CreateTempDirectory();
    readonly TestTimeProvider _time = new(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task Accepted_Purpose_Can_Activate_Sdk()
    {
        var consent = CreateService();
        var activated = false;
        consent.RegisterSdk("analytics", ["analytics"], _ =>
        {
            activated = true;
            return Task.CompletedTask;
        });

        await consent.RecordAsync("analytics", ConsentDecision.Accepted);
        var ready = await consent.ActivateReadySdksAsync();

        Assert.True(await consent.HasConsentAsync("analytics"));
        Assert.Equal(["analytics"], ready);
        Assert.True(activated);
    }

    [Fact]
    public async Task Denied_Purpose_Does_Not_Activate_Sdk()
    {
        var consent = CreateService();
        var activated = false;
        consent.RegisterSdk("ads", ["ads"], _ =>
        {
            activated = true;
            return Task.CompletedTask;
        });

        await consent.RecordAsync("ads", ConsentDecision.Denied);

        Assert.False(await consent.HasConsentAsync("ads"));
        Assert.Empty(await consent.ActivateReadySdksAsync());
        Assert.False(activated);
    }

    [Fact]
    public async Task Revocation_Removes_Consent()
    {
        var consent = CreateService();
        await consent.RecordAsync("analytics", ConsentDecision.Accepted);
        await consent.RecordAsync("analytics", ConsentDecision.Revoked);

        Assert.False(await consent.HasConsentAsync("analytics"));
        Assert.Equal(ConsentDecision.Revoked, (await consent.GetAsync("analytics"))!.Decision);
    }

    [Fact]
    public async Task Policy_Renewal_Invalidates_Previous_Receipts()
    {
        var first = CreateService("1");
        await first.RecordAsync("analytics", ConsentDecision.Accepted);

        var renewed = CreateService("2");
        Assert.False(await renewed.HasConsentAsync("analytics"));
    }

    [Fact]
    public async Task Expired_Receipt_Is_Not_Consent()
    {
        var consent = CreateService("1", TimeSpan.FromMinutes(5));
        await consent.RecordAsync("analytics", ConsentDecision.Accepted);
        _time.Advance(TimeSpan.FromMinutes(6));

        Assert.False(await consent.HasConsentAsync("analytics"));
    }

    IPrivacyConsentService CreateService(string version = "1", TimeSpan? lifetime = null) =>
        new PrivacyConsentService(
            new AtomicVersionedStore(_directory, null, null),
            new PrivacyConsentOptions
            {
                Enabled = true,
                Policy = new ConsentPolicy(
                    version,
                    [new PrivacyPurpose("analytics", "Analytics"), new PrivacyPurpose("ads", "Ads")],
                    lifetime)
            },
            _time,
            platform: null,
            popups: null,
            NullLogger<PrivacyConsentService>.Instance);
}
