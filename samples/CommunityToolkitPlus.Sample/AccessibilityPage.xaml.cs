using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class AccessibilityPage : ContentPage
{
    public AccessibilityPage()
    {
        InitializeComponent();
    }

    void OnScanClicked(object? sender, EventArgs e)
    {
        var audit = SampleServices.Get<IAccessibilityAuditService>();
        if (audit is null)
        {
            OutputLabel.Text = "Accessibility Audit is not enabled.";
            return;
        }

        var report = audit.Audit(this);
        OutputLabel.Text =
            $"{report.Findings.Count} findings\n" +
            string.Join('\n', report.Findings.Select(finding =>
                $"{finding.Rule}: {finding.Outcome} — {finding.Message}"));
    }
}
