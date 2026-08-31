namespace Plugin.Maui.CommunityToolkitPlus.Tests;

public sealed class AccessibilityAuditTests
{
    readonly IAccessibilityAuditService _audit = new AccessibilityAuditService(
        new AccessibilityAuditOptions(),
        TimeProvider.System);

    [Fact]
    public void Audit_Reports_Missing_Label_On_Button()
    {
        var button = new Button { AutomationId = "Save" };
        var report = _audit.Audit(button);

        Assert.Contains(report.Findings, finding =>
            finding.Rule == AccessibilityRule.MissingSemanticLabel && finding.Outcome == "fail");
    }

    [Fact]
    public void Audit_Accepts_Button_Text_As_Label()
    {
        var report = _audit.Audit(new Button { Text = "Save" });

        Assert.DoesNotContain(report.Findings, finding =>
            finding.Rule == AccessibilityRule.MissingSemanticLabel && finding.Outcome == "fail");
    }

    [Fact]
    public void Audit_Reports_Duplicate_Automation_Ids()
    {
        var layout = new VerticalStackLayout
        {
            Children =
            {
                new Button { Text = "One", AutomationId = "dup" },
                new Button { Text = "Two", AutomationId = "dup" }
            }
        };

        var report = _audit.Audit(layout);

        Assert.Contains(report.Findings, finding =>
            finding.Rule == AccessibilityRule.DuplicateAutomationId && finding.Target == "dup");
    }

    [Fact]
    public void Audit_Reports_Undersized_Interactive_Target()
    {
        var sized = new Button { Text = "Go" };
        typeof(VisualElement).GetProperty(nameof(VisualElement.Width))!.SetValue(sized, 20d);
        typeof(VisualElement).GetProperty(nameof(VisualElement.Height))!.SetValue(sized, 20d);

        var report = _audit.Audit(sized);

        Assert.Contains(report.Findings, finding =>
            finding.Rule == AccessibilityRule.UndersizedTarget && finding.Outcome == "fail");
    }

    [Fact]
    public void Audit_Returns_NotEvaluated_When_Target_Has_No_Bounds()
    {
        var report = _audit.Audit(new Button { Text = "Go" });

        Assert.Contains(report.Findings, finding =>
            finding.Rule == AccessibilityRule.UndersizedTarget && finding.Outcome == "not_evaluated");
    }

    [Fact]
    public void Audit_Reports_Low_Contrast()
    {
        var label = new Label
        {
            Text = "Hello",
            TextColor = Colors.White,
            BackgroundColor = Color.FromRgb(240, 240, 240)
        };

        var report = _audit.Audit(label);

        Assert.Contains(report.Findings, finding =>
            finding.Rule == AccessibilityRule.ContrastFailure && finding.Outcome == "fail");
    }

    [Fact]
    public void Audit_Reports_Interactive_Image_Without_Description()
    {
        var image = new Image { AutomationId = "hero" };
        image.GestureRecognizers.Add(new TapGestureRecognizer());

        var report = _audit.Audit(image);

        Assert.Contains(report.Findings, finding =>
            finding.Rule == AccessibilityRule.InteractiveImageWithoutDescription);
    }

    [Fact]
    public void Audit_Reports_Hidden_Interactive_Focus_Target()
    {
        var layout = new VerticalStackLayout
        {
            Children =
            {
                new Button { Text = "A" },
                new Button { Text = "Hidden", IsVisible = false }
            }
        };

        var report = _audit.Audit(layout);

        Assert.Contains(report.Findings, finding =>
            finding.Rule == AccessibilityRule.SuspiciousFocusOrder && finding.Outcome == "fail");
    }

    [Fact]
    public void ToSarif_Includes_Failed_Results_Only()
    {
        var layout = new VerticalStackLayout
        {
            Children =
            {
                new Button { AutomationId = "dup" },
                new Button { Text = "Two", AutomationId = "dup" }
            }
        };
        var sarif = _audit.ToSarif(_audit.Audit(layout));

        Assert.Contains("\"version\": \"2.1.0\"", sarif);
        Assert.Contains("DuplicateAutomationId", sarif);
        Assert.DoesNotContain("not_evaluated", sarif);
    }

    [Fact]
    public void ToJson_Contains_Findings()
    {
        var json = _audit.ToJson(_audit.Audit(new Button { AutomationId = "x" }));
        Assert.Contains("missingSemanticLabel", json, StringComparison.OrdinalIgnoreCase);
    }

}
