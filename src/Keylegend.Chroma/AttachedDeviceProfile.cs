using Keylegend.Core.Devices;
using Keylegend.Core.Input;

namespace Keylegend.Chroma;

/// <summary>
/// Builds the profile for the keyboard that is actually plugged in, instead of choosing a
/// shipped one for a model somebody guessed at.
/// </summary>
/// <remarks>
/// <para>
/// Three things make up a profile, and each now comes from where it is actually known:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <b>Which keys exist</b> — from the lighting service, which reports the scan codes of the
/// attached hardware. A German board reports a key a US one does not, and no guess is involved.
/// </description></item>
/// <item><description>
/// <b>Where each key sits in the matrix</b> — from <see cref="StandardKeyMatrix"/>. That is a
/// property of the lighting protocol and identical on every model, which is why the vendor's own
/// software needs no per-model table either.
/// </description></item>
/// <item><description>
/// <b>What the keyboard looks like</b> — from the vendor's own drawing of that model in that
/// physical layout: the keys and their names, their real sizes, the casing with its dial and
/// media strip, and the outlines of the characters printed on the caps.
/// </description></item>
/// </list>
/// <para>
/// So no profile is shipped at all any more. The per-model files that used to carry
/// <c>row</c>/<c>column</c> values were made redundant by the second point — those values had been
/// derived from device firmware, which describes how a board is <em>wired</em> rather than how a
/// custom frame is addressed, and on 202 of 458 of them the two disagreed. The generic layouts
/// that replaced them were made redundant by the third: they supplied geometry and legends, and
/// the drawing supplies both, for the right model and in the right language.
/// </para>
/// <para>
/// The one profile ever calibrated against hardware is kept as test data, and it is what all of
/// this is checked against: <c>FromDrawingTests</c> builds a profile from the drawing and compares
/// it against that measurement, key for key and cell for cell.
/// </para>
/// </remarks>
public static class AttachedDeviceProfile
{
    /// <summary>
    /// Builds the attached keyboard's profile from the vendor's drawing.
    /// </summary>
    /// <param name="device">What the lighting service says about the attached keyboard.</param>
    /// <param name="drawing">The vendor's drawing of that model, in that physical layout.</param>
    /// <remarks>
    /// <para>
    /// Everything a profile used to carry is in the drawing: the keys and their names, their
    /// geometry, the casing, the printed legends, and — just past the closing tag — which physical
    /// layout it is for. The matrix cell of each key comes from <c>RZKEY</c>, which is a constant
    /// of the SDK, and its scan code from its id. So a file per model, or per layout, restates what
    /// is already there.
    /// </para>
    /// <para>
    /// Returns <c>null</c> if the drawing cannot be understood — no key of it resolving to a known
    /// id, most likely because the vendor renamed something. The caller then has the choice of
    /// falling back, which is why this does not throw.
    /// </para>
    /// </remarks>
    public static DeviceProfile? FromDrawing(SdkDeviceDescription device, SvgKeyboardLayout drawing)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(drawing);

        var iso = drawing.Keys.Any(k => k.Name == "Extra1");
        var japanese = drawing.Keys.Any(k => k.Name == "Extra3");
        var available = KnownIds(iso, japanese);

        var keys = new List<KeyDefinition>(drawing.Keys.Count);
        var taken = new HashSet<string>(StringComparer.Ordinal);

        foreach (var drawn in drawing.Keys)
        {
            if (DrawnKeyNames.Resolve(drawn.Name, available) is not { } id || !taken.Add(id))
            {
                continue;
            }

            var rectangles = drawn.Rectangles().ToArray();
            var cell = StandardKeyMatrix.Cell(id);

            keys.Add(new KeyDefinition(
                id,
                rectangles[0].X,
                rectangles[0].Y,
                rectangles[0].Width,
                rectangles[0].Height,
                cell?.Row,
                cell?.Column,
                ScanCode: null,
                Parts: rectangles.Length > 1
                    ? [.. rectangles.Skip(1).Select(r => new KeyArea(r.X, r.Y, r.Width, r.Height))]
                    : null,
                // The drawing's own name, as a last resort for a key that types nothing. The
                // printed legends are what is normally shown; this is only read when the outline
                // is missing, and an English word beats a blank cap.
                Label: drawn.Name.Length > 0 ? drawn.Name.ToLowerInvariant() : null));
        }

        if (keys.Count == 0)
        {
            return null;
        }

        var name = string.IsNullOrWhiteSpace(device.ProductName) ? "Keyboard" : device.ProductName;

        var bare = new DeviceProfile(
            Name: name,
            PhysicalLayout: LayoutTemplates.NameOf(device.LayoutId, iso, japanese),
            Canvas: new Canvas(drawing.Width, drawing.Height),
            Matrix: new MatrixSize(
                device.MatrixRows > 0 ? device.MatrixRows : 6,
                device.MatrixColumns > 0 ? device.MatrixColumns : 22),
            Keys: keys);

        // Geometry, casing and legends all arrive through the same hand-off as before, which is
        // also where the canvas is sized to the case and every key moved onto its origin. The
        // keys have to be taken from its result, not from what went in: dropping the unreported
        // ones from `bare` instead left every key at the drawing's own coordinates while the
        // canvas had already moved, and the right-hand column then sat outside it.
        var measured = WithGeometryOf(bare, drawing);

        return measured with { Keys = KeysTheDeviceHas(measured, device) };
    }

    /// <summary>
    /// Every key id that could appear on a keyboard of this shape.
    /// </summary>
    /// <remarks>
    /// <see cref="DrawnKeyNames"/> needs this to settle the three names that mean different keys
    /// on different layouts — <c>Backslash</c> is the key after the left Shift on ISO and the one
    /// above Enter on ANSI. Which of those a drawing means is decided by the drawing itself: ISO
    /// boards carry the extra key beside Enter, and that is <c>Extra1</c>.
    /// </remarks>
    /// <summary>
    /// Drops the keys the hardware does not report, and keeps the silent ones.
    /// </summary>
    /// <remarks>
    /// A drawing describes a model, and the board in front of you may be a variant of it. Two
    /// cases look alike here and must not be treated alike: a tenkeyless board does not report a
    /// number pad because it has none, while fn is not reported because it sends nothing — it is
    /// right there under your finger. The device says how many such silent keys it carries, and
    /// that many unreported keys are kept.
    /// </remarks>
    private static List<KeyDefinition> KeysTheDeviceHas(
        DeviceProfile profile, SdkDeviceDescription device)
    {
        var reported = Reported(device);
        var kept = new List<KeyDefinition>(profile.Keys.Count);

        var unreported = profile.Keys.Count(k => ScanCodeOf(k) is { } c && !reported.Contains(c));
        var keepSilent = unreported <= device.SilentKeys;

        foreach (var key in profile.Keys)
        {
            var code = ScanCodeOf(key);

            if (code is not null && !reported.Contains(code.Value) && !keepSilent)
            {
                continue;
            }

            kept.Add(key with { Label = LabelFor(key) });
        }

        return kept;
    }

    private static IReadOnlySet<string> KnownIds(bool iso, bool japanese)
    {
        var ids = new HashSet<string>(StandardKeyMatrix.Ids, StringComparer.Ordinal);

        if (!iso && !japanese)
        {
            ids.Remove("Keyboard_NonUsBackslash");
            ids.Remove("Keyboard_NonUsTilde");
        }
        else
        {
            // On ISO the key above Enter does not exist; leaving it in would let Backslash resolve
            // to it and put the key next to the left Shift somewhere else entirely.
            ids.Remove("Keyboard_Backslash");
        }

        if (!japanese)
        {
            foreach (var id in ids.Where(i => i.StartsWith("Keyboard_Jp", StringComparison.Ordinal)).ToArray())
            {
                ids.Remove(id);
            }
        }

        return ids;
    }

    /// <summary>
    /// Replaces the drawn geometry with the vendor's own measurements, matching keys by where
    /// they sit rather than by name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The shipped layouts describe a shape of keyboard — full-size, tenkeyless — and a model
    /// that departs from it is drawn wrong: a macro column is missing, a dial is not there, a
    /// key is the wrong width. The vendor's drawing has the real measurements for the attached
    /// model, and taking them is what makes an undrawn keyboard appear as itself.
    /// </para>
    /// <para>
    /// Matching is positional because the two sides name keys differently — the drawing says
    /// <c>Caps</c> and <c>NumPad0</c>, the profile says <c>Keyboard_CapsLock</c> and
    /// <c>Keyboard_Num0</c>. Reading positions off a picture is reliable in a way that
    /// reconciling two naming schemes is not. A key whose place has no counterpart keeps what it
    /// had, so a drawing that turns out to describe something else leaves the layout intact
    /// rather than scrambling it.
    /// </para>
    /// </remarks>
    private static DeviceProfile WithGeometryOf(DeviceProfile layout, SvgKeyboardLayout drawing)
    {
        // Both sides describe the same keyboard, but not on the same canvas: the vendor's drawing
        // includes the casing around the keys, the profile does not, and the two are not even the
        // same shape. Lining up the canvases therefore squashes the drawing — the space bar came
        // out a sixth of its width. What does correspond is the block of keys itself, so the fit
        // is taken from that.
        var from = Bounds(drawing.Keys.Select(k => (k.X, k.Y, k.Width, k.Height)));
        var onto = Bounds(layout.Keys.Select(k => (k.X, k.Y, k.Width, k.Height)));

        if (from.Width <= 0 || from.Height <= 0 || onto.Width <= 0 || onto.Height <= 0)
        {
            return layout;
        }

        var scaleX = onto.Width / from.Width;
        var scaleY = onto.Height / from.Height;

        double MapX(double x) => onto.Left + ((x - from.Left) * scaleX);
        double MapY(double y) => onto.Top + ((y - from.Top) * scaleY);

        var byPlace = new List<(double X, double Y, SvgKey Key)>();

        foreach (var key in drawing.Keys)
        {
            byPlace.Add((MapX(key.X), MapY(key.Y), key));
        }

        var taken = new HashSet<SvgKey>();
        var keys = new List<KeyDefinition>(layout.Keys.Count);
        var drawnKeys = new Dictionary<string, KeyArea>(StringComparer.Ordinal);

        // By name first. Both schemes name keys after the US layout, so they line up one to one
        // once the spellings are reconciled, and a name is exact where a position is a guess —
        // the ISO Enter had no positional counterpart at all, because its main rectangle is the
        // narrow upper half while the drawing's outline starts at the wider lower one.
        var available = layout.Keys.Select(k => k.Id).ToHashSet(StringComparer.Ordinal);
        var byName = new Dictionary<string, SvgKey>(StringComparer.Ordinal);

        foreach (var drawnKey in drawing.Keys)
        {
            if (DrawnKeyNames.Resolve(drawnKey.Name, available) is { } id)
            {
                byName.TryAdd(id, drawnKey);
            }
        }

        foreach (var key in layout.Keys)
        {
            var match = byName.GetValueOrDefault(key.Id) ?? Nearest(byPlace, key, taken);

            if (match is not null)
            {
                taken.Add(match);

                // Recorded for every key that has a counterpart, including the ones whose
                // geometry is left alone below: this is what the printed legends are placed by,
                // and a key with no legend of its own would otherwise show its neighbour's.
                //
                // Everything the drawn key covers, not just its main rectangle. The preview
                // aligns a legend on the centre of everything our key covers, so giving it only
                // the Enter's upper half to compare against moved the word "enter" a third of the
                // key downwards — measured against the same key, or the two do not agree.
                var span = Bounds(match.Rectangles().Select(r => (r.X, r.Y, r.Width, r.Height)));

                drawnKeys[key.Id] = new KeyArea(span.Left, span.Top, span.Width, span.Height);
            }

            if (match is null)
            {
                keys.Add(key);
                continue;
            }

            // A key drawn as several rectangles takes them all. Fitting our own two into the
            // outline's bounding box — which this did first — put the step in the wrong place:
            // the drawing's lower half of the Enter is the taller one, so its step sits at 46.7 %
            // of the height, while a layout on the standard grid has two equal halves and puts it
            // at 50 %. The step was visibly off and the halves no longer met, which showed up as
            // a seam in the glow beside the Enter.
            if (match.Parts is { Count: > 0 })
            {
                var rectangles = match.Rectangles()
                    .Select(r => new KeyArea(
                        MapX(r.X), MapY(r.Y), r.Width * scaleX, r.Height * scaleY))
                    .ToList();

                keys.Add(key with
                {
                    X = rectangles[0].X,
                    Y = rectangles[0].Y,
                    Width = rectangles[0].Width,
                    Height = rectangles[0].Height,
                    Parts = [.. rectangles.Skip(1)],
                });

                continue;
            }

            // Drawn as one rectangle, but our layout has it as several — nothing to take the
            // extra ones from, so the whole key keeps what it was drawn with rather than being
            // flattened into the single rectangle the drawing offers.
            if (key.Parts is { Count: > 0 })
            {
                keys.Add(key);
                continue;
            }

            keys.Add(key with
            {
                X = MapX(match.X),
                Y = MapY(match.Y),
                Width = match.Width * scaleX,
                Height = match.Height * scaleY,
            });
        }

        // The legends travel with the geometry, under the same mapping, expressed so that a
        // drawing coordinate becomes a profile coordinate on its own. Kept whether or not the
        // measured geometry survives the check below: the mapping is taken from the two blocks of
        // keys as a whole, so it lines the legends up either way, and a slightly imperfect fit is
        // worth far more than no printed legend at all.
        var chassis = drawing.Chassis?
            .Select(s => new ChassisShape(s.Path, (ChassisLayer)(int)s.Layer))
            .ToList();

        var outline = drawing.Legends is { Length: > 0 } path ? path : string.Empty;

        var offsetX = onto.Left - (from.Left * scaleX);
        var offsetY = onto.Top - (from.Top * scaleY);

        var canvas = layout.Canvas;

        // With a casing to draw, the canvas has to be the whole drawing rather than the block of
        // keys: the case reaches past the keys on every side, and the media strip along the top
        // right sits some twenty units above the first row. Putting the drawing's own origin at
        // 0,0 makes the profile describe exactly what the drawing describes — everything shifts
        // by the same amount, so nothing moves relative to anything else.
        if (chassis is { Count: > 0 })
        {
            // Sized to the case, not to the drawing: the drawing leaves a margin the case does
            // not use, and carrying that through would shrink the keyboard on screen for nothing.
            var body = Bounds(drawing.Chassis!
                .Where(s => s.Bounds.Width > 0 && s.Bounds.Height > 0)
                .Select(s => (s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height)));

            var caseLeft = body.Width > 0 ? MapX(body.Left) : 0;
            var caseTop = body.Height > 0 ? MapY(body.Top) : 0;

            for (var i = 0; i < keys.Count; i++)
            {
                keys[i] = Shifted(keys[i], -caseLeft, -caseTop);
            }

            canvas = body.Width > 0 && body.Height > 0
                ? new Canvas(body.Width * scaleX, body.Height * scaleY)
                : new Canvas(drawing.Width * scaleX, drawing.Height * scaleY);

            offsetX -= caseLeft;
            offsetY -= caseTop;
        }

        // Kept if there is anything at all to draw: the printed legends, the casing, or both.
        var legend = outline.Length > 0 || chassis is { Count: > 0 }
            ? new LegendDrawing(
                outline,
                scaleX,
                scaleY,
                offsetX,
                offsetY,
                drawnKeys,
                chassis)
            : null;

        var measured = layout with { Keys = keys, Canvas = canvas, Legend = legend };

        // The drawing comes from another program and may not describe what we think it does. If
        // laying it over the layout produces something that could not be drawn — keys on top of
        // one another — the drawn geometry is kept. Better a keyboard of roughly the right shape
        // than one that cannot be shown at all.
        return DeviceProfileValidator.Validate(measured).Count == 0
            ? measured
            : layout with { Legend = legend };
    }

    /// <summary>
    /// The same key, with every one of its rectangles scaled into the given box.
    /// </summary>
    /// <remarks>
    /// For the keys that are not one rectangle. The proportions are kept, so an L stays an L and
    /// the step stays where it was, while the key as a whole comes to occupy what the drawing says
    /// it occupies — which is what lets it sit among neighbours that have taken the drawing's
    /// measurements without overlapping any of them.
    /// </remarks>
    private static KeyDefinition FittedInto(
        KeyDefinition key, double left, double top, double width, double height)
    {
        var from = Bounds(key.Areas().Select(a => (a.X, a.Y, a.Width, a.Height)));

        if (from.Width <= 0 || from.Height <= 0)
        {
            return key;
        }

        var sx = width / from.Width;
        var sy = height / from.Height;

        KeyArea Fit(double x, double y, double w, double h) => new(
            left + ((x - from.Left) * sx),
            top + ((y - from.Top) * sy),
            w * sx,
            h * sy);

        var main = Fit(key.X, key.Y, key.Width, key.Height);

        return key with
        {
            X = main.X,
            Y = main.Y,
            Width = main.Width,
            Height = main.Height,
            Parts = [.. (key.Parts ?? []).Select(p => Fit(p.X, p.Y, p.Width, p.Height))],
        };
    }

    /// <summary>The same key, moved by a fixed amount, with all of its rectangles.</summary>
    private static KeyDefinition Shifted(KeyDefinition key, double dx, double dy)
        => key with
        {
            X = key.X + dx,
            Y = key.Y + dy,
            Parts = key.Parts is null
                ? null
                : [.. key.Parts.Select(p => new KeyArea(p.X + dx, p.Y + dy, p.Width, p.Height))],
        };

    /// <summary>The rectangle a set of keys occupies together.</summary>
    private static (double Left, double Top, double Width, double Height) Bounds(
        IEnumerable<(double X, double Y, double Width, double Height)> keys)
    {
        double left = double.MaxValue, top = double.MaxValue;
        double right = double.MinValue, bottom = double.MinValue;
        var any = false;

        foreach (var (x, y, w, h) in keys)
        {
            any = true;
            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x + w);
            bottom = Math.Max(bottom, y + h);
        }

        return any ? (left, top, right - left, bottom - top) : (0, 0, 0, 0);
    }

    /// <summary>
    /// The unclaimed drawn key closest to where the profile puts this one, if it is close enough
    /// to be the same key. The tolerance is a third of a key width — wide enough for the two
    /// layouts to disagree slightly, far too narrow to confuse neighbours.
    /// </summary>
    private static SvgKey? Nearest(
        List<(double X, double Y, SvgKey Key)> drawn,
        KeyDefinition key,
        HashSet<SvgKey> taken)
    {
        SvgKey? best = null;
        var bestDistance = double.MaxValue;

        // A key of several rectangles is compared by everything it covers, not by its main
        // rectangle. The ISO Enter is why: its main rectangle is the narrow upper half, while the
        // drawing's outline starts at the wider lower half, and comparing those two corners put
        // them far enough apart to fall outside the tolerance. The Enter then had no counterpart
        // and was the one key on the board left without its printed legend.
        var area = key.Parts is { Count: > 0 }
            ? Bounds(key.Areas().Select(a => (a.X, a.Y, a.Width, a.Height)))
            : (Left: key.X, Top: key.Y, Width: key.Width, Height: key.Height);

        var tolerance = area.Width / 3;

        foreach (var (x, y, candidate) in drawn)
        {
            if (taken.Contains(candidate))
            {
                continue;
            }

            var dx = x - area.Left;
            var dy = y - area.Top;
            var distance = (dx * dx) + (dy * dy);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return bestDistance <= tolerance * tolerance ? best : null;
    }

    /// <summary>
    /// The legend a key carries on this hardware. Only one key needs correcting: a layout drawn
    /// for ordinary keyboards prints the Windows symbol where these keyboards have fn, and a key
    /// labelled with the wrong symbol is worse than one labelled with none.
    /// </summary>
    private static string? LabelFor(KeyDefinition key)
        => key.Id == "Keyboard_RightGui" ? "fn" : key.Label;

    /// <summary>The scan codes the service reported for this keyboard.</summary>
    internal static HashSet<ushort> Reported(SdkDeviceDescription device)
    {
        var reported = new HashSet<ushort>();

        foreach (var key in device.Keys)
        {
            reported.Add((ushort)(key.Extended ? ScanCodes.ExtendedPrefix | key.Scancode : key.Scancode));
        }

        return reported;
    }

    /// <summary>
    /// The scan code a key sends, or <c>null</c> for one that sends nothing.
    /// </summary>
    /// <remarks>
    /// The profile's own <c>scanCode</c> wins, because a physical layout can disagree with the
    /// US-based key naming. Asking per key rather than translating the reported codes back into
    /// names matters: several keys share a code — the ISO <c>#</c> sits where ANSI has the
    /// backslash, and Pause shares Num Lock's — so the reverse direction is ambiguous and would
    /// silently drop whichever key it did not pick.
    /// </remarks>
    internal static ushort? ScanCodeOf(KeyDefinition key)
    {
        if (key.ScanCode is { } explicitCode)
        {
            return (ushort)explicitCode;
        }

        return ScanCodes.TryGet(key.Id, out var code) ? code : null;
    }
}
