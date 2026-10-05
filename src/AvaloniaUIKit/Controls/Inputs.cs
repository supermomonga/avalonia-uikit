using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's InputState rules for a TextBox (crates/base/src/input/base/state.rs):
/// edits that would leave an invalid text, a mask that formats the text while it
/// is typed, Escape clearing the text, and Tab indenting the selected lines of a
/// multi-line TextBox. Nothing changes until the app sets one of the properties.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PatternProperty"/>, <see cref="ValidateProperty"/> and
/// <see cref="MaskPatternProperty"/> apply to a single-line TextBox, as GPUI's
/// do. An edit by typing, pasting, an IME commit, cutting or deleting is
/// checked against the whole text it would leave: it is rejected when that text
/// fails the predicate, the mask or the pattern (an empty text always passes)
/// and the text before it passed (state.rs replace_text_in_range), so a text
/// set by the app that does not pass can still be edited. A mask then
/// formats the text and keeps the caret where GPUI puts it. Text the app sets
/// is masked but not checked. Undo and redo restore texts as they were.
/// </para>
/// <para>
/// On a NumberInput the properties apply to its text field.
/// </para>
/// </remarks>
public static class Inputs
{
    /// <summary>
    /// A regular expression the whole text must match (GPUI's pattern), searched
    /// anywhere unless anchored: <c>^[a-zA-Z0-9]*$</c>.
    /// </summary>
    public static readonly AttachedProperty<string?> PatternProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("Pattern", typeof(Inputs));

    /// <summary>A predicate the whole text must pass (GPUI's validate), e.g. <c>s => float.TryParse(s, out _)</c>.</summary>
    public static readonly AttachedProperty<Func<string, bool>?> ValidateProperty =
        AvaloniaProperty.RegisterAttached<Control, Func<string, bool>?>("Validate", typeof(Inputs));

    /// <summary>
    /// The mask that formats the text while it is typed (GPUI's mask_pattern): a
    /// <see cref="PatternMask"/> ("(999)999-9999") or a <see cref="NumberMask"/>.
    /// An empty TextBox shows a pattern's placeholder unless it has its own.
    /// </summary>
    public static readonly AttachedProperty<MaskPattern?> MaskPatternProperty =
        AvaloniaProperty.RegisterAttached<Control, MaskPattern?>("MaskPattern", typeof(Inputs));

    /// <summary>Whether Escape clears the text (GPUI's clean_on_escape), as an undoable edit.</summary>
    public static readonly AttachedProperty<bool> CleanOnEscapeProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("CleanOnEscape", typeof(Inputs));

    /// <summary>
    /// The indent of a multi-line TextBox (AcceptsReturn) in spaces; 0, the
    /// default, leaves Tab to move the focus. GPUI's Textarea indents by 2
    /// (indent.rs TabSize): Tab indents (at the caret, or the selected lines),
    /// Shift+Tab outdents the caret's or the selected lines, and Cmd+] / Cmd+[
    /// (Ctrl on Windows and Linux) indent and outdent the lines.
    /// </summary>
    public static readonly AttachedProperty<int> TabSizeProperty =
        AvaloniaProperty.RegisterAttached<Control, int>("TabSize", typeof(Inputs));

    /// <summary>Whether an indent is a tab character rather than <see cref="TabSizeProperty"/> spaces (GPUI's hard_tabs).</summary>
    public static readonly AttachedProperty<bool> HardTabsProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("HardTabs", typeof(Inputs));

    private static readonly AttachedProperty<Rules?> RulesProperty =
        AvaloniaProperty.RegisterAttached<TextBox, Rules?>("Rules", typeof(Inputs));

    static Inputs()
    {
        foreach (var property in new AvaloniaProperty[] { PatternProperty, ValidateProperty, MaskPatternProperty, CleanOnEscapeProperty, TabSizeProperty })
        {
            property.Changed.AddClassHandler<TextBox>((box, _) => Attach(box).Update());
        }
    }

    /// <summary>Gets the regular expression the text must match.</summary>
    public static string? GetPattern(Control control) => control.GetValue(PatternProperty);

    /// <summary>Sets the regular expression the text must match.</summary>
    public static void SetPattern(Control control, string? value) => control.SetValue(PatternProperty, value);

    /// <summary>Gets the predicate the text must pass.</summary>
    public static Func<string, bool>? GetValidate(Control control) => control.GetValue(ValidateProperty);

    /// <summary>Sets the predicate the text must pass.</summary>
    public static void SetValidate(Control control, Func<string, bool>? value) => control.SetValue(ValidateProperty, value);

    /// <summary>Gets the mask.</summary>
    public static MaskPattern? GetMaskPattern(Control control) => control.GetValue(MaskPatternProperty);

    /// <summary>Sets the mask.</summary>
    public static void SetMaskPattern(Control control, MaskPattern? value) => control.SetValue(MaskPatternProperty, value);

    /// <summary>Gets whether Escape clears the text.</summary>
    public static bool GetCleanOnEscape(Control control) => control.GetValue(CleanOnEscapeProperty);

    /// <summary>Sets whether Escape clears the text.</summary>
    public static void SetCleanOnEscape(Control control, bool value) => control.SetValue(CleanOnEscapeProperty, value);

    /// <summary>Gets the indent size.</summary>
    public static int GetTabSize(Control control) => control.GetValue(TabSizeProperty);

    /// <summary>Sets the indent size.</summary>
    public static void SetTabSize(Control control, int value) => control.SetValue(TabSizeProperty, value);

    /// <summary>Gets whether an indent is a tab character.</summary>
    public static bool GetHardTabs(Control control) => control.GetValue(HardTabsProperty);

    /// <summary>Sets whether an indent is a tab character.</summary>
    public static void SetHardTabs(Control control, bool value) => control.SetValue(HardTabsProperty, value);

    /// <summary>The text without its mask's separators (GPUI's unmask_value): "1234.5" for "1,234.5".</summary>
    public static string UnmaskValue(TextBox box) =>
        box.GetValue(MaskPatternProperty) is { } mask ? mask.Unmask(box.Text ?? string.Empty) : box.Text ?? string.Empty;

    /// <summary>Whether <paramref name="text"/> passes the box's predicate, mask and pattern.</summary>
    internal static bool IsValidText(TextBox box, string text) => box.GetValue(RulesProperty)?.IsValid(text) ?? true;

    private static Rules Attach(TextBox box)
    {
        if (box.GetValue(RulesProperty) is not { } rules)
        {
            rules = new Rules(box);
            box.SetValue(RulesProperty, rules);
        }
        return rules;
    }

    /// <summary>The handlers of one TextBox.</summary>
    private sealed class Rules
    {
        private readonly TextBox _box;
        private Regex? _regex;
        private string? _regexSource;
        private string? _maskPlaceholder;
        // While the rules edit the text themselves.
        private bool _applying;
        // The text and selection before a key the TextBox handles, between the tunnel and the bubble.
        private (string Text, int Start, int End, Key Key)? _beforeKey;

        public Rules(TextBox box)
        {
            _box = box;
            box.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
            box.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            box.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble, handledEventsToo: true);
            box.AddHandler(TextBox.PastingFromClipboardEvent, OnPasting, RoutingStrategies.Bubble);
            box.AddHandler(TextBox.CuttingToClipboardEvent, OnCutting, RoutingStrategies.Bubble);
            box.PropertyChanged += OnPropertyChanged;
        }

        private string Text => _box.Text ?? string.Empty;

        private bool IsSingleLine => !_box.AcceptsReturn;

        private MaskPattern Mask => _box.GetValue(MaskPatternProperty) ?? MaskPattern.None;

        // GPUI validates and masks only single-line inputs.
        private bool HasTextRules =>
            IsSingleLine && (_box.GetValue(PatternProperty) is not null || _box.GetValue(ValidateProperty) is not null || !Mask.IsNone);

        private (int Start, int End) Selection => (Math.Min(_box.SelectionStart, _box.SelectionEnd), Math.Max(_box.SelectionStart, _box.SelectionEnd));

        public void Update()
        {
            var placeholder = IsSingleLine ? Mask.Placeholder : null;
            if (_box.PlaceholderText is null || _box.PlaceholderText == _maskPlaceholder)
            {
                _box.SetCurrentValue(TextBox.PlaceholderTextProperty, placeholder);
            }
            // state.rs mask_pattern leaves the text it has as it is, as default_value does.
            _maskPlaceholder = placeholder;
        }

        /// <summary>state.rs is_valid_input: an empty text passes; otherwise the predicate, the mask, then the pattern.</summary>
        public bool IsValid(string text)
        {
            if (text.Length == 0)
            {
                return true;
            }
            if (_box.GetValue(ValidateProperty) is { } validate && !validate(text))
            {
                return false;
            }
            if (!Mask.IsValid(text))
            {
                return false;
            }
            var source = _box.GetValue(PatternProperty);
            if (source is null)
            {
                return true;
            }
            if (_regexSource != source)
            {
                _regex = new Regex(source, RegexOptions.CultureInvariant);
                _regexSource = source;
            }
            return _regex!.IsMatch(text);
        }

        // Only a valid text is kept from going invalid, so a text that does not pass stays editable.
        private bool Rejects(string before, string after) => !IsValid(after) && IsValid(before);

        /// <summary>The text the TextBox would insert for <paramref name="input"/> (TextBox.SanitizeInputText, MaxLength).</summary>
        private string Sanitize(string input, int replaced)
        {
            if (IsSingleLine && input.IndexOfAny(['\r', '\n', '\v', '\f', '\u0085', '\u2028', '\u2029']) is var lineBreak and >= 0)
            {
                input = input[..lineBreak];
            }
            input = input.Replace("\u007f", string.Empty, StringComparison.Ordinal);
            var length = Text.Length - replaced + input.Length;
            if (_box.MaxLength > 0 && length > _box.MaxLength)
            {
                input = input[..Math.Max(0, input.Length - (length - _box.MaxLength))];
            }
            return input;
        }

        /// <summary>
        /// Puts <paramref name="input"/> over the selection by the rules: rejected,
        /// masked, or typed as is. Returns false to leave it to the TextBox.
        /// </summary>
        private bool Insert(string input, bool always)
        {
            var mask = Mask;
            var typed = mask.Normalize(input);
            var (start, end) = Selection;
            typed = Sanitize(typed, end - start);
            if (typed.Length == 0)
            {
                return false;
            }
            var before = Text;
            var pending = string.Concat(before.AsSpan(0, start), typed, before.AsSpan(end));
            if (Rejects(before, pending))
            {
                return true;
            }
            var masked = mask.Mask(pending);
            if (!always && masked == pending && typed == input)
            {
                return false;
            }
            Apply(masked, Caret(start, typed.Length, pending, masked));
            return true;
        }

        /// <summary>state.rs: the caret after an edit at <paramref name="start"/> that inserted <paramref name="inserted"/> characters, once masked.</summary>
        private static int Caret(int start, int inserted, string pending, string masked) =>
            Math.Min(start + Math.Max(0, inserted + masked.Length - pending.Length), masked.Length);

        /// <summary>
        /// Replaces the text with <paramref name="text"/> as the TextBox's own edit
        /// (so it can be undone): only the part that differs is selected and typed
        /// over. Then puts the selection at <paramref name="anchor"/>..<paramref name="caret"/>.
        /// </summary>
        private void Apply(string text, int caret, int? anchor = null)
        {
            var before = Text;
            var prefix = 0;
            var limit = Math.Min(before.Length, text.Length);
            while (prefix < limit && before[prefix] == text[prefix])
            {
                prefix++;
            }
            var suffix = 0;
            while (suffix < limit - prefix && before[^(suffix + 1)] == text[^(suffix + 1)])
            {
                suffix++;
            }
            _applying = true;
            try
            {
                if (prefix < before.Length - suffix || prefix < text.Length - suffix)
                {
                    Select(prefix, before.Length - suffix);
                    _box.SelectedText = text[prefix..^suffix];
                }
                Select(anchor ?? caret, caret);
            }
            finally
            {
                _applying = false;
            }
        }

        private void Select(int anchor, int caret)
        {
            _box.SetCurrentValue(TextBox.CaretIndexProperty, caret);
            _box.SetCurrentValue(TextBox.SelectionStartProperty, anchor);
            _box.SetCurrentValue(TextBox.SelectionEndProperty, caret);
        }

        private void OnTextInput(object? sender, TextInputEventArgs e)
        {
            if (!e.Handled && !_box.IsReadOnly && !string.IsNullOrEmpty(e.Text) && HasTextRules)
            {
                e.Handled = Insert(e.Text, always: false);
            }
        }

        private void OnPasting(object? sender, RoutedEventArgs e)
        {
            if (e.Handled || _box.IsReadOnly || !HasTextRules)
            {
                return;
            }
            // The clipboard is read here so the pasted text is checked as a whole edit.
            e.Handled = true;
            _ = PasteAsync();
        }

        private async Task PasteAsync()
        {
            if (TopLevel.GetTopLevel(_box)?.Clipboard is not { } clipboard)
            {
                return;
            }
            var text = await clipboard.TryGetTextAsync();
            if (!string.IsNullOrEmpty(text))
            {
                Insert(text, always: true);
            }
        }

        private void OnCutting(object? sender, RoutedEventArgs e)
        {
            var (start, end) = Selection;
            if (e.Handled || _box.IsReadOnly || !HasTextRules || start == end || (_box.PasswordChar != default && !_box.RevealPassword))
            {
                return;
            }
            e.Handled = true;
            var before = Text;
            if (TopLevel.GetTopLevel(_box)?.Clipboard is { } clipboard)
            {
                _ = clipboard.SetTextAsync(before[start..end]);
            }
            var pending = before.Remove(start, end - start);
            if (!Rejects(before, pending))
            {
                var masked = Mask.Mask(pending);
                Apply(masked, Caret(start, 0, pending, masked));
            }
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            _beforeKey = null;
            if (e.Handled)
            {
                return;
            }
            if (TryIndent(e))
            {
                e.Handled = true;
                return;
            }
            var (start, end) = Selection;
            _beforeKey = (Text, start, end, e.Key);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            var before = _beforeKey;
            _beforeKey = null;
            if (e.Key == Key.Escape && !e.Handled && e.KeyModifiers == KeyModifiers.None && _box.GetValue(CleanOnEscapeProperty))
            {
                // state.rs clean: an edit, even on a read-only input.
                _box.Clear();
                e.Handled = true;
                return;
            }
            if (before is not { } b || b.Key is not (Key.Back or Key.Delete) || !HasTextRules)
            {
                return;
            }
            var after = Text;
            if (after == b.Text)
            {
                return;
            }
            if (Rejects(b.Text, after))
            {
                if (_box.IsUndoEnabled && _box.CanUndo)
                {
                    _applying = true;
                    try
                    {
                        _box.Undo();
                    }
                    finally
                    {
                        _applying = false;
                    }
                }
                if (Text != b.Text)
                {
                    Apply(b.Text, b.End, b.Start);
                }
                return;
            }
            var masked = Mask.Mask(after);
            if (masked != after)
            {
                // The deleted range: before a collapsed caret for Backspace, after it for Delete.
                var removed = b.Text.Length - after.Length;
                var start = b.Start != b.End || b.Key == Key.Delete ? b.Start : b.End - removed;
                Apply(masked, Caret(start, 0, after, masked));
            }
        }

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.TextProperty && !_applying && _beforeKey is null)
            {
                MaskProgrammaticText();
            }
            else if (e.Property == TextBox.AcceptsReturnProperty)
            {
                Update();
            }
        }

        // state.rs set_value masks the text too; the caret goes to the end.
        private void MaskProgrammaticText()
        {
            var text = Text;
            if (!IsSingleLine || Mask.IsNone || text.Length == 0)
            {
                return;
            }
            var masked = Mask.Mask(text);
            if (masked != text)
            {
                _applying = true;
                try
                {
                    _box.SetCurrentValue(TextBox.TextProperty, masked);
                    _box.SetCurrentValue(TextBox.CaretIndexProperty, masked.Length);
                }
                finally
                {
                    _applying = false;
                }
            }
        }

        /// <summary>indent.rs apply_indent: Tab, Shift+Tab, Cmd+] and Cmd+[ on an editable multi-line TextBox.</summary>
        private bool TryIndent(KeyEventArgs e)
        {
            var size = _box.GetValue(TabSizeProperty);
            if (size <= 0 || IsSingleLine || _box.IsReadOnly)
            {
                return false;
            }
            var command = Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control;
            bool outdent, block;
            if (e.Key == Key.Tab && e.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift)
            {
                (outdent, block) = (e.KeyModifiers == KeyModifiers.Shift, false);
            }
            else if ((e.Key is Key.OemCloseBrackets or Key.OemOpenBrackets) && e.KeyModifiers == command)
            {
                (outdent, block) = (e.Key == Key.OemOpenBrackets, true);
            }
            else
            {
                return false;
            }
            Indent(outdent, block, _box.GetValue(HardTabsProperty) ? "\t" : new string(' ', size));
            return true;
        }

        private void Indent(bool outdent, bool block, string tab)
        {
            var text = Text;
            var (anchor, caret) = (_box.SelectionStart, _box.SelectionEnd);
            var (start, end) = Selection;
            // Edits as (offset, removed length, inserted text), in order.
            var edits = new List<(int At, int Removed, string Inserted)>();
            if (block || start != end)
            {
                // compute_block_indent: every line the selection touches, from its start.
                for (var line = LineStart(text, start); ; line = text.IndexOf('\n', line) + 1)
                {
                    if (!outdent)
                    {
                        edits.Add((line, 0, tab));
                    }
                    else if (string.CompareOrdinal(text, line, tab, 0, tab.Length) == 0)
                    {
                        edits.Add((line, tab.Length, string.Empty));
                    }
                    if (text.IndexOf('\n', line) is var next and >= 0 && next < end)
                    {
                        continue;
                    }
                    break;
                }
            }
            else if (!outdent)
            {
                // compute_inline_indent: the indent goes in at the caret.
                edits.Add((caret, 0, tab));
            }
            else if (LineStart(text, caret) is var line && string.CompareOrdinal(text, line, tab, 0, tab.Length) == 0)
            {
                edits.Add((line, tab.Length, string.Empty));
            }
            if (edits.Count == 0)
            {
                return;
            }
            var result = new StringBuilder(text.Length + edits.Count * tab.Length);
            var at = 0;
            foreach (var edit in edits)
            {
                result.Append(text, at, edit.At - at).Append(edit.Inserted);
                at = edit.At + edit.Removed;
            }
            result.Append(text, at, text.Length - at);
            // An offset moves by the edits before it; one inside removed indentation stays on its line.
            int Map(int offset)
            {
                var shift = 0;
                foreach (var edit in edits)
                {
                    if (edit.Removed == 0 ? edit.At <= offset : edit.At < offset)
                    {
                        shift += edit.Inserted.Length - Math.Min(edit.Removed, offset - edit.At);
                    }
                }
                return offset + shift;
            }
            Apply(result.ToString(), Map(caret), Map(anchor));
        }

        private static int LineStart(string text, int offset) =>
            offset == 0 ? 0 : text.LastIndexOf('\n', offset - 1) + 1;
    }
}
