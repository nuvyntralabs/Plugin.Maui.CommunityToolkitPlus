using Microsoft.Extensions.DependencyInjection;
using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

static class SampleServices
{
    public static T? Get<T>() where T : class =>
        IPlatformApplication.Current?.Services.GetService<T>();
}

public sealed class IntegrityPage : ContentPage
{
    readonly Label _output = new() { LineBreakMode = LineBreakMode.WordWrap };

    public IntegrityPage()
    {
        Title = "App Integrity";
        var button = new Button { Text = "Create challenge and proof" };
        button.Clicked += async (_, _) => await RunAsync();
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Proofs are opaque. Only a backend can verify them." },
                    button,
                    _output
                }
            }
        };
    }

    async Task RunAsync()
    {
        var integrity = SampleServices.Get<IAppIntegrityService>();
        if (integrity is null)
        {
            _output.Text = "App Integrity is not enabled.";
            return;
        }

        var capability = integrity.GetCapability();
        var challenge = await integrity.CreateChallengeAsync();
        var proof = await integrity.CreateProofAsync(challenge);
        _output.Text =
            $"Supported: {capability.IsSupported} ({capability.Platform})\n" +
            $"Challenge: {challenge.Id}\n" +
            $"Expires: {challenge.ExpiresAt:u}\n" +
            $"Proof: {(proof.Succeeded ? proof.Proof!.Platform + " / " + proof.Proof.Payload[..Math.Min(24, proof.Proof.Payload.Length)] + "…" : proof.Code + " — " + proof.Message)}";
    }
}

public sealed class AccessibilityPage : ContentPage
{
    readonly Label _output = new() { LineBreakMode = LineBreakMode.WordWrap };

    public AccessibilityPage()
    {
        Title = "Accessibility Audit";
        var unlabeled = new Button { AutomationId = "unlabeled" };
        var save = new Button { Text = "Scan this page", AutomationId = "scan" };
        save.Clicked += (_, _) =>
        {
            var audit = SampleServices.Get<IAccessibilityAuditService>();
            if (audit is null)
            {
                _output.Text = "Accessibility Audit is not enabled.";
                return;
            }

            var report = audit.Audit(this);
            _output.Text =
                $"{report.Findings.Count} findings\n" +
                string.Join('\n', report.Findings.Select(finding =>
                    $"{finding.Rule}: {finding.Outcome} — {finding.Message}"));
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "This page includes an unlabeled button so the scan has something to report. Findings assist testing and are not a WCAG certification." },
                    unlabeled,
                    save,
                    _output
                }
            }
        };
    }
}

public sealed class StatePage : ContentPage
{
    readonly Editor _draft = new() { AutoSize = EditorAutoSizeOption.TextChanges, Placeholder = "Unfinished draft" };
    readonly Label _output = new();

    public StatePage()
    {
        Title = "State Restoration";
        var contributor = SampleServices.Get<DemoDraftContributor>();
        if (contributor is not null)
            _draft.Text = contributor.Text;

        var save = new Button { Text = "Checkpoint draft" };
        save.Clicked += async (_, _) =>
        {
            if (contributor is not null)
                contributor.Text = _draft.Text ?? "";
            var state = SampleServices.Get<IStateRestorationService>();
            if (state is null)
                return;
            var checkpoint = await state.CheckpointAsync("//State");
            _output.Text = $"Saved until {checkpoint.ExpiresAt:u}";
        };

        var restore = new Button { Text = "Restore draft" };
        restore.Clicked += async (_, _) =>
        {
            var state = SampleServices.Get<IStateRestorationService>();
            if (state is null)
                return;
            var context = await state.LoadAsync();
            if (context is null)
            {
                _output.Text = "No checkpoint.";
                return;
            }

            await state.ApplyAsync(context);
            if (contributor is not null)
                _draft.Text = contributor.Text;
            _output.Text = $"Restored route {context.Route}";
        };

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                new Label { Text = "Transient workflow state. This is not OfflineSync." },
                _draft,
                save,
                restore,
                _output
            }
        };
    }
}

public sealed class UpgradePage : ContentPage
{
    readonly Label _output = new() { LineBreakMode = LineBreakMode.WordWrap };

    public UpgradePage()
    {
        Title = "Upgrade Guard";
        var run = new Button { Text = "Run startup gate" };
        run.Clicked += async (_, _) =>
        {
            var guard = SampleServices.Get<IUpgradeGuard>();
            if (guard is null)
                return;
            var decision = await guard.RunAsync();
            if (decision == UpgradeDecision.Continue)
                await guard.MarkStartupHealthyAsync();
            var journal = await guard.GetJournalAsync();
            _output.Text =
                $"Decision: {decision}\n" +
                $"Version: {journal.ToVersion}\n" +
                string.Join('\n', journal.Migrations.Select(entry => $"{entry.Id}: {entry.Status}"));
        };

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                new Label { Text = "Await this gate during startup. AppUpdate installs a binary; Upgrade Guard protects local data after that." },
                run,
                _output
            }
        };
    }
}

public sealed class TrustedTimePage : ContentPage
{
    readonly Label _output = new() { LineBreakMode = LineBreakMode.WordWrap };

    public TrustedTimePage()
    {
        Title = "Trusted Time";
        var sync = new Button { Text = "Get trusted UTC" };
        sync.Clicked += async (_, _) =>
        {
            var time = SampleServices.Get<ITrustedTimeService>();
            if (time is null)
                return;
            var result = await time.GetUtcNowAsync();
            _output.Text = result.Succeeded
                ? $"{result.Value!.UtcNow:u}\nConfidence: {result.Value.Confidence}\nSources: {result.Value.SourceCount}"
                : $"{result.Code}: {result.Message}";
        };

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                new Label { Text = "Request trusted time explicitly. This does not replace DateTime.UtcNow." },
                sync,
                _output
            }
        };
    }
}

public sealed class WalletPage : ContentPage
{
    readonly Label _output = new() { LineBreakMode = LineBreakMode.WordWrap };

    public WalletPage()
    {
        Title = "Wallet Passes";
        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                new Label { Text = "Certificates and Google Wallet objects stay on the backend. The sample uses fake save URLs." },
                CreateButton("ticket", "Add ticket"),
                CreateButton("loyalty", "Add loyalty card"),
                CreateButton("coupon", "Add coupon"),
                _output
            }
        };

        var wallet = SampleServices.Get<IWalletPassService>();
        if (wallet is not null)
        {
            var capability = wallet.GetCapability();
            _output.Text =
                $"Platform: {capability.Platform}\nAdd: {capability.CanAdd}  List: {capability.CanList}  Update: {capability.CanUpdate}  Remove: {capability.CanRemove}";
        }
    }

    Button CreateButton(string passId, string title)
    {
        var button = new Button { Text = title };
        button.Clicked += async (_, _) =>
        {
            var wallet = SampleServices.Get<IWalletPassService>();
            if (wallet is null)
                return;
            var result = await wallet.AddAsync(passId);
            _output.Text = result.Succeeded ? $"Presented {passId}." : $"{result.Code}: {result.Message}";
        };
        return button;
    }
}

public sealed class ConsentPage : ContentPage
{
    readonly Label _output = new() { LineBreakMode = LineBreakMode.WordWrap };

    public ConsentPage()
    {
        Title = "Privacy Consent";
        var accept = new Button { Text = "Accept analytics" };
        accept.Clicked += async (_, _) => await RecordAsync(ConsentDecision.Accepted);
        var deny = new Button { Text = "Deny analytics" };
        deny.Clicked += async (_, _) => await RecordAsync(ConsentDecision.Denied);
        var revoke = new Button { Text = "Revoke analytics" };
        revoke.Clicked += async (_, _) => await RecordAsync(ConsentDecision.Revoked);

        Content = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 12,
            Children =
            {
                new Label { Text = "Purpose-based legal consent is not the same as OS permission prompts. This package does not guarantee GDPR or CCPA compliance." },
                accept,
                deny,
                revoke,
                _output
            }
        };
    }

    async Task RecordAsync(ConsentDecision decision)
    {
        var consent = SampleServices.Get<IPrivacyConsentService>();
        if (consent is null)
            return;
        var receipt = await consent.RecordAsync("analytics", decision);
        var allowed = await consent.HasConsentAsync("analytics");
        _output.Text = $"{receipt.Decision} for {receipt.PurposeId} (policy {receipt.PolicyVersion}). Active: {allowed}";
    }
}
