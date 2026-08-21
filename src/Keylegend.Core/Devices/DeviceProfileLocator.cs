namespace Keylegend.Core.Devices;

/// <summary>
/// Finds device profiles on disk, so that running straight from a build works without
/// arguments and a published copy works with its profiles alongside.
/// </summary>
public static class DeviceProfileLocator
{
    /// <summary>
    /// Walks up from the application directory looking for a <c>devices</c> folder and returns
    /// the profile most likely to be the right one, or <c>null</c> if there is none.
    /// </summary>
    /// <param name="attached">
    /// The USB ids of the keyboards currently plugged in, if they could be established.
    /// <c>Keylegend.Windows.ConnectedKeyboards.Detect()</c> supplies them; they arrive as plain
    /// data so that this assembly stays free of platform APIs.
    /// </param>
    /// <param name="preferredLayout">
    /// The physical layout to prefer when several profiles describe the same recognised model,
    /// e.g. <c>ISO-DE</c>. Only ever used to break that tie.
    /// </param>
    /// <remarks>
    /// <para>
    /// While only one profile shipped, "the first file found" was the same thing as "the right
    /// one". It stopped being that the moment a second profile arrived, and picked whichever
    /// name sorted first — a 60 % layout, which left two thirds of a full-size keyboard dark
    /// because a profile that does not mention a key cannot light it.
    /// </para>
    /// <para>
    /// So the choice is ranked instead:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// A profile whose <c>usb</c> ids match a keyboard Windows reports as attached. That is
    /// recognition rather than inference, and it wins outright.
    /// </description></item>
    /// <item><description>
    /// Then the physical layout Windows suggests, which separates the ISO and ANSI variants of
    /// a model that was recognised — a vendor uses one product id for both.
    /// </description></item>
    /// <item><description>
    /// Then a generic profile over a named model. If the hardware was not recognised, calling it
    /// a "Razer BlackWidow V4" in the window is a guess presented as a fact — and wrong for
    /// everyone who does not own one. "Full-size keyboard (US)" claims only what is known.
    /// </description></item>
    /// <item><description>
    /// Then <c>verified</c>. That flag means somebody stepped through the mapping on the real
    /// hardware — of <em>that</em> model, which says nothing about an unrecognised one, so it
    /// ranks below both of the above.
    /// </description></item>
    /// <item><description>
    /// Then the most keys. Guessing too large is the cheaper mistake: a full-size profile on a
    /// tenkeyless board draws keys that are not there, while a compact profile on a full-size
    /// board leaves real keys dark and looks like a bug in the lighting.
    /// </description></item>
    /// <item><description>
    /// Then by path, so that an otherwise tied choice is at least the same one every start.
    /// </description></item>
    /// </list>
    /// <para>
    /// None of this beats asking the user, which is what <see cref="FindAll"/> is there for.
    /// It only has to be a defensible default until they have chosen.
    /// </para>
    /// </remarks>
    public static string? FindDefault(
        IReadOnlyList<UsbId>? attached = null,
        string? preferredLayout = null)
        => ChooseFrom(FindAll(), attached, preferredLayout);

    /// <summary>
    /// Ranks an explicit set of profile paths. Separate from <see cref="FindDefault"/> so that
    /// the ranking can be exercised without a devices folder next to the test binaries.
    /// </summary>
    /// <param name="paths">Candidate profiles, in the order ties should be resolved.</param>
    /// <inheritdoc cref="FindDefault" path="/param"/>
    public static string? ChooseFrom(
        IReadOnlyList<string> paths,
        IReadOnlyList<UsbId>? attached = null,
        string? preferredLayout = null)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var best = default(string);
        var bestScore = (Recognised: false, Layout: false, Generic: false, Verified: false, Keys: -1);

        foreach (var path in paths)
        {
            // A profile that does not parse must not stop the application from starting: the
            // next one may be perfectly good, and the validator reports the broken one anyway.
            DeviceProfile profile;
            try
            {
                profile = DeviceProfileLoader.Load(path);
            }
            catch (DeviceProfileException)
            {
                continue;
            }

            var recognised = profile.Usb is not null
                && attached is not null
                && attached.Any(id => profile.Usb.Matches(id));

            var layoutMatches = preferredLayout is not null
                && string.Equals(profile.PhysicalLayout, preferredLayout, StringComparison.OrdinalIgnoreCase);

            // Only ever decides between profiles that were not recognised: a generic profile
            // carries no usb ids, so it can never win the first comparison anyway.
            var generic = string.Equals(profile.Vendor, "Generic", StringComparison.OrdinalIgnoreCase);

            var score = (recognised, layoutMatches, generic, profile.Verified, profile.Keys.Count);

            // recognised beats everything, then the layout, then verified, then size. The
            // candidates arrive ordered by path and a strict comparison keeps the first of any
            // tie, so the choice is the same on every start.
            if (Compare(score, bestScore) > 0)
            {
                best = path;
                bestScore = score;
            }
        }

        return best;
    }

    private static int Compare(
        (bool Recognised, bool Layout, bool Generic, bool Verified, int Keys) first,
        (bool Recognised, bool Layout, bool Generic, bool Verified, int Keys) second)
    {
        if (first.Recognised != second.Recognised) return first.Recognised ? 1 : -1;
        if (first.Layout != second.Layout) return first.Layout ? 1 : -1;
        if (first.Generic != second.Generic) return first.Generic ? 1 : -1;
        if (first.Verified != second.Verified) return first.Verified ? 1 : -1;

        return first.Keys.CompareTo(second.Keys);
    }

    /// <summary>Every profile found, ordered by path, for a device picker.</summary>
    public static IReadOnlyList<string> FindAll()
    {
        foreach (var directory in DevicesDirectories())
        {
            var profiles = Directory
                .EnumerateFiles(directory, "device.json", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (profiles.Length > 0)
            {
                return profiles;
            }
        }

        return [];
    }

    private static IEnumerable<string> DevicesDirectories()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "devices");

            if (Directory.Exists(candidate))
            {
                yield return candidate;
            }

            directory = directory.Parent;
        }
    }
}
