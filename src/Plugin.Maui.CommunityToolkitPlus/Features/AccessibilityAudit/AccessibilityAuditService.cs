namespace Plugin.Maui.CommunityToolkitPlus;

sealed class AccessibilityAuditService : IAccessibilityAuditService
{
    readonly AccessibilityAuditOptions _options;
    readonly TimeProvider _time;

    public AccessibilityAuditService(AccessibilityAuditOptions options, TimeProvider time)
    {
        _options = options;
        _time = time;
    }

    public AccessibilityAuditReport Audit(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var findings = new List<AccessibilityFinding>();
        var elements = new List<Element>();
        Collect(root, elements);

        EvaluateMissingLabels(elements, findings);
        EvaluateDuplicateAutomationIds(elements, findings);
        EvaluateTargetSizes(elements, findings);
        EvaluateContrast(elements, findings);
        EvaluateInteractiveImages(elements, findings);
        EvaluateFocusOrder(elements, findings);
        EvaluateTextClipping(elements, findings);

        return new AccessibilityAuditReport(_time.GetUtcNow(), findings);
    }

    public string ToJson(AccessibilityAuditReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine($"  \"generatedAt\": \"{report.GeneratedAt:O}\",");
        builder.AppendLine("  \"findings\": [");
        for (var i = 0; i < report.Findings.Count; i++)
        {
            var finding = report.Findings[i];
            builder.Append("    {");
            builder.Append($"\"rule\":\"{finding.Rule}\",");
            builder.Append($"\"severity\":\"{finding.Severity}\",");
            builder.Append($"\"outcome\":\"{finding.Outcome}\",");
            builder.Append($"\"target\":{Quote(finding.Target)},");
            builder.Append($"\"message\":{Quote(finding.Message)}");
            builder.Append('}');
            if (i < report.Findings.Count - 1)
                builder.Append(',');
            builder.AppendLine();
        }

        builder.AppendLine("  ]");
        builder.AppendLine("}");
        return builder.ToString();
    }

    public string ToSarif(AccessibilityAuditReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine("  \"version\": \"2.1.0\",");
        builder.AppendLine("  \"$schema\": \"https://json.schemastore.org/sarif-2.1.0.json\",");
        builder.AppendLine("  \"runs\": [{");
        builder.AppendLine("    \"tool\": { \"driver\": {");
        builder.AppendLine("      \"name\": \"Plugin.Maui.CommunityToolkitPlus.AccessibilityAudit\",");
        builder.AppendLine("      \"informationUri\": \"https://github.com/nuvyntralabs/Plugin.Maui.CommunityToolkitPlus\"");
        builder.AppendLine("    } },");
        builder.AppendLine("    \"results\": [");
        var failed = report.Findings.Where(finding => finding.Outcome == "fail").ToArray();
        for (var i = 0; i < failed.Length; i++)
        {
            var finding = failed[i];
            var level = finding.Severity switch
            {
                AccessibilitySeverity.Error => "error",
                AccessibilitySeverity.Warning => "warning",
                _ => "note"
            };
            builder.Append("      {");
            builder.Append($"\"ruleId\":\"{finding.Rule}\",");
            builder.Append($"\"level\":\"{level}\",");
            builder.Append($"\"message\":{{\"text\":{Quote(finding.Message)}}},");
            builder.Append($"\"locations\":[{{\"logicalLocations\":[{{\"fullyQualifiedName\":{Quote(finding.Target)}}}]}}]");
            builder.Append('}');
            if (i < failed.Length - 1)
                builder.Append(',');
            builder.AppendLine();
        }

        builder.AppendLine("    ]");
        builder.AppendLine("  }]");
        builder.AppendLine("}");
        return builder.ToString();
    }

    void EvaluateMissingLabels(IReadOnlyList<Element> elements, List<AccessibilityFinding> findings)
    {
        foreach (var element in elements)
        {
            if (!IsInteractive(element) && element is not Image)
                continue;

            if (HasLabel(element))
                continue;

            findings.Add(new(
                AccessibilityRule.MissingSemanticLabel,
                AccessibilitySeverity.Error,
                "fail",
                Describe(element),
                "The element is interactive or informative but has no semantic description or AutomationProperties.Name."));
        }
    }

    void EvaluateDuplicateAutomationIds(IReadOnlyList<Element> elements, List<AccessibilityFinding> findings)
    {
        foreach (var group in elements
            .Where(element => !string.IsNullOrWhiteSpace(element.AutomationId))
            .GroupBy(element => element.AutomationId, StringComparer.Ordinal))
        {
            if (group.Count() < 2)
                continue;

            findings.Add(new(
                AccessibilityRule.DuplicateAutomationId,
                AccessibilitySeverity.Error,
                "fail",
                group.Key!,
                $"AutomationId '{group.Key}' is used {group.Count()} times."));
        }
    }

    void EvaluateTargetSizes(IReadOnlyList<Element> elements, List<AccessibilityFinding> findings)
    {
        foreach (var element in elements.OfType<VisualElement>())
        {
            if (!IsInteractive(element))
                continue;

            if (element.Width <= 0 || element.Height <= 0)
            {
                findings.Add(new(
                    AccessibilityRule.UndersizedTarget,
                    AccessibilitySeverity.NotEvaluated,
                    "not_evaluated",
                    Describe(element),
                    "The interactive target has not been laid out, so its size cannot be measured."));
                continue;
            }

            if (element.Width + 0.01 < _options.MinimumTargetSize ||
                element.Height + 0.01 < _options.MinimumTargetSize)
            {
                findings.Add(new(
                    AccessibilityRule.UndersizedTarget,
                    AccessibilitySeverity.Warning,
                    "fail",
                    Describe(element),
                    $"Interactive target is {element.Width:0.#}x{element.Height:0.#}, below {_options.MinimumTargetSize}."));
            }
        }
    }

    void EvaluateContrast(IReadOnlyList<Element> elements, List<AccessibilityFinding> findings)
    {
        foreach (var view in elements.OfType<View>())
        {
            if (view is not Label and not Button)
                continue;

            if (!TryGetTextColor(view, out var foreground) ||
                !TryGetBackground(view, out var background))
            {
                findings.Add(new(
                    AccessibilityRule.ContrastFailure,
                    AccessibilitySeverity.NotEvaluated,
                    "not_evaluated",
                    Describe(view),
                    "Foreground or background color is not set, so contrast cannot be measured."));
                continue;
            }

            var ratio = ContrastRatio(foreground, background);
            if (ratio < 4.5)
            {
                findings.Add(new(
                    AccessibilityRule.ContrastFailure,
                    AccessibilitySeverity.Warning,
                    "fail",
                    Describe(view),
                    $"Approximate contrast ratio {ratio:0.00} is below 4.5:1."));
            }
        }
    }

    void EvaluateInteractiveImages(IReadOnlyList<Element> elements, List<AccessibilityFinding> findings)
    {
        foreach (var image in elements.OfType<Image>())
        {
            var interactive = image.GestureRecognizers.OfType<TapGestureRecognizer>().Any()
                || !string.IsNullOrWhiteSpace(image.AutomationId);
            if (!interactive)
                continue;

            if (HasLabel(image))
                continue;

            findings.Add(new(
                AccessibilityRule.InteractiveImageWithoutDescription,
                AccessibilitySeverity.Error,
                "fail",
                Describe(image),
                "An interactive image has no semantic description."));
        }
    }

    void EvaluateFocusOrder(IReadOnlyList<Element> elements, List<AccessibilityFinding> findings)
    {
        var interactive = elements.OfType<VisualElement>().Where(IsInteractive).ToArray();
        if (interactive.Length == 0)
            return;

        foreach (var element in interactive.Where(item => !item.IsVisible || item.Opacity <= 0))
        {
            findings.Add(new(
                AccessibilityRule.SuspiciousFocusOrder,
                AccessibilitySeverity.Warning,
                "fail",
                Describe(element),
                "An interactive element is hidden or fully transparent, which can produce a surprising focus order."));
        }

        foreach (var element in interactive.Where(item => item.InputTransparent && item.IsEnabled))
        {
            findings.Add(new(
                AccessibilityRule.SuspiciousFocusOrder,
                AccessibilitySeverity.Warning,
                "fail",
                Describe(element),
                "An enabled interactive element is input-transparent and may be skipped or reached unexpectedly."));
        }
    }

    void EvaluateTextClipping(IReadOnlyList<Element> elements, List<AccessibilityFinding> findings)
    {
        foreach (var label in elements.OfType<Label>())
        {
            if (string.IsNullOrEmpty(label.Text) || label.Width <= 0)
            {
                findings.Add(new(
                    AccessibilityRule.TextClippingAtFontScale,
                    AccessibilitySeverity.NotEvaluated,
                    "not_evaluated",
                    Describe(label),
                    "Label bounds are not available, so font-scale clipping cannot be measured."));
                continue;
            }

            if (label.LineBreakMode is LineBreakMode.TailTruncation or LineBreakMode.HeadTruncation
                or LineBreakMode.MiddleTruncation)
            {
                findings.Add(new(
                    AccessibilityRule.TextClippingAtFontScale,
                    AccessibilitySeverity.Warning,
                    "fail",
                    Describe(label),
                    $"Label uses {label.LineBreakMode} and may clip text at font scale {_options.AccessibilityFontScale}."));
            }
        }
    }

    static void Collect(Element element, List<Element> elements)
    {
        elements.Add(element);
        if (element is not IVisualTreeElement visual)
            return;

        foreach (var child in visual.GetVisualChildren())
        {
            if (child is Element childElement)
                Collect(childElement, elements);
        }
    }

    static bool IsInteractive(Element element) =>
        element is Button or ImageButton or CheckBox or Switch or Slider or Stepper or DatePicker
            or TimePicker or SearchBar or Editor or Entry or Picker;

    static bool HasLabel(Element element)
    {
        if (!string.IsNullOrWhiteSpace(SemanticProperties.GetDescription(element)))
            return true;
        if (!string.IsNullOrWhiteSpace(AutomationProperties.GetName(element)))
            return true;
        if (element is Button button && !string.IsNullOrWhiteSpace(button.Text))
            return true;
        if (element is Label label && !string.IsNullOrWhiteSpace(label.Text))
            return true;
        return false;
    }

    static string Describe(Element element) =>
        string.IsNullOrWhiteSpace(element.AutomationId)
            ? element.GetType().Name
            : element.AutomationId;

    static bool TryGetTextColor(View view, out Color color)
    {
        color = view switch
        {
            Label label => label.TextColor,
            Button button => button.TextColor,
            _ => null
        } ?? Colors.Transparent;
        return color != Colors.Transparent && color.Alpha > 0;
    }

    static bool TryGetBackground(View view, out Color color)
    {
        color = view.BackgroundColor ?? Colors.Transparent;
        return color != Colors.Transparent && color.Alpha > 0;
    }

    static double ContrastRatio(Color first, Color second)
    {
        var l1 = RelativeLuminance(first);
        var l2 = RelativeLuminance(second);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    static double RelativeLuminance(Color color)
    {
        var r = Linear(color.Red);
        var g = Linear(color.Green);
        var b = Linear(color.Blue);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    static double Linear(double channel) =>
        channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

    static string Quote(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ => character.ToString()
            });
        }

        builder.Append('"');
        return builder.ToString();
    }
}
