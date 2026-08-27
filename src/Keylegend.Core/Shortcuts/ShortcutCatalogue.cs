using Keylegend.Core.Input;

namespace Keylegend.Core.Shortcuts;

/// <summary>
/// Broad purpose of a command, so that related shortcuts read as a block of one colour
/// rather than as scattered individual keys.
/// </summary>
public enum FunctionGroup
{
    Edit,
    File,
    Search,
    View,
    Window,
    System,
    Tools,
    Navigation
}

/// <summary>
/// One command reachable under a modifier combination.
/// </summary>
/// <param name="Group">Decides the colour; related commands share one.</param>
/// <param name="Label">
/// What the command does, in plain words — "Duplicate layer", not "Ctrl+J". The LEDs cannot
/// show text, so this costs nothing on the hardware. It exists because the preview inside the
/// application can show it, and because a shipped profile with eighty entries is otherwise
/// impossible to check: <c>"j" → Edit</c> on its own says nothing about whether it is right.
/// </param>
public sealed record Shortcut(FunctionGroup Group, string? Label = null)
{
    public override string ToString() => Label is null ? Group.ToString() : $"{Label} ({Group})";
}

/// <summary>
/// Which keys carry a command under one particular modifier combination.
/// </summary>
/// <param name="Characters">
/// Shortcuts identified by the character the key types, lower case — <c>"z"</c> for undo.
/// </param>
/// <param name="Keys">
/// Shortcuts identified by key position, for keys that type nothing: Escape, Tab, the arrows.
/// </param>
/// <remarks>
/// The split exists because Ctrl+Z means "the key that types z", not "the key sitting where a
/// US keyboard has Z". On a German layout those are different keys — undo and redo would be
/// shown swapped — and on Dvorak almost every letter shortcut would land somewhere else.
/// Keys that produce no character have no such ambiguity and are addressed by position.
/// </remarks>
public sealed record ShortcutSet(
    IReadOnlyDictionary<string, Shortcut> Characters,
    IReadOnlyDictionary<string, Shortcut> Keys)
{
    public static ShortcutSet Empty { get; } = new(
        new Dictionary<string, Shortcut>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, Shortcut>(StringComparer.Ordinal));

    /// <summary>Looks up by key position.</summary>
    public bool TryGet(string keyId, out Shortcut shortcut) => Keys.TryGetValue(keyId, out shortcut!);

    /// <summary>Looks up by the character a key types.</summary>
    public bool TryGetByCharacter(string? character, out Shortcut shortcut)
    {
        if (string.IsNullOrEmpty(character))
        {
            shortcut = null!;
            return false;
        }

        return Characters.TryGetValue(character, out shortcut!);
    }

    /// <summary>Total number of assignments, for the interface.</summary>
    public int Count => Characters.Count + Keys.Count;
}

/// <summary>
/// All shortcut sets, looked up by the modifiers currently held. Because lookup is by
/// combination rather than by a hardcoded list of layers, adding a new one — say Ctrl+Shift+Alt —
/// is a data change and needs no code.
/// </summary>
public sealed class ShortcutCatalogue
{
    private readonly IReadOnlyDictionary<ModifierKeys, ShortcutSet> _sets;

    public ShortcutCatalogue(IReadOnlyDictionary<ModifierKeys, ShortcutSet> sets)
    {
        _sets = sets ?? throw new ArgumentNullException(nameof(sets));
    }

    /// <summary>All sets, for editing and saving.</summary>
    public IReadOnlyDictionary<ModifierKeys, ShortcutSet> Sets => _sets;

    /// <summary>
    /// Finds the set for the given state. Sides are collapsed first — left and right Ctrl mean
    /// the same thing for a shortcut — while Shift is kept, because Ctrl+Shift is its own set.
    /// </summary>
    public bool TryGetSet(KeyboardState state, out ShortcutSet set)
        => _sets.TryGetValue(Normalise(state), out set!);

    /// <summary>
    /// Returns a catalogue with one set replaced. Application profiles use this; it returns a
    /// new instance rather than mutating, because profiles come and go as the foreground
    /// window changes and a shared mutable catalogue would leak between them.
    /// </summary>
    public ShortcutCatalogue WithOverride(ModifierKeys modifiers, ShortcutSet set)
    {
        ArgumentNullException.ThrowIfNull(set);

        var copy = new Dictionary<ModifierKeys, ShortcutSet>(_sets) { [modifiers] = set };

        return new ShortcutCatalogue(copy);
    }

    /// <summary>
    /// Returns a catalogue with several sets replaced at once.
    /// </summary>
    /// <remarks>
    /// Layers the profile does not mention are kept rather than dropped. An application profile
    /// says what Ctrl means <em>in that program</em>; it has nothing to say about Win+E, which
    /// Windows assigns and which stays true whatever is in front. Replacing the whole catalogue
    /// would blank those out.
    /// </remarks>
    public ShortcutCatalogue WithOverrides(IReadOnlyDictionary<ModifierKeys, ShortcutSet> overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);

        if (overrides.Count == 0)
        {
            return this;
        }

        var copy = new Dictionary<ModifierKeys, ShortcutSet>(_sets);

        foreach (var (modifiers, set) in overrides)
        {
            copy[modifiers] = copy.TryGetValue(modifiers, out var underneath)
                ? Layer(underneath, set)
                : set;
        }

        return new ShortcutCatalogue(copy);
    }

    /// <summary>
    /// One set laid over another: every key the upper one names wins, the rest show through.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Laid over, never replacing, and the difference is easy to miss. A profile naming Ctrl does
    /// not mean "Ctrl means nothing else here" — it means "Ctrl also means this". Replacing the
    /// layer throws away everything the profile happens not to repeat, and what that costs most
    /// often is the clipboard: Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+Z, Ctrl+Y and Ctrl+A go dark in a
    /// browser, in Slack, in Teams, in a terminal — programs one does nothing but type and paste
    /// in. Twenty-nine of the fifty-one shipped profiles would lose them; the twenty-two that
    /// would not had simply repeated those six entries by hand.
    /// </para>
    /// <para>
    /// Layering keeps the ability to <em>change</em> a meaning, which is what a profile is for:
    /// name the key and the new meaning wins. It only removes the ability to blank a layer
    /// wholesale, which nothing wanted.
    /// </para>
    /// </remarks>
    private static ShortcutSet Layer(ShortcutSet underneath, ShortcutSet over)
    {
        var characters = new Dictionary<string, Shortcut>(
            underneath.Characters, StringComparer.OrdinalIgnoreCase);

        foreach (var (character, shortcut) in over.Characters)
        {
            characters[character] = shortcut;
        }

        var keys = new Dictionary<string, Shortcut>(underneath.Keys, StringComparer.Ordinal);

        foreach (var (id, shortcut) in over.Keys)
        {
            keys[id] = shortcut;
        }

        return new ShortcutSet(characters, keys);
    }

    /// <summary>
    /// Collapses left/right into a single canonical flag per modifier. AltGr is deliberately
    /// mapped to <see cref="ModifierKeys.RightAlt"/> alone, dropping the Ctrl that Windows
    /// synthesises alongside it — otherwise every AltGr press would look like Ctrl+Alt.
    /// </summary>
    public static ModifierKeys Normalise(KeyboardState state)
    {
        var result = ModifierKeys.None;

        if (state.AltGr)
        {
            result |= ModifierKeys.RightAlt;
        }
        else
        {
            if (state.Ctrl)
            {
                result |= ModifierKeys.LeftCtrl;
            }

            if (state.Alt)
            {
                result |= ModifierKeys.LeftAlt;
            }
        }

        if (state.Win)
        {
            result |= ModifierKeys.LeftWin;
        }

        if (state.Shift)
        {
            result |= ModifierKeys.LeftShift;
        }

        return result;
    }
}
