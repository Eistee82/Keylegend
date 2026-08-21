using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows.Data;
using System.Windows.Markup;

namespace Keylegend.App.Localisation;

/// <summary>Which language the interface is shown in.</summary>
/// <remarks>
/// The names are what lands in the settings file, so they are spelled out rather than
/// abbreviated to culture codes: a hand-edited <c>"Language": "Ukrainian"</c> is readable in a
/// way <c>"uk"</c> is not, and it cannot be confused with the United Kingdom.
/// </remarks>
public enum LanguageChoice
{
    /// <summary>Follow Windows, falling back to English where there is no translation.</summary>
    Automatic,

    English,

    German,

    Spanish,

    French,

    Italian,

    Dutch,

    Polish,

    Portuguese,

    Russian,

    Ukrainian,

    ChineseSimplified
}

/// <summary>
/// The interface texts, looked up at runtime.
/// </summary>
/// <remarks>
/// <para>
/// An indexer on a single observable instance rather than the generated static class, so that
/// switching language repaints the window instead of requiring a restart. Every localised
/// element binds to <c>[Key]</c> on this object; raising <see cref="PropertyChanged"/> for the
/// indexer re-reads all of them at once.
/// </para>
/// <para>
/// A missing key returns the key itself rather than blank or an exception. A window with
/// <c>SettingsIdleHeading</c> written across it is obviously broken and easy to find; an empty
/// label looks like a layout problem and gets hunted in the wrong place.
/// </para>
/// </remarks>
public sealed class Texts : INotifyPropertyChanged
{
    private static readonly ResourceManager Resources =
        new("Keylegend.App.Localisation.Strings", typeof(Texts).Assembly);

    /// <summary>
    /// Which culture each choice selects. <see cref="LanguageChoice.Automatic"/> is absent on
    /// purpose: it means "whatever Windows says", which is not a fixed culture.
    /// </summary>
    /// <remarks>
    /// Chinese is requested as <c>zh-Hans</c> rather than <c>zh-CN</c> so that the resources
    /// also serve a system set to Singapore or any other simplified-Chinese region — .NET walks
    /// zh-CN to zh-Hans to zh on its own, but not sideways from one region to another.
    /// </remarks>
    private static readonly Dictionary<LanguageChoice, string> Cultures = new()
    {
        [LanguageChoice.English] = "en",
        [LanguageChoice.German] = "de",
        [LanguageChoice.Spanish] = "es",
        [LanguageChoice.French] = "fr",
        [LanguageChoice.Italian] = "it",
        [LanguageChoice.Dutch] = "nl",
        [LanguageChoice.Polish] = "pl",
        [LanguageChoice.Portuguese] = "pt",
        [LanguageChoice.Russian] = "ru",
        [LanguageChoice.Ukrainian] = "uk",
        [LanguageChoice.ChineseSimplified] = "zh-Hans"
    };

    private CultureInfo _culture = CultureInfo.CurrentUICulture;

    public static Texts Instance { get; } = new();

    private Texts()
    {
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => Resources.GetString(key, _culture) ?? key;

    /// <summary>The language in use, as a culture.</summary>
    public CultureInfo Culture => _culture;

    /// <summary>
    /// Switches language and repaints everything bound to these texts.
    /// </summary>
    public void Use(LanguageChoice choice)
    {
        var culture = Cultures.TryGetValue(choice, out var name)
            ? CultureInfo.GetCultureInfo(name)
            : CultureInfo.CurrentUICulture;

        if (Equals(culture, _culture))
        {
            return;
        }

        _culture = culture;

        // "Item[]" is the name WPF listens for to re-evaluate every indexer binding.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    /// <summary>Looks up a text without binding to it, for messages built in code.</summary>
    public static string Get(string key) => Instance[key];

    /// <summary>Looks up a text and fills in its placeholders.</summary>
    public static string Get(string key, params object?[] arguments)
        => string.Format(Instance.Culture, Instance[key], arguments);
}

/// <summary>
/// Puts a localised text into XAML: <c>Text="{loc:Text SettingsBrightnessHeading}"</c>.
/// </summary>
/// <remarks>
/// Returns a binding rather than the string itself, which is what makes switching language take
/// effect immediately rather than at the next start.
/// </remarks>
[MarkupExtensionReturnType(typeof(string))]
public sealed class TextExtension : MarkupExtension
{
    public TextExtension()
    {
    }

    public TextExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = Texts.Instance,
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}
