namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// Inspects a visual tree for common accessibility defects. Findings assist testing and do not certify WCAG.
/// </summary>
public interface IAccessibilityAuditService
{
    /// <summary>Evaluates the supplied visual tree.</summary>
    AccessibilityAuditReport Audit(Element root);

    /// <summary>Serializes a report to JSON.</summary>
    string ToJson(AccessibilityAuditReport report);

    /// <summary>Serializes a report to SARIF 2.1.0.</summary>
    string ToSarif(AccessibilityAuditReport report);
}

/// <summary>A complete accessibility scan.</summary>
/// <param name="GeneratedAt">UTC timestamp.</param>
/// <param name="Findings">All evaluated findings.</param>
public sealed record AccessibilityAuditReport(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<AccessibilityFinding> Findings);

/// <summary>A single accessibility observation.</summary>
/// <param name="Rule">The rule that produced the finding.</param>
/// <param name="Severity">How serious the finding is.</param>
/// <param name="Outcome">pass, fail, or not_evaluated.</param>
/// <param name="Target">Automation ID, type name, or path.</param>
/// <param name="Message">Human-readable explanation.</param>
public sealed record AccessibilityFinding(
    AccessibilityRule Rule,
    AccessibilitySeverity Severity,
    string Outcome,
    string Target,
    string Message);

/// <summary>Built-in accessibility rules.</summary>
public enum AccessibilityRule
{
    /// <summary>Interactive or informative elements without a semantic label.</summary>
    MissingSemanticLabel,

    /// <summary>The same AutomationId is used more than once.</summary>
    DuplicateAutomationId,

    /// <summary>An interactive target is smaller than the configured minimum.</summary>
    UndersizedTarget,

    /// <summary>Foreground and background colors fail a simple contrast check.</summary>
    ContrastFailure,

    /// <summary>An interactive image has no description.</summary>
    InteractiveImageWithoutDescription,

    /// <summary>Tab indexes look out of order or duplicated.</summary>
    SuspiciousFocusOrder,

    /// <summary>Text is likely to clip at the configured accessibility font scale.</summary>
    TextClippingAtFontScale
}

/// <summary>Finding severity. Rules that cannot be measured return <see cref="NotEvaluated"/>.</summary>
public enum AccessibilitySeverity
{
    /// <summary>The rule could not be measured reliably.</summary>
    NotEvaluated,

    /// <summary>Informational observation.</summary>
    Info,

    /// <summary>Should be fixed before release.</summary>
    Warning,

    /// <summary>Likely blocks assistive use.</summary>
    Error
}
