using Keylegend.Core.Devices;
using Keylegend.Core.Input;
using Keylegend.Core.Profiles;
using Keylegend.Core.Shortcuts;

namespace Keylegend.Core.Lighting;

/// <summary>
/// Turns a keyboard state into a picture. This is the heart of the program, and deliberately
/// a pure calculation: given the same inputs it always produces the same frame, with no clock,
/// no network and no hardware involved. That is what lets the on-screen preview and the real
/// keyboard be driven by the same code.
/// </summary>
public sealed class FrameComposer
{
    private readonly DeviceProfile _profile;
    private readonly IKeyResolver _resolver;

    public FrameComposer(DeviceProfile profile, IKeyResolver resolver)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <summary>Creates a frame sized for this device.</summary>
    public LedFrame CreateFrame() => new(_profile.Matrix.Rows, _profile.Matrix.Columns);

    /// <summary>
    /// Fills <paramref name="target"/> according to the three rules, in order of precedence:
    /// lock states first, then the modifier layer, then the plain category colouring.
    /// </summary>
    /// <param name="profile">
    /// Application profile in effect, or <c>null</c> for the default behaviour. Its key
    /// highlights take precedence over the category colours but not over the lock display or a
    /// modifier layer.
    /// </param>
    public void Compose(
        LedFrame target,
        KeyboardState state,
        ColourScheme scheme,
        ShortcutCatalogue shortcuts,
        ApplicationProfile? profile = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(shortcuts);

        target.Clear();

        // A profile may replace individual shortcut layers - what Ctrl means inside Photoshop.
        // Layers it does not mention keep the general set, because Win+E is assigned by Windows
        // and stays true whatever is in front.
        var effectiveShortcuts = profile?.ApplyShortcuts(shortcuts) ?? shortcuts;

        // Only looked up once per frame rather than per key.
        var hasSet = effectiveShortcuts.TryGetSet(state, out var shortcutSet);

        // Whether to show only the keys that carry a command. Two ways to get there, and the
        // second is what lets a program define its own layers.
        //
        // Ctrl, Alt, Win and AltGr filter whether or not anything is assigned under them: holding
        // Ctrl with nothing bound is a dark keyboard, which is the honest answer. Shift does not,
        // because Shift normally changes which character a key types — a keyboard that went dark
        // on Shift would lose the uppercase display for nothing.
        //
        // But Shift is only "not a modifier" for text. For functions it plainly is one, and games
        // use it that way — sprint, walk, secondary fire — as they use the bare keys, where WASD
        // means direction rather than letters. So a layer that somebody has actually defined
        // filters too. Nothing changes by default, because no shipped layer is keyed on Shift or
        // on no modifier at all; an application profile that defines one turns it into a function
        // layer for as long as that program is in front.
        var filtering = state.HasFilteringModifier || hasSet;

        foreach (var key in _profile.Keys)
        {
            if (key.Row is not { } row || key.Column is not { } column)
            {
                // No LED mapped yet - calibration has not covered this key.
                continue;
            }

            var colour = ColourFor(key, state, scheme, shortcutSet, filtering, profile);

            target.Set(row, column, colour.Scale(scheme.Brightness));
        }
    }

    private RgbColor ColourFor(
        KeyDefinition key,
        KeyboardState state,
        ColourScheme scheme,
        ShortcutSet? shortcutSet,
        bool filtering,
        ApplicationProfile? profile)
    {
        // Rule 1: lock keys always show their own state, even underneath a filtering modifier.
        // Losing sight of Caps Lock just because Ctrl is held would defeat the purpose.
        if (LockColourFor(key.Id, state, scheme) is { } lockColour)
        {
            return lockColour;
        }

        // Rule 2: a filtering modifier is held, so only assigned keys light up.
        if (filtering)
        {
            // AltGr shows the character layer; the others show their shortcut set.
            if (state.AltGr)
            {
                var meaning = _resolver.Resolve(key.Id, key.ScanCode, state);

                // Only keys that actually type something count here. A key with no AltGr
                // assignment reports "no character", which is indistinguishable from what a
                // control key reports - so accepting anything "assigned" would light the whole
                // keyboard rather than the handful of keys the layer exists to show.
                return meaning.ProducesCharacter ? scheme.For(meaning.Category) : RgbColor.Off;
            }

            return ShortcutGroupFor(key, state, shortcutSet) is { } group
                ? scheme.For(group)
                : RgbColor.Off;
        }

        // Rule 3: an application profile may pin certain keys to a fixed colour. While playing
        // a game, where your hands go matters and which letter a key types does not, so WASD
        // outranks the typing categories - but never the lock display or a modifier layer,
        // which is why this sits here rather than at the top.
        if (profile is not null && profile.Highlights.TryGetValue(key.Id, out var highlight))
        {
            return highlight.Colour;
        }

        // Rule 4: colour by what the key produces right now. Shift, Caps Lock and Num Lock
        // need no special case here - they change the character, and the category follows.
        // Function keys are the one exception: they produce nothing, so there is no character
        // to classify and their category comes from the key's identity instead.
        if (KeyRoles.StructuralCategory(key.Id) is { } structural)
        {
            return scheme.For(structural);
        }

        var resolved = _resolver.Resolve(key.Id, key.ScanCode, state);

        return scheme.For(resolved.Category);
    }

    /// <summary>
    /// Finds the function group for a key on a shortcut layer, or <c>null</c> if it carries no
    /// command there.
    /// </summary>
    /// <remarks>
    /// Two lookups, in this order. Position first, for keys that type nothing — Escape, Tab,
    /// the arrows. Then by the character the key types <em>unmodified</em>, because Ctrl+Z means
    /// "the key that types z". Asking with the modifiers applied would be wrong: Ctrl turns
    /// letters into control characters, so nothing would ever match.
    /// </remarks>
    private FunctionGroup? ShortcutGroupFor(KeyDefinition key, KeyboardState state, ShortcutSet? set)
    {
        if (set is null)
        {
            return null;
        }

        if (set.TryGet(key.Id, out var byPosition))
        {
            return byPosition.Group;
        }

        var plain = new KeyboardState(
            ModifierKeys.None,
            new LockStates(state.Locks.NumLock, CapsLock: false, ScrollLock: false));

        var meaning = _resolver.Resolve(key.Id, key.ScanCode, plain);

        return set.TryGetByCharacter(meaning.Character, out var byCharacter) ? byCharacter.Group : null;
    }

    private static RgbColor? LockColourFor(string keyId, KeyboardState state, ColourScheme scheme)
        => keyId switch
        {
            "Keyboard_NumLock" => state.Locks.NumLock ? scheme.NumLock.On : scheme.NumLock.Off,
            "Keyboard_CapsLock" => state.Locks.CapsLock ? scheme.CapsLock.On : scheme.CapsLock.Off,
            "Keyboard_ScrollLock" => state.Locks.ScrollLock ? scheme.ScrollLock.On : scheme.ScrollLock.Off,
            _ => null
        };
}
