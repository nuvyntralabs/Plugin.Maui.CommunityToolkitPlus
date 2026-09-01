using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class AccessibilityPage : ContentPage
{
    AccessibilityAuditReport? _lastReport;

    public AccessibilityPage()
    {
        InitializeComponent();
        TransparentButton.InputTransparent = true;
        HeroImage.GestureRecognizers.Add(new TapGestureRecognizer());
    }

    void OnScanClicked(object? sender, EventArgs e)
    {
        var audit = SampleServices.Get<IAccessibilityAuditService>();
        if (audit is null)
        {
            OutputLabel.Text = "Accessibility Audit is not enabled.";
            return;
        }

        _lastReport = audit.Audit(this);
        OutputLabel.Text =
            $"{_lastReport.Findings.Count} findings\n" +
            string.Join('\n', _lastReport.Findings.Select(finding =>
                $"{finding.Rule} [{finding.Severity}]: {finding.Outcome} — {finding.Message}"));
    }

    void OnExportJsonClicked(object? sender, EventArgs e) =>
        Export(audit => audit.ToJson(RequireReport()));

    void OnExportSarifClicked(object? sender, EventArgs e) =>
        Export(audit => audit.ToSarif(RequireReport()));

    void Export(Func<IAccessibilityAuditService, string> exporter)
    {
        var audit = SampleServices.Get<IAccessibilityAuditService>();
        if (audit is null)
        {
            OutputLabel.Text = "Accessibility Audit is not enabled.";
            return;
        }

        try
        {
            OutputLabel.Text = exporter(audit);
        }
        catch (Exception ex)
        {
            OutputLabel.Text = ex.Message;
        }
    }

    AccessibilityAuditReport RequireReport() =>
        _lastReport ?? throw new InvalidOperationException("Scan the page before exporting a report.");
}
