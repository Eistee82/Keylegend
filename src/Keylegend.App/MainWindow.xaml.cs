using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Keylegend.App.Localisation;
using Keylegend.App.Views;
using Keylegend.Core.Configuration;
using Keylegend.Core.Devices;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Profiles;
using Keylegend.Core.Session;
using Keylegend.Core.Shortcuts;
using Keylegend.Engine;
using Keylegend.Windows;

// WPF has a ModifierKeys of its own, so ours is aliased rather than imported.
using Modifiers = Keylegend.Core.Input.ModifierKeys;

namespace Keylegend.App;

public partial class MainWindow : Window
{
    private readonly LightingEngine _engine;
    private readonly IKeyResolver _resolver;
    private readonly ConfigStore _store;
    private readonly ProfileLibrary _library;

    private readonly List<ProfileRow> _rows = [];
    private ProfileEntry? _entry;
    private RgbColor _highlightColour = new(255, 0, 0);
    private bool _loading = true;
    private LanguageChoice _language = LanguageChoice.Automatic;

    /// <summary>
    /// The configured quiet period. Kept apart from the engine's timeout, which holds
    /// <see cref="SessionManager.Never"/> when the hand-back is switched off and is therefore
    /// not a number of seconds the box can show.
    /// </summary>
    private TimeSpan _idlePeriod = TimeSpan.FromSeconds(60);

    /// <summary>Set while the profile list is being rebuilt, so its selection events are ignored.</summary>
    private bool _rebuilding;

    public MainWindow(
        LightingEngine engine,
        IKeyResolver resolver,
        ConfigStore store,
        ProfileLibrary library,
        LanguageChoice language,
        TimeSpan idlePeriod,
        bool handBackWhenIdle,
        string? loadProblem)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _library = library ?? throw new ArgumentNullException(nameof(library));

        InitializeComponent();

        // Before anything is filled in, so the lists built below are already in the chosen
        // language rather than in the one Windows happens to be set to.
        ShowLanguage(language);

        var device = engine.Profile;
        var labels = BuildLabels(device);

        Preview.Profile = device;
        Preview.SetLabels(labels);
        ProfilePreview.Profile = device;
        ProfilePreview.SetLabels(labels);
        ProfilePreview.KeyClicked += OnProfileKeyClicked;
        ProfilePreview.KeyRightClicked += OnProfileKeyCleared;
        ProfilePreview.KeyHovered += OnProfileKeyHovered;

        ShortcutPreview.Profile = device;
        ShortcutPreview.SetLabels(labels);
        ShortcutPreview.KeyClicked += OnShortcutKeyClicked;
        ShortcutPreview.KeyRightClicked += OnShortcutKeyCleared;
        ShortcutPreview.KeyHovered += OnShortcutKeyHovered;

        DeviceName.Text = device.Name;
        ShowDevice(device);

        ConfigPathLabel.Text = store.Path;

        BuildColourEditors();
        BuildShortcutEditor();
        BuildProfileShortcutEditor();
        RefreshProfileList();

        // An empty editor beside a full list only invites the question of what to click first.
        ProfileList.SelectedIndex = 0;

        // The period and the hand-back switch are held apart, so that switching the hand-back
        // off and on again returns the period the user set rather than the default.
        _idlePeriod = idlePeriod;
        HandBackBox.IsChecked = handBackWhenIdle;
        IdleBox.Text = ((int)_idlePeriod.TotalSeconds).ToString(CultureInfo.CurrentCulture);
        IdleBox.IsEnabled = handBackWhenIdle;
        IdleSecondsLabel.Opacity = handBackWhenIdle ? 1.0 : 0.5;
        BrightnessSlider.Value = engine.Settings.Scheme.Brightness;
        BrightnessLabel.Text = $"{engine.Settings.Scheme.Brightness:P0}";
        ProfilesEnabledBox.IsChecked = engine.Settings.UseApplicationProfiles;
        AutostartBox.IsChecked = Autostart.IsEnabled();
        UpdateHighlightSwatch();

        // Hardware and preview are filled by the same composer, so mirroring is a matter of
        // forwarding the frame - there is no second rendering path to keep in step.
        engine.FrameComposed += OnFrameComposed;
        engine.StateChanged += OnStateChanged;
        engine.Fault += OnFault;
        engine.ProfileChanged += OnProfileChanged;

        UpdateStateLabel(engine.State);
        ShowIdlePreview();

        _loading = false;

        if (!string.IsNullOrEmpty(loadProblem))
        {
            StatusLine.Text = loadProblem;
        }
    }

    /// <summary>
    /// The line under the device name. The layout and the key count come from the device, so
    /// only the words around them are localised.
    /// </summary>
    /// <remarks>
    /// It used to say whether the mapping had been calibrated on hardware. That distinction has
    /// gone: the matrix positions now come from the lighting protocol's own table, which is the
    /// same for every model, so there is nothing left for a calibration to confirm.
    /// </remarks>
    private void ShowDevice(DeviceProfile device)
        => DeviceDetail.Text = Texts.Get("StatusDevice", device.PhysicalLayout, device.Keys.Count);

    /// <summary>
    /// Rebuilds everything whose text was put together in code rather than bound.
    /// </summary>
    /// <remarks>
    /// A binding re-reads itself when the language changes; a string assigned to a property does
    /// not. Rather than turning every such label into a binding, the handful of places that
    /// compose text are simply run again.
    /// </remarks>
    private void ShowTexts()
    {
        ShowDevice(_engine.Profile);
        UpdateStateLabel(_engine.State);
        OnProfileChanged(_engine.ActiveProfile);
        BuildColourEditors();
        RefreshProfileList();
        RefreshShortcutPreview();
    }

    // ---------------------------------------------------------------- labels

    /// <summary>
    /// Works out what is printed on each key.
    /// </summary>
    /// <remarks>
    /// Three sources, in order. A label from the device profile wins — the text on a keycap
    /// belongs to the keyboard, not to the program's language, so a German board says "Strg"
    /// whatever language the interface speaks. Otherwise the characters the key types are asked
    /// from the active layout, in all three positions a keycap carries them. Only if neither
    /// applies does a fallback name appear, which should be rare and signals a profile worth
    /// improving.
    /// </remarks>
    private Dictionary<string, KeyLegend> BuildLabels(DeviceProfile profile)
    {
        var plain = new KeyboardState(Modifiers.None, new LockStates(true, false, false));
        var shift = new KeyboardState(Modifiers.LeftShift, new LockStates(true, false, false));
        var altGr = new KeyboardState(
            Modifiers.RightAlt | Modifiers.LeftCtrl, new LockStates(true, false, false));

        var legends = new Dictionary<string, KeyLegend>(StringComparer.Ordinal);

        foreach (var key in profile.Keys)
        {
            // A profile label wins for the main legend; the layout supplies it otherwise.
            var fromLayout = Printable(_resolver.Resolve(key.Id, key.ScanCode, plain));

            var main = key.Label
                ?? (fromLayout is null
                    ? FallbackName(key.Id)
                    // Letters are printed capitalised on a keycap, whatever the key types when
                    // pressed. Only letters: digits and symbols have no case, and eszett has no
                    // capital form on a German keyboard either.
                    : char.IsLetter(fromLayout[0]) ? fromLayout.ToUpperInvariant() : fromLayout);

            // A second line from the profile - "s-abf" under "druck", or the navigation name
            // under a number pad digit - takes the place the shifted character would occupy.
            if (!string.IsNullOrEmpty(key.LabelSecondary))
            {
                legends[key.Id] = new KeyLegend(main, key.LabelSecondary);
                continue;
            }

            if (key.Label is not null || fromLayout is null)
            {
                legends[key.Id] = new KeyLegend(main);
                continue;
            }

            var plainMeaning = _resolver.Resolve(key.Id, key.ScanCode, plain);
            var shifted = Printable(_resolver.Resolve(key.Id, key.ScanCode, shift));
            var third = Printable(_resolver.Resolve(key.Id, key.ScanCode, altGr));

            // Letters differ only in case, and printing "a" over "A" would be noise; the
            // keyboard itself prints one letter for the same reason.
            var secondary =
                string.Equals(shifted, fromLayout, StringComparison.OrdinalIgnoreCase) ? null : shifted;

            legends[key.Id] = new KeyLegend(main, secondary, third, SideBySide(
                plainMeaning, fromLayout, secondary, third));
        }

        return legends;
    }

    /// <summary>
    /// Whether a key's two characters are printed next to each other rather than stacked.
    /// </summary>
    /// <remarks>
    /// The punctuation keys are: <c>, ;</c> and <c>. :</c> and <c>- _</c> and <c># '</c>.
    /// Presumably because a comma stacked above a semicolon is barely distinguishable at that
    /// size, whereas a digit above its shifted symbol reads fine. Dead keys are excluded — the
    /// circumflex and acute are stacked on the real keyboard despite also being punctuation.
    /// </remarks>
    private static bool SideBySide(KeyMeaning plain, string? main, string? shifted, string? altGr)
    {
        if (main is null || shifted is null || altGr is not null)
        {
            return false;      // needs exactly two characters
        }

        if (plain.Category == KeyCategory.DeadKey)
        {
            return false;
        }

        return !char.IsLetterOrDigit(main[0]) && !char.IsLetterOrDigit(shifted[0]);
    }

    private static string? Printable(KeyMeaning meaning)
        => meaning.ProducesCharacter && !string.IsNullOrWhiteSpace(meaning.Character)
            ? meaning.Character
            : null;

    /// <summary>
    /// Last resort for a key that types nothing and carries no label in the profile. Kept
    /// deliberately plain — a profile should name such keys itself, in the language printed on
    /// the hardware.
    /// </summary>
    private static string FallbackName(string keyId)
    {
        var name = keyId.StartsWith("Keyboard_", StringComparison.Ordinal)
            ? keyId["Keyboard_".Length..]
            : keyId;

        return name switch
        {
            "Space" => string.Empty,
            "ArrowUp" => "↑",
            "ArrowDown" => "↓",
            "ArrowLeft" => "←",
            "ArrowRight" => "→",
            _ => name
        };
    }

    // ---------------------------------------------------------------- engine events

    private void OnFrameComposed(LedFrame frame) =>
        Dispatcher.BeginInvoke(() =>
        {
            // Only mirror the hardware while no layer is being inspected, otherwise the live
            // keyboard would keep overwriting what the user asked to see.
            if (_engine.Settings.OverrideState is null)
            {
                Preview.Update(frame);
            }
        });

    private void OnStateChanged(LightingState state) =>
        Dispatcher.BeginInvoke(() => UpdateStateLabel(state));

    /// <summary>
    /// Says what is wrong with the lighting, and takes it back when it is not wrong any more.
    /// </summary>
    /// <remarks>
    /// The status line is styled as an aside, which is right for what it usually carries and wrong
    /// for this: a keyboard that has gone dark while the program still looks fine is exactly the
    /// case a user cannot diagnose. So a fault switches the line to amber, and clearing it puts the
    /// line back rather than leaving a stale complaint behind.
    /// </remarks>
    private void OnFault(string? message) =>
        Dispatcher.BeginInvoke(() =>
        {
            StatusLine.Text = message ?? string.Empty;
            StatusLine.Style = (Style)FindResource(message is null ? "Muted" : "Trouble");
        });

    private void OnProfileChanged(ApplicationProfile? profile) =>
        Dispatcher.BeginInvoke(() =>
        {
            ProfileLabel.Text = profile is null
                ? string.Empty
                : Texts.Get("StatusProfileLabel", profile.Name);

            // A layer being inspected is composed with whichever profile applies, so when that
            // changes the drawing is out of date. Without this the header names one profile
            // while the keyboard beside it still shows another's layer — two statements in the
            // same window that contradict each other, and the one in bigger type is the wrong
            // one. Only while a layer is held: with none, the live frames already carry it.
            if (_engine.Settings.OverrideState is { } state)
            {
                Preview.Update(_engine.Preview(state, profile));
                Preview.SetTips(ShortcutTips(state.Modifiers, profile));
            }
        });

    private void UpdateStateLabel(LightingState state)
    {
        // Short by design: this label sits on one line beside the pause button, and a long
        // sentence here would widen the whole window.
        StateLabel.Text = state switch
        {
            LightingState.Active => Texts.Get("StatusPainting"),
            LightingState.Idle => Texts.Get("StatusIdle"),
            LightingState.Paused => Texts.Get("StatusPaused"),
            _ => string.Empty
        };

        StateLabel.ToolTip = state switch
        {
            LightingState.Active => Texts.Get("StatusActiveTooltip"),
            LightingState.Idle => Texts.Get("StatusIdleTooltip"),
            LightingState.Paused => Texts.Get("StatusPausedTooltip"),
            _ => null
        };

        PauseButton.Content = Texts.Get(
            state == LightingState.Paused ? "StatusResumeButton" : "StatusPauseButton");
    }

    private void OnPauseClicked(object sender, RoutedEventArgs e)
    {
        if (_engine.State == LightingState.Paused)
        {
            _engine.Resume();
        }
        else
        {
            _engine.Pause();
        }

        UpdateStateLabel(_engine.State);
    }

    // ---------------------------------------------------------------- layers

    /// <summary>
    /// Guards against reacting to each toggle individually while several are being reset, which
    /// would otherwise push seven intermediate layers at the keyboard on the way to none.
    /// </summary>
    private bool _clearingLayers;

    private void OnLayerChanged(object sender, RoutedEventArgs e)
    {
        if (_clearingLayers)
        {
            return;
        }

        ApplyLayer();
    }

    private void OnFollowClicked(object sender, RoutedEventArgs e)
    {
        _clearingLayers = true;
        try
        {
            foreach (var toggle in new[]
                     {
                         ShiftToggle, AltGrToggle, CtrlToggle, AltToggle, WinToggle,
                         NumToggle, CapsToggle, ScrollToggle
                     })
            {
                toggle.IsChecked = false;
            }
        }
        finally
        {
            _clearingLayers = false;
        }

        ApplyLayer();
    }

    private void ApplyLayer()
    {
        var modifiers = Modifiers.None;

        if (ShiftToggle.IsChecked == true) { modifiers |= Modifiers.LeftShift; }
        if (CtrlToggle.IsChecked == true) { modifiers |= Modifiers.LeftCtrl; }
        if (AltToggle.IsChecked == true) { modifiers |= Modifiers.LeftAlt; }
        if (WinToggle.IsChecked == true) { modifiers |= Modifiers.LeftWin; }

        // AltGr is Ctrl plus right Alt as far as Windows is concerned, and the state type
        // relies on that to tell it from a real Ctrl+Alt.
        if (AltGrToggle.IsChecked == true)
        {
            modifiers |= Modifiers.RightAlt | Modifiers.LeftCtrl;
        }

        var anyLayer = modifiers != Modifiers.None
            || NumToggle.IsChecked == true
            || CapsToggle.IsChecked == true
            || ScrollToggle.IsChecked == true;

        if (!anyLayer)
        {
            _engine.Settings = _engine.Settings with { OverrideState = null };

            // The keyboard is followed live again, so any layer's labels would be stale.
            Preview.SetTips(new Dictionary<string, string>());

            StatusLine.Text = Texts.Get("StatusFollowing");
            ShowIdlePreview();
            return;
        }

        var state = new KeyboardState(
            modifiers,
            new LockStates(
                NumToggle.IsChecked == true,
                CapsToggle.IsChecked == true,
                ScrollToggle.IsChecked == true));

        _engine.Settings = _engine.Settings with { OverrideState = state };
        Preview.Update(_engine.Preview(state));
        Preview.SetTips(ShortcutTips(modifiers, _engine.ActiveProfile));
        StatusLine.Text = Texts.Get("StatusShowingLayer");
    }

    private void ShowIdlePreview()
    {
        var state = _engine.Settings.OverrideState
            ?? new KeyboardState(Modifiers.None, new LockStates(true, false, false));

        Preview.Update(_engine.Preview(state));
    }

    // ---------------------------------------------------------------- colours

    private readonly Dictionary<ColourRow, Action<RgbColor>> _colourSetters = [];

    private void BuildColourEditors()
    {
        _colourSetters.Clear();

        var categories = new List<ColourRow>();
        foreach (var category in Enum.GetValues<KeyCategory>())
        {
            if (category == KeyCategory.Unassigned)
            {
                continue;      // always off; there is nothing to choose
            }

            var row = new ColourRow(
                Describe(category),
                _engine.Settings.Scheme.For(category),
                Example(category));

            _colourSetters[row] = colour => UpdateScheme(s =>
            {
                var map = new Dictionary<KeyCategory, RgbColor>(s.Categories) { [category] = colour };
                return s with { Categories = map };
            });

            row.Picked += OnColourRowPicked;
            categories.Add(row);
        }

        CategoryList.ItemsSource = categories;

        var groups = new List<ColourRow>();
        foreach (var group in Enum.GetValues<FunctionGroup>())
        {
            var row = new ColourRow(group.ToString(), _engine.Settings.Scheme.For(group));

            _colourSetters[row] = colour => UpdateScheme(s =>
            {
                var map = new Dictionary<FunctionGroup, RgbColor>(s.Groups) { [group] = colour };
                return s with { Groups = map };
            });

            row.Picked += OnColourRowPicked;
            groups.Add(row);
        }

        GroupList.ItemsSource = groups;

        var locks = new List<ColourRow>();

        AddLockRow("ColoursLockNumOn", s => s.NumLock.On, (s, c) => s with { NumLock = s.NumLock with { On = c } });
        AddLockRow("ColoursLockNumOff", s => s.NumLock.Off, (s, c) => s with { NumLock = s.NumLock with { Off = c } });
        AddLockRow("ColoursLockCapsOn", s => s.CapsLock.On, (s, c) => s with { CapsLock = s.CapsLock with { On = c } });
        AddLockRow("ColoursLockCapsOff", s => s.CapsLock.Off, (s, c) => s with { CapsLock = s.CapsLock with { Off = c } });
        AddLockRow("ColoursLockScrollOn", s => s.ScrollLock.On, (s, c) => s with { ScrollLock = s.ScrollLock with { On = c } });
        AddLockRow("ColoursLockScrollOff", s => s.ScrollLock.Off, (s, c) => s with { ScrollLock = s.ScrollLock with { Off = c } });

        LockList.ItemsSource = locks;

        void AddLockRow(string key, Func<ColourScheme, RgbColor> read, Func<ColourScheme, RgbColor, ColourScheme> write)
        {
            var row = new ColourRow(Texts.Get(key), read(_engine.Settings.Scheme));
            _colourSetters[row] = colour => UpdateScheme(s => write(s, colour));
            row.Picked += OnColourRowPicked;
            locks.Add(row);
        }
    }

    private static string Describe(KeyCategory category) => category switch
    {
        KeyCategory.Digit => Texts.Get("ColoursCategoryDigit"),
        KeyCategory.Lowercase => Texts.Get("ColoursCategoryLowercase"),
        KeyCategory.Uppercase => Texts.Get("ColoursCategoryUppercase"),
        KeyCategory.Symbol => Texts.Get("ColoursCategorySymbol"),
        KeyCategory.Control => Texts.Get("ColoursCategoryControl"),
        KeyCategory.DeadKey => Texts.Get("ColoursCategoryDeadKey"),
        KeyCategory.FunctionKey => Texts.Get("ColoursCategoryFunctionKey"),
        _ => category.ToString()
    };

    private static string? Example(KeyCategory category) => category switch
    {
        KeyCategory.Digit => Texts.Get("ColoursExampleDigit"),
        KeyCategory.Lowercase => Texts.Get("ColoursExampleLowercase"),
        KeyCategory.Uppercase => Texts.Get("ColoursExampleUppercase"),
        KeyCategory.Symbol => Texts.Get("ColoursExampleSymbol"),
        KeyCategory.Control => Texts.Get("ColoursExampleControl"),
        KeyCategory.DeadKey => Texts.Get("ColoursExampleDeadKey"),
        KeyCategory.FunctionKey => Texts.Get("ColoursExampleFunctionKey"),
        _ => null
    };

    private void OnColourRowPicked(ColourRow row)
    {
        if (!_colourSetters.TryGetValue(row, out var apply))
        {
            return;
        }

        if (ColourPicker.Choose(row.Colour) is not { } chosen)
        {
            return;
        }

        row.Show(chosen);
        apply(chosen);
    }

    private void UpdateScheme(Func<ColourScheme, ColourScheme> change)
    {
        _engine.Settings = _engine.Settings with { Scheme = change(_engine.Settings.Scheme) };
        ShowIdlePreview();
        RefreshProfilePreview();
        Save();
    }

    private void OnResetColoursClicked(object sender, RoutedEventArgs e)
    {
        _engine.Settings = _engine.Settings with
        {
            Scheme = ColourScheme.Default with { Brightness = _engine.Settings.Scheme.Brightness }
        };

        BuildColourEditors();
        ShowIdlePreview();
        Save();
        StatusLine.Text = Texts.Get("StatusColoursRestored");
    }

    // ---------------------------------------------------------------- profiles

    /// <summary>Which of a profile's two editable sections the keyboard below is showing.</summary>
    private enum EditMode
    {
        Highlights,
        Shortcuts
    }

    private EditMode _mode = EditMode.Highlights;
    private Modifiers _profileLayer = Modifiers.LeftCtrl;
    private FunctionGroup _profileGroup = FunctionGroup.File;
    private readonly List<ToggleButton> _profileGroupButtons = [];
    private Dictionary<string, string> _profileTips = [];
    private string _profileSummary = string.Empty;

    /// <summary>
    /// One line in the profile list: the entry plus what the list has to show about it.
    /// </summary>
    /// <remarks>
    /// Provenance is on every row rather than only in the editor because the point of the list
    /// is choosing: "shipped" and "shipped · edited" are the difference between a profile that
    /// still improves with the next build and one that is now the user's own.
    /// </remarks>
    public sealed class ProfileRow
    {
        public required ProfileEntry Entry { get; init; }

        /// <summary>Heading this row sits under — applications and games are separate lists.</summary>
        public required string Group { get; init; }

        public required string Detail { get; init; }

        /// <summary>Hidden profiles stay in the list, set back rather than removed from sight.</summary>
        public required double Dim { get; init; }

        public string Name => Entry.Profile.Name;

        public string? Tip => Entry.Profile.Match.Processes.Count == 0
            ? null
            : string.Join(", ", Entry.Profile.Match.Processes);

        /// <summary>What a screen reader announces for the row; the template shows more.</summary>
        public override string ToString() => Name;
    }

    private void RefreshProfileList()
    {
        var wanted = _entry?.Id;
        var search = ProfileSearchBox.Text.Trim();

        _rows.Clear();

        foreach (var entry in _library.Entries
                     .OrderBy(e => e.Profile.Kind)
                     .ThenBy(e => e.Profile.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            if (!MatchesSearch(entry, search))
            {
                continue;
            }

            _rows.Add(new ProfileRow
            {
                Entry = entry,
                Group = Texts.Get(entry.Profile.Kind == ProfileKind.Game
                    ? "ProfilesGroupGames"
                    : "ProfilesGroupApps"),
                Detail = DescribeOrigin(entry),
                Dim = entry.Hidden ? 0.45 : 1.0
            });
        }

        var view = new ListCollectionView(_rows);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ProfileRow.Group)));

        _rebuilding = true;
        ProfileList.ItemsSource = view;
        ProfileList.SelectedItem = _rows.FirstOrDefault(r => string.Equals(r.Entry.Id, wanted, StringComparison.Ordinal));
        _rebuilding = false;

        if (ProfileList.SelectedItem is not null)
        {
            // A profile just added lands somewhere in eighty alphabetical entries, and the
            // editor beside a list scrolled elsewhere reads as the wrong profile.
            ProfileList.ScrollIntoView(ProfileList.SelectedItem);
        }

        // The entry is a snapshot, so the edit that prompted the rebuild has to be picked up
        // again here - otherwise the editor would keep showing the state before it.
        _entry = ProfileList.SelectedItem is ProfileRow row
            ? row.Entry
            : wanted is null ? null : _library.Find(wanted);

        ShowEntry();
    }

    /// <summary>Matches on name and on program names — people look for "chrome", not for "Google Chrome".</summary>
    private static bool MatchesSearch(ProfileEntry entry, string search)
        => search.Length == 0
            || entry.Profile.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)
            || entry.Profile.Match.Processes.Any(p => p.Contains(search, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Where a profile came from, and whether it is currently hidden.
    /// </summary>
    /// <remarks>
    /// The hidden note is a placeholder in the resource rather than a suffix glued on here: a
    /// translation is free to put it first, or to say it differently.
    /// </remarks>
    private static string DescribeOrigin(ProfileEntry entry)
    {
        var origin = entry.Source == ProfileSource.User
            ? Texts.Get("ProfilesOriginUser")
            : entry.Overridden.Count > 0 || entry.Renamed
                ? Texts.Get("ProfilesOriginShippedEdited")
                : Texts.Get("ProfilesOriginShipped");

        return entry.Hidden ? Texts.Get("ProfilesOriginHidden", origin) : origin;
    }

    private void OnProfileSearchChanged(object sender, TextChangedEventArgs e)
    {
        ProfileSearchHint.Visibility = ProfileSearchBox.Text.Length == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (!_loading)
        {
            RefreshProfileList();
        }
    }

    private void OnProfileSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_rebuilding)
        {
            return;
        }

        _entry = (ProfileList.SelectedItem as ProfileRow)?.Entry;
        ShowEntry();
    }

    /// <summary>Fills the editor from the selected entry, including what may be reset.</summary>
    private void ShowEntry()
    {
        var entry = _entry;

        ProfileEditor.IsEnabled = entry is not null;
        ResetAllButton.IsEnabled = entry?.CanReset == true;

        ShowSection(ProfileSection.Match, MatchStateLabel, ResetMatchButton);
        ShowSection(ProfileSection.Highlights, HighlightStateLabel, ResetHighlightsButton);
        ShowSection(ProfileSection.Shortcuts, ShortcutStateLabel, ResetShortcutsButton);

        if (entry is null)
        {
            ProfileTitle.Text = Texts.Get("ProfilesNoSelection");
            ProfileOrigin.Text = string.Empty;
            RemoveProfileButton.Content = Texts.Get("ProfilesHideButton");
            Fill(ProfileNameBox, string.Empty);
            Fill(ProfileProcessBox, string.Empty);
            Fill(ProfilePriorityBox, string.Empty);
            ProfileGamesBox.IsChecked = false;
            RefreshProfilePreview();

            return;
        }

        var profile = entry.Profile;

        ProfileTitle.Text = profile.Name;
        ProfileOrigin.Text = DescribeOrigin(entry);

        // A shipped profile cannot be deleted - its file is inside the program and would be back
        // at the next start - so it is hidden instead. See ProfileLibrary.Hide.
        RemoveProfileButton.Content = Texts.Get(entry.Source == ProfileSource.User
            ? "ProfilesDeleteButton"
            : entry.Hidden ? "ProfilesShowButton" : "ProfilesHideButton");

        RemoveProfileButton.ToolTip = Texts.Get(entry.Source == ProfileSource.User
            ? "ProfilesDeleteTooltip"
            : entry.Hidden ? "ProfilesShowTooltip" : "ProfilesHideTooltip");

        Fill(ProfileNameBox, profile.Name);
        Fill(ProfileProcessBox, string.Join(", ", profile.Match.Processes));
        Fill(ProfilePriorityBox, profile.Match.Priority.ToString(CultureInfo.InvariantCulture));
        ProfileGamesBox.IsChecked = profile.Match.AppliesToGames;

        RefreshProfilePreview();

        void ShowSection(ProfileSection section, TextBlock state, Button reset)
        {
            var overridden = entry?.Source == ProfileSource.Shipped && entry.IsOverridden(section);

            state.Text = entry is null
                ? string.Empty
                : Texts.Get(entry.Source == ProfileSource.User
                    ? "ProfilesSectionYours"
                    : overridden ? "ProfilesSectionEdited" : "ProfilesSectionShipped");

            reset.IsEnabled = overridden;
        }

        // Only written when it differs, so committing a field does not move the caret in the box
        // the user has just tabbed into.
        static void Fill(TextBox box, string value)
        {
            if (!string.Equals(box.Text, value, StringComparison.Ordinal))
            {
                box.Text = value;
            }
        }
    }

    private void OnAddProfileClicked(object sender, RoutedEventArgs e)
    {
        var name = Texts.Get("ProfilesNewName");

        var profile = new ApplicationProfile(
            _library.NewId(name),
            name,
            ProfileKind.App,
            ProfileSource.User,
            ProfileMatch.None,
            ApplicationProfile.NoHighlights,
            ApplicationProfile.NoShortcuts);

        _library.Add(profile);
        _entry = _library.Find(profile.Id);

        // A filter still in the box would hide the profile that was just created.
        ProfileSearchBox.Text = string.Empty;

        Commit();

        ProfileNameBox.Focus();
        ProfileNameBox.SelectAll();
    }

    private void OnRemoveProfileClicked(object sender, RoutedEventArgs e)
    {
        if (_entry is not { } entry)
        {
            return;
        }

        if (entry.Source == ProfileSource.User)
        {
            _library.Remove(entry.Id);
            _entry = null;
            StatusLine.Text = Texts.Get("StatusProfileDeleted", entry.Profile.Name);
        }
        else if (entry.Hidden)
        {
            _library.Show(entry.Id);
            StatusLine.Text = Texts.Get("StatusProfileShown", entry.Profile.Name);
        }
        else
        {
            _library.Hide(entry.Id);
            StatusLine.Text = Texts.Get("StatusProfileHidden", entry.Profile.Name);
        }

        Commit();
    }

    private void OnResetSectionClicked(object sender, RoutedEventArgs e)
    {
        if (_entry is not { } entry
            || sender is not Button button
            || !Enum.TryParse<ProfileSection>(button.Tag as string, out var section))
        {
            return;
        }

        _library.Reset(entry.Id, section);
        Commit();

        StatusLine.Text = Texts.Get("StatusSectionReset", SectionName(section), entry.Profile.Name);
    }

    private void OnResetProfileClicked(object sender, RoutedEventArgs e)
    {
        if (_entry is not { } entry)
        {
            return;
        }

        _library.ResetAll(entry.Id);
        Commit();

        StatusLine.Text = Texts.Get("StatusProfileReset", entry.Profile.Name);
    }

    /// <summary>The heading a section carries in the editor, so the message names it the same way.</summary>
    private static string SectionName(ProfileSection section) => section switch
    {
        ProfileSection.Match => Texts.Get("ProfilesSectionMatch"),
        ProfileSection.Highlights => Texts.Get("ProfilesSectionHighlights"),
        ProfileSection.Shortcuts => Texts.Get("ProfilesSectionShortcuts"),
        _ => section.ToString()
    };

    private void OnProfileFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_loading || _rebuilding || _entry is not { } entry)
        {
            return;
        }

        var profile = entry.Profile;

        var processes = ProfileProcessBox.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Replace(".exe", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Built from the existing match rather than from scratch, so fields the editor does not
        // show survive. TitleContains is one: it is set only for programs sharing an executable
        // name - LibreOffice's modules, anything running as javaw - and rebuilding without it
        // would drop the one condition keeping Writer and Calc apart, the moment the user
        // touched an unrelated field.
        var match = profile.Match with
        {
            Processes = processes,
            AppliesToGames = ProfileGamesBox.IsChecked == true,
            Priority = int.TryParse(ProfilePriorityBox.Text, out var priority)
                ? priority
                : profile.Match.Priority
        };

        var name = string.IsNullOrWhiteSpace(ProfileNameBox.Text) ? profile.Name : ProfileNameBox.Text.Trim();

        var matchChanged = !SameMatch(match, profile.Match);
        var renamed = !string.Equals(name, profile.Name, StringComparison.Ordinal);

        // Nothing changed, so nothing is written and nothing is frozen. These handlers run on
        // every lost focus: clicking into a box and out again must leave a shipped profile
        // following the shipped file, or browsing the list would quietly freeze all of them.
        if (!matchChanged && !renamed)
        {
            // The fields are still put back, so a name of only spaces or an ".exe" the parser
            // dropped does not stay on screen as if it had been accepted.
            ShowEntry();
            return;
        }

        if (matchChanged)
        {
            _library.Edit(profile.Id, profile with { Match = match }, ProfileSection.Match);
        }

        // After the edit above, because for a profile of the user's own that edit rewrites the
        // whole profile - including the name it had before the rename.
        if (renamed)
        {
            _library.Rename(profile.Id, name);
        }

        Commit();
    }

    private static bool SameMatch(ProfileMatch left, ProfileMatch right)
        => left.AppliesToGames == right.AppliesToGames
           && left.Priority == right.Priority
           && left.Processes.SequenceEqual(right.Processes, StringComparer.Ordinal)
           && (left.TitleContains ?? []).SequenceEqual(right.TitleContains ?? [], StringComparer.Ordinal);

    // ---------------------------------------------------------------- profile editing

    private void OnEditModeChanged(object sender, RoutedEventArgs e)
    {
        _mode = ReferenceEquals(sender, ShortcutModeButton) ? EditMode.Shortcuts : EditMode.Highlights;

        HighlightModeButton.IsChecked = _mode == EditMode.Highlights;
        ShortcutModeButton.IsChecked = _mode == EditMode.Shortcuts;
        HighlightPanel.Visibility = _mode == EditMode.Highlights ? Visibility.Visible : Visibility.Collapsed;
        ShortcutPanel.Visibility = _mode == EditMode.Shortcuts ? Visibility.Visible : Visibility.Collapsed;

        RefreshProfilePreview();
    }

    private void BuildProfileShortcutEditor()
    {
        ProfileLayerList.ItemsSource = ModifierCombination.Known
            .Select(m => new LayerEntry(m, ModifierCombination.Format(m)))
            .ToList();

        ProfileLayerList.SelectedIndex = 3;      // Ctrl: the layer a program profile usually replaces

        BuildGroupPicker(ProfileGroupPicker, _profileGroupButtons, _profileGroup, OnProfileGroupPicked);
    }

    private void OnProfileGroupPicked(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton clicked && clicked.Tag is FunctionGroup group)
        {
            _profileGroup = group;
            Check(_profileGroupButtons, clicked);
        }
    }

    private void OnProfileLayerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileLayerList.SelectedItem is LayerEntry entry)
        {
            _profileLayer = entry.Modifiers;
            RefreshProfilePreview();
        }
    }

    private void OnProfileKeyClicked(KeyDefinition key)
    {
        if (_entry is not { } entry)
        {
            StatusLine.Text = Texts.Get("StatusSelectProfileFirst");
            return;
        }

        if (_mode == EditMode.Highlights)
        {
            var highlights = new Dictionary<string, KeyHighlight>(entry.Profile.Highlights, StringComparer.Ordinal);

            // The label survives a recolouring: it is the only record of what the key does, and
            // losing it would make the profile uncheckable.
            var existing = highlights.GetValueOrDefault(key.Id);
            var highlight = new KeyHighlight(_highlightColour, existing?.Label);

            // Clicking a key that already has this colour changes nothing, so it must not freeze
            // the section either.
            if (highlight == existing)
            {
                return;
            }

            highlights[key.Id] = highlight;

            Apply(entry.Profile with { Highlights = highlights }, ProfileSection.Highlights);

            return;
        }

        var set = ProfileSetFor(entry.Profile, _profileLayer);
        var characters = new Dictionary<string, Shortcut>(
            set?.Characters ?? ShortcutSet.Empty.Characters, StringComparer.OrdinalIgnoreCase);
        var keys = new Dictionary<string, Shortcut>(
            set?.Keys ?? ShortcutSet.Empty.Keys, StringComparer.Ordinal);

        var character = TypedCharacter(key);
        var previous = character is null ? keys.GetValueOrDefault(key.Id) : characters.GetValueOrDefault(character);
        var shortcut = new Shortcut(_profileGroup, previous?.Label);

        // Same group on a layer the profile already defines: nothing to record.
        if (set is not null && shortcut == previous)
        {
            return;
        }

        if (character is null)
        {
            keys[key.Id] = shortcut;
        }
        else
        {
            characters[character] = shortcut;
        }

        Apply(
            entry.Profile with { Shortcuts = WithLayer(entry.Profile, new ShortcutSet(characters, keys)) },
            ProfileSection.Shortcuts);
    }

    private void OnProfileKeyCleared(KeyDefinition key)
    {
        if (_entry is not { } entry)
        {
            return;
        }

        if (_mode == EditMode.Highlights)
        {
            if (!entry.Profile.Highlights.ContainsKey(key.Id))
            {
                return;
            }

            var highlights = new Dictionary<string, KeyHighlight>(entry.Profile.Highlights, StringComparer.Ordinal);
            highlights.Remove(key.Id);

            Apply(entry.Profile with { Highlights = highlights }, ProfileSection.Highlights);

            return;
        }

        if (ProfileSetFor(entry.Profile, _profileLayer) is not { } set)
        {
            return;
        }

        var characters = new Dictionary<string, Shortcut>(set.Characters, StringComparer.OrdinalIgnoreCase);
        var keys = new Dictionary<string, Shortcut>(set.Keys, StringComparer.Ordinal);

        var removed = keys.Remove(key.Id);

        if (TypedCharacter(key) is { } character)
        {
            removed |= characters.Remove(character);
        }

        if (!removed)
        {
            return;
        }

        Apply(
            entry.Profile with { Shortcuts = WithLayer(entry.Profile, new ShortcutSet(characters, keys)) },
            ProfileSection.Shortcuts);
    }

    private void OnClearHighlightsClicked(object sender, RoutedEventArgs e)
    {
        // Clearing what is already empty would freeze the section for nothing.
        if (_entry is { } entry && entry.Profile.Highlights.Count > 0)
        {
            Apply(
                entry.Profile with { Highlights = ApplicationProfile.NoHighlights },
                ProfileSection.Highlights);
        }
    }

    private void OnClearProfileLayerClicked(object sender, RoutedEventArgs e)
    {
        if (_entry is not { } entry || ProfileSetFor(entry.Profile, _profileLayer) is null)
        {
            return;      // the profile does not replace this layer, so there is nothing to clear
        }

        // Removed rather than emptied: a layer the profile does not mention keeps the general
        // one, whereas an empty layer would blank the keyboard while the program is in front.
        var shortcuts = WithoutLayer(entry.Profile);

        Apply(entry.Profile with { Shortcuts = shortcuts }, ProfileSection.Shortcuts);

        StatusLine.Text = Texts.Get(
            "StatusProfileLayerCleared", entry.Profile.Name, ModifierCombination.Format(_profileLayer));
    }

    /// <summary>
    /// The layer a profile defines for the given combination, or <c>null</c> if it defines none.
    /// </summary>
    /// <remarks>
    /// Looked up by the normalised form rather than by the flags: a profile file spells AltGr as
    /// Ctrl plus right Alt, which is a different flag set than the one a lookup asks for.
    /// </remarks>
    private static ShortcutSet? ProfileSetFor(ApplicationProfile profile, Modifiers layer)
    {
        var wanted = Normalise(layer);

        foreach (var (modifiers, set) in profile.Shortcuts)
        {
            if (Normalise(modifiers) == wanted)
            {
                return set;
            }
        }

        return null;
    }

    private Dictionary<Modifiers, ShortcutSet> WithLayer(ApplicationProfile profile, ShortcutSet set)
    {
        var shortcuts = WithoutLayer(profile);
        shortcuts[Normalise(_profileLayer)] = set;

        return shortcuts;
    }

    private Dictionary<Modifiers, ShortcutSet> WithoutLayer(ApplicationProfile profile)
    {
        var wanted = Normalise(_profileLayer);
        var shortcuts = new Dictionary<Modifiers, ShortcutSet>(profile.Shortcuts);

        foreach (var modifiers in shortcuts.Keys.Where(m => Normalise(m) == wanted).ToList())
        {
            shortcuts.Remove(modifiers);
        }

        return shortcuts;
    }

    private void OnHighlightColourClicked(object sender, RoutedEventArgs e)
    {
        if (ColourPicker.Choose(_highlightColour) is { } chosen)
        {
            _highlightColour = chosen;
            UpdateHighlightSwatch();
        }
    }

    private void UpdateHighlightSwatch() =>
        HighlightSwatch.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(_highlightColour.R, _highlightColour.G, _highlightColour.B));

    /// <summary>
    /// Records a change to one section of the selected profile.
    /// </summary>
    /// <remarks>
    /// The section matters: only the one named here stops following the shipped file, so
    /// recolouring a key leaves that profile's shortcuts still picking up improvements from the
    /// next build.
    /// </remarks>
    private void Apply(ApplicationProfile updated, ProfileSection section)
    {
        _library.Edit(updated.Id, updated, section);
        Commit();
    }

    /// <summary>
    /// Puts the library's result back into the engine and saves.
    /// </summary>
    /// <remarks>
    /// The catalogue is rebuilt rather than patched. It is the flattened view the engine selects
    /// from and no longer knows which parts came from where — which is exactly why the library
    /// is held alongside it.
    /// </remarks>
    private void Commit()
    {
        _engine.Settings = _engine.Settings with { Profiles = _library.Catalogue() };

        Save();
        RefreshProfileList();
    }

    /// <summary>
    /// Shows the edited profile as it will actually look, by composing with it directly rather
    /// than drawing the highlights separately — the preview stays honest that way.
    /// </summary>
    private void RefreshProfilePreview()
    {
        if (ProfilePreview.Profile is null)
        {
            return;
        }

        var profile = _entry?.Profile;

        if (_mode == EditMode.Shortcuts)
        {
            var state = new KeyboardState(_profileLayer, new LockStates(true, false, false));

            ProfilePreview.Update(_engine.Preview(state, profile));

            _profileTips = ShortcutTips(_profileLayer, profile);
            _profileSummary = DescribeProfileLayer(profile);
        }
        else
        {
            var state = new KeyboardState(Modifiers.None, new LockStates(true, false, false));

            ProfilePreview.Update(_engine.Preview(state, profile));

            _profileTips = HighlightTips(profile);
            _profileSummary = profile is null
                ? string.Empty
                : profile.Highlights.Count == 1
                    ? Texts.Get("ProfilesHighlightCountOne")
                    : Texts.Get("ProfilesHighlightCount", profile.Highlights.Count);
        }

        ProfilePreview.SetTips(_profileTips);
        ProfileHint.Text = _profileSummary;
    }

    private string DescribeProfileLayer(ApplicationProfile? profile)
    {
        var name = ModifierCombination.Format(_profileLayer);

        if (profile is null)
        {
            return string.Empty;
        }

        if (ProfileSetFor(profile, _profileLayer) is not { } set || set.Count == 0)
        {
            return Texts.Get("ProfilesLayerUndefined", name);
        }

        return set.Count == 1
            ? Texts.Get("ProfilesLayerCountOne", name)
            : Texts.Get("ProfilesLayerCount", name, set.Count);
    }

    /// <summary>Key id → what a highlight says the key is for, for the tooltips.</summary>
    private static Dictionary<string, string> HighlightTips(ApplicationProfile? profile)
    {
        var tips = new Dictionary<string, string>(StringComparer.Ordinal);

        if (profile is null)
        {
            return tips;
        }

        foreach (var (keyId, highlight) in profile.Highlights)
        {
            if (!string.IsNullOrWhiteSpace(highlight.Label))
            {
                tips[keyId] = highlight.Label;
            }
        }

        return tips;
    }

    /// <summary>
    /// Key id → what the command on that key does, for the layer being shown.
    /// </summary>
    /// <remarks>
    /// Looked up exactly as the composer does — position first, then the character the key types
    /// — so the tooltip names the command the key is actually lit for.
    /// </remarks>
    private Dictionary<string, string> ShortcutTips(Modifiers layer, ApplicationProfile? profile)
    {
        var tips = new Dictionary<string, string>(StringComparer.Ordinal);
        var catalogue = profile?.ApplyShortcuts(_engine.Settings.Shortcuts) ?? _engine.Settings.Shortcuts;

        if (!catalogue.Sets.TryGetValue(Normalise(layer), out var set))
        {
            return tips;
        }

        foreach (var key in _engine.Profile.Keys)
        {
            if (set.TryGet(key.Id, out var byPosition))
            {
                tips[key.Id] = byPosition.ToString();
            }
            else if (TypedCharacter(key) is { } character && set.TryGetByCharacter(character, out var byCharacter))
            {
                tips[key.Id] = byCharacter.ToString();
            }
        }

        return tips;
    }

    private void OnProfileKeyHovered(KeyDefinition? key)
        => ProfileHint.Text = key is not null && _profileTips.TryGetValue(key.Id, out var tip)
            ? tip
            : _profileSummary;

    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loading && ProfilePreview.Profile is not null)
        {
            RefreshProfilePreview();
        }
    }

    // ---------------------------------------------------------------- shortcut editor

    private Modifiers _editingLayer = Modifiers.LeftWin;
    private FunctionGroup _editingGroup = FunctionGroup.File;
    private readonly List<ToggleButton> _groupButtons = [];

    private sealed record LayerEntry(Modifiers Modifiers, string Label)
    {
        public override string ToString() => Label;
    }

    private void BuildShortcutEditor()
    {
        var entries = ModifierCombination.Known
            .Select(m => new LayerEntry(m, ModifierCombination.Format(m)))
            .ToList();

        LayerList.ItemsSource = entries;
        LayerList.SelectedIndex = 0;

        BuildGroupPicker(GroupPicker, _groupButtons, _editingGroup, OnGroupPicked);
    }

    /// <summary>
    /// Fills a panel with one button per function group, each in that group's own colour — so
    /// the choice is made in the same terms the keyboard will show it in.
    /// </summary>
    private void BuildGroupPicker(
        WrapPanel host,
        List<ToggleButton> buttons,
        FunctionGroup selected,
        RoutedEventHandler picked)
    {
        buttons.Clear();
        host.Children.Clear();

        foreach (var group in Enum.GetValues<FunctionGroup>())
        {
            var colour = _engine.Settings.Scheme.For(group);

            var button = new ToggleButton
            {
                Content = group.ToString(),
                Tag = group,
                IsChecked = group == selected,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(colour.R, colour.G, colour.B)),
                Foreground = Readable(colour)
            };

            button.Click += picked;
            buttons.Add(button);
            host.Children.Add(button);
        }
    }

    /// <summary>Leaves exactly one of a set of buttons checked.</summary>
    private static void Check(List<ToggleButton> buttons, ToggleButton chosen)
    {
        foreach (var button in buttons)
        {
            button.IsChecked = ReferenceEquals(button, chosen);
        }
    }

    /// <summary>Black or white text, whichever stays legible on the given colour.</summary>
    private static System.Windows.Media.Brush Readable(RgbColor colour)
    {
        var luminance = (0.299 * colour.R + 0.587 * colour.G + 0.114 * colour.B) / 255.0;

        return luminance > 0.55
            ? System.Windows.Media.Brushes.Black
            : System.Windows.Media.Brushes.White;
    }

    private void OnGroupPicked(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton clicked && clicked.Tag is FunctionGroup group)
        {
            _editingGroup = group;
            Check(_groupButtons, clicked);
        }
    }

    private void OnLayerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (LayerList.SelectedItem is LayerEntry entry)
        {
            _editingLayer = entry.Modifiers;
            RefreshShortcutPreview();
        }
    }

    /// <summary>
    /// Shows the layer being edited by composing it as the keyboard would — so an assignment
    /// that does not show up here would not show up on the hardware either.
    /// </summary>
    private void RefreshShortcutPreview()
    {
        if (ShortcutPreview.Profile is null)
        {
            return;
        }

        var state = new KeyboardState(_editingLayer, new LockStates(true, false, false));
        ShortcutPreview.Update(_engine.Preview(state, profile: null));

        var count = _engine.Settings.Shortcuts.Sets.TryGetValue(Normalise(_editingLayer), out var set)
            ? set.Count
            : 0;

        _shortcutTips = ShortcutTips(_editingLayer, profile: null);
        _shortcutSummary = count == 1
            ? Texts.Get("ShortcutsCountOne")
            : Texts.Get("ShortcutsCount", count);

        ShortcutPreview.SetTips(_shortcutTips);
        ShortcutCountLabel.Text = _shortcutSummary;
    }

    private Dictionary<string, string> _shortcutTips = [];
    private string _shortcutSummary = string.Empty;

    /// <summary>Names the command under the pointer — the labels exist to be read somewhere.</summary>
    private void OnShortcutKeyHovered(KeyDefinition? key)
        => ShortcutCountLabel.Text = key is not null && _shortcutTips.TryGetValue(key.Id, out var tip)
            ? tip
            : _shortcutSummary;

    /// <summary>
    /// The key under which a layer is stored. Sides are collapsed, matching how the catalogue
    /// looks sets up at runtime — storing the raw flags would create entries nothing can find.
    /// </summary>
    private static Modifiers Normalise(Modifiers modifiers)
        => ShortcutCatalogue.Normalise(new KeyboardState(modifiers, new LockStates(false, false, false)));

    /// <summary>
    /// The character a key types unmodified, or <c>null</c> if it types nothing.
    /// </summary>
    /// <remarks>
    /// Decides how a clicked key is recorded. A key that types something is stored by that
    /// character, because Ctrl+Z means "the key that types z" — on a German keyboard that is a
    /// different position than on a US one. Keys that type nothing are stored by position.
    /// </remarks>
    private string? TypedCharacter(KeyDefinition key)
    {
        var plain = new KeyboardState(Modifiers.None, new LockStates(true, false, false));
        var meaning = _resolver.Resolve(key.Id, key.ScanCode, plain);

        return meaning.ProducesCharacter ? meaning.Character!.ToLowerInvariant() : null;
    }

    private void OnShortcutKeyClicked(KeyDefinition key)
    {
        var layer = Normalise(_editingLayer);
        var sets = new Dictionary<Modifiers, ShortcutSet>(_engine.Settings.Shortcuts.Sets);
        var existing = sets.TryGetValue(layer, out var found) ? found : ShortcutSet.Empty;

        var characters = new Dictionary<string, Shortcut>(existing.Characters, StringComparer.OrdinalIgnoreCase);
        var keys = new Dictionary<string, Shortcut>(existing.Keys, StringComparer.Ordinal);

        // Changing the group keeps whatever the command was called: the label says what the key
        // does, which no colour can.
        if (TypedCharacter(key) is { } character)
        {
            characters[character] = new Shortcut(_editingGroup, characters.GetValueOrDefault(character)?.Label);
        }
        else
        {
            keys[key.Id] = new Shortcut(_editingGroup, keys.GetValueOrDefault(key.Id)?.Label);
        }

        sets[layer] = new ShortcutSet(characters, keys);
        _engine.Settings = _engine.Settings with { Shortcuts = new ShortcutCatalogue(sets) };

        RefreshShortcutPreview();
        ShowIdlePreview();
        Save();
    }

    private void OnShortcutKeyCleared(KeyDefinition key)
    {
        var layer = Normalise(_editingLayer);

        if (!_engine.Settings.Shortcuts.Sets.TryGetValue(layer, out var existing))
        {
            return;
        }

        var characters = new Dictionary<string, Shortcut>(existing.Characters, StringComparer.OrdinalIgnoreCase);
        var keys = new Dictionary<string, Shortcut>(existing.Keys, StringComparer.Ordinal);

        // Remove whichever way it was recorded - the user should not have to know.
        var removed = keys.Remove(key.Id);

        if (TypedCharacter(key) is { } character)
        {
            removed |= characters.Remove(character);
        }

        if (!removed)
        {
            return;
        }

        var sets = new Dictionary<Modifiers, ShortcutSet>(_engine.Settings.Shortcuts.Sets)
        {
            [layer] = new ShortcutSet(characters, keys)
        };

        _engine.Settings = _engine.Settings with { Shortcuts = new ShortcutCatalogue(sets) };

        RefreshShortcutPreview();
        Save();
    }

    private void OnClearLayerClicked(object sender, RoutedEventArgs e)
    {
        var sets = new Dictionary<Modifiers, ShortcutSet>(_engine.Settings.Shortcuts.Sets)
        {
            [Normalise(_editingLayer)] = ShortcutSet.Empty
        };

        _engine.Settings = _engine.Settings with { Shortcuts = new ShortcutCatalogue(sets) };

        RefreshShortcutPreview();
        Save();
        StatusLine.Text = Texts.Get("StatusLayerCleared", ModifierCombination.Format(_editingLayer));
    }

    private void OnResetShortcutsClicked(object sender, RoutedEventArgs e)
    {
        _engine.Settings = _engine.Settings with { Shortcuts = DefaultShortcuts.Create() };

        RefreshShortcutPreview();
        Save();
        StatusLine.Text = Texts.Get("StatusShortcutsRestored");
    }

    // ---------------------------------------------------------------- settings

    private void OnIdleKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitIdleTimeout();
            Keyboard.ClearFocus();
        }
    }

    private void OnIdleCommitted(object sender, RoutedEventArgs e) => CommitIdleTimeout();

    private void CommitIdleTimeout()
    {
        if (_loading)
        {
            return;
        }

        if (int.TryParse(IdleBox.Text, out var seconds) && seconds > 0)
        {
            _idlePeriod = TimeSpan.FromSeconds(seconds);
            ApplyIdleTimeout();
            StatusLine.Text = Texts.Get("StatusIdleTimeoutSet", seconds);
            Save();
        }
        else
        {
            // Read back from the period rather than from the engine: with the hand-back switched
            // off the engine holds SessionManager.Never, which is not a number of seconds.
            IdleBox.Text = ((int)_idlePeriod.TotalSeconds).ToString(CultureInfo.CurrentCulture);
            StatusLine.Text = Texts.Get("StatusIdleTimeoutInvalid");
        }
    }

    /// <summary>Switches the hand-back off entirely, or back on with the period as set.</summary>
    private void OnHandBackChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        ApplyIdleTimeout();

        StatusLine.Text = HandsBack
            ? Texts.Get("StatusHandBackOn", (int)_idlePeriod.TotalSeconds)
            : Texts.Get("StatusHandBackOff");

        Save();
    }

    private bool HandsBack => HandBackBox.IsChecked == true;

    /// <summary>
    /// Pushes the period and the hand-back switch into the engine as one timeout, and greys out
    /// the period when it has no effect.
    /// </summary>
    private void ApplyIdleTimeout()
    {
        _engine.Settings = _engine.Settings with
        {
            IdleTimeout = HandsBack ? _idlePeriod : SessionManager.Never
        };

        IdleBox.IsEnabled = HandsBack;
        IdleSecondsLabel.Opacity = HandsBack ? 1.0 : 0.5;
    }

    private void OnBrightnessChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading || !IsLoaded)
        {
            return;
        }

        BrightnessLabel.Text = $"{e.NewValue:P0}";

        _engine.Settings = _engine.Settings with
        {
            Scheme = _engine.Settings.Scheme with { Brightness = e.NewValue }
        };

        ShowIdlePreview();
        RefreshProfilePreview();
        Save();
    }

    private void OnProfilesEnabledChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _engine.Settings = _engine.Settings with
        {
            UseApplicationProfiles = ProfilesEnabledBox.IsChecked == true
        };

        Save();
    }

    private void OnAutostartChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        var executable = Environment.ProcessPath;

        if (string.IsNullOrEmpty(executable))
        {
            StatusLine.Text = Texts.Get("StatusAutostartUnknownPath");
            AutostartBox.IsChecked = Autostart.IsEnabled();
            return;
        }

        var problem = Autostart.Set(AutostartBox.IsChecked == true, executable);

        if (problem is not null)
        {
            StatusLine.Text = problem;
            AutostartBox.IsChecked = Autostart.IsEnabled();
            return;
        }

        StatusLine.Text = Texts.Get(AutostartBox.IsChecked == true
            ? "StatusAutostartOn"
            : "StatusAutostartOff");

        Save();
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        var problem = Save();
        StatusLine.Text = problem ?? Texts.Get("StatusSaved", _store.Path);
    }

    private void OnOpenConfigFolderClicked(object sender, RoutedEventArgs e)
    {
        var folder = System.IO.Path.GetDirectoryName(_store.Path);

        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        System.IO.Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
    }

    /// <summary>Selects the language shown in the interface and saves the choice.</summary>
    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || LanguageBox.SelectedItem is not ComboBoxItem { Tag: string tag })
        {
            return;
        }

        if (!Enum.TryParse<LanguageChoice>(tag, out var choice) || choice == _language)
        {
            return;
        }

        _language = choice;

        // Repaints the window rather than asking for a restart: every localised element binds
        // to the text store, so switching the culture is enough for the markup. The labels put
        // together in code are rebuilt afterwards, since a plain string cannot re-read itself.
        Texts.Instance.Use(choice);
        ShowTexts();
        Save();
    }

    private void ShowLanguage(LanguageChoice choice)
    {
        _language = choice;
        Texts.Instance.Use(choice);

        foreach (var item in LanguageBox.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string tag && string.Equals(tag, choice.ToString(), StringComparison.Ordinal))
            {
                LanguageBox.SelectedItem = item;
                return;
            }
        }
    }

    /// <summary>Where the donation buttons in the footer lead.</summary>
    private const string PayPalUrl = "https://paypal.me/eistee";

    private const string KofiUrl = "https://ko-fi.com/eistee";

    private void OnDonateClicked(object sender, RoutedEventArgs e) => Open(PayPalUrl);

    private void OnKofiClicked(object sender, RoutedEventArgs e) => Open(KofiUrl);

    private void Open(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception
                                   or InvalidOperationException
                                   or System.IO.FileNotFoundException)
        {
            // No default browser, or the shell refused. Showing the address beats a dialog the
            // user cannot act on, and beats bringing the program down over a donation link.
            StatusLine.Text = Texts.Get("DonateBrowserFailed", url);
        }
    }

    private string? Save()
    {
        if (_loading)
        {
            return null;
        }

        var stored = StoredSettings.From(
            // The period, not the engine's timeout: with the hand-back off the engine holds
            // Never, and the file should keep the period so switching it back on restores it.
            _idlePeriod,
            _engine.Settings.Scheme,
            _library,
            AutostartBox.IsChecked == true,
            _engine.Settings.UseApplicationProfiles,
            _engine.Settings.Shortcuts,
            _language.ToString(),
            HandsBack);

        return _store.Save(stored);
    }

    protected override void OnClosed(EventArgs e)
    {
        _engine.FrameComposed -= OnFrameComposed;
        _engine.StateChanged -= OnStateChanged;
        _engine.Fault -= OnFault;
        _engine.ProfileChanged -= OnProfileChanged;

        base.OnClosed(e);
    }
}
