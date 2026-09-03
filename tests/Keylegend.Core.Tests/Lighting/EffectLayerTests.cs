using Keylegend.Core.Devices;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Lighting.Effects;

namespace Keylegend.Core.Tests.Lighting;

/// <summary>
/// The layer that lets a keystroke effect touch the finished frame.
/// </summary>
/// <remarks>
/// It runs after the composer, never inside it. The composer stays a pure function of the
/// keyboard state, which is what keeps the picture in the window and the light on the desk the
/// same thing — an effect is a change over time laid on top, not a different way of deciding
/// what a key means.
/// </remarks>
public class EffectLayerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    private static AttachedKeyboard Keyboard() => new(
        "Test", "ISO-DE", new Canvas(100, 40), new MatrixSize(6, 22),
        [
            new KeyDefinition("Keyboard_A", 0, 0, 10, 10, Row: 3, Column: 2),
            new KeyDefinition("Keyboard_B", 10, 0, 10, 10, Row: 3, Column: 3),
            new KeyDefinition("Keyboard_Nowhere", 20, 0, 10, 10, Row: null, Column: null)
        ]);

    /// <summary>Hands out a fixed tint, so the layer's own arithmetic is what is under test.</summary>
    private sealed class FixedTint(KeyTint tint) : IKeyEffect
    {
        public int Advances { get; private set; }

        public bool Animating => true;

        public void Advance(KeyActivity activity, DateTimeOffset now) => Advances++;

        public KeyTint TintFor(KeyDefinition key, DateTimeOffset now) => tint;

        public void Reset() { }
    }

    private static LedFrame Painted(RgbColor colour)
    {
        var frame = new LedFrame(6, 22);
        frame.Set(3, 2, colour);
        frame.Set(3, 3, colour);

        return frame;
    }

    private static (LedFrame Frame, EffectLayer Layer) Apply(KeyTint tint, RgbColor painted)
    {
        var frame = Painted(painted);
        var layer = new EffectLayer(Keyboard()) { Effect = new FixedTint(tint) };

        layer.Advance(new KeyActivity(), Now);
        layer.Paint(frame, Now);

        return (frame, layer);
    }

    [Fact]
    public void DimsAKeyByItsFactor()
    {
        var (frame, _) = Apply(new KeyTint(0.5, null, 0), new RgbColor(200, 100, 0));

        Assert.Equal(new RgbColor(100, 50, 0), frame[3, 2]);
    }

    [Fact]
    public void TurnsAKeyOffAtAFactorOfZero()
    {
        var (frame, _) = Apply(new KeyTint(0, null, 0), new RgbColor(200, 100, 0));

        Assert.Equal(RgbColor.Off, frame[3, 2]);
    }

    /// <summary>
    /// A factor can only take light away; one above one is ignored rather than pretending.
    /// </summary>
    /// <remarks>
    /// The mistake this replaces: brightening used to be a factor. On the shipped palette that
    /// does nothing at all — every colour runs one channel at 255 and the rest at 0, so
    /// multiplying hits the ceiling on one and leaves the others at zero. Four of the eight
    /// effects were invisible on a lit keyboard, which is exactly how it was reported.
    /// </remarks>
    [Fact]
    public void CannotBrightenByFactorBecauseAFullChannelHasNowhereToGo()
    {
        var (frame, _) = Apply(new KeyTint(4.0, null, 0), new RgbColor(0, 0, 255));

        Assert.Equal(new RgbColor(0, 0, 255), frame[3, 2]);
    }

    /// <summary>
    /// And the way that does work: white mixed in. At full strength a saturated key becomes white
    /// at full brightness, which is the only shade brighter than a saturated colour.
    /// </summary>
    [Fact]
    public void BrightensASaturatedKeyByMixingWhiteIntoIt()
    {
        var (full, _) = Apply(KeyTint.Lit(1.0), new RgbColor(0, 0, 255));

        Assert.Equal(new RgbColor(255, 255, 255), full[3, 2]);

        var (half, _) = Apply(KeyTint.Lit(0.5), new RgbColor(0, 0, 255));

        Assert.Equal(new RgbColor(128, 128, 255), half[3, 2]);
    }

    [Fact]
    public void LeavesTheKeyAloneAtNoBrightening()
    {
        var (frame, _) = Apply(KeyTint.Lit(0), new RgbColor(0, 0, 255));

        Assert.Equal(new RgbColor(0, 0, 255), frame[3, 2]);
    }

    [Fact]
    public void MixesAnEffectColourOverTheDimmedOne()
    {
        // Damped to nothing first, then the effect's own colour laid fully over it.
        var (full, _) = Apply(new KeyTint(0, new RgbColor(0, 0, 255), 1.0), new RgbColor(200, 0, 0));

        Assert.Equal(new RgbColor(0, 0, 255), full[3, 2]);

        // Half and half, against an undamped key.
        var (half, _) = Apply(new KeyTint(1.0, new RgbColor(0, 0, 200), 0.5), new RgbColor(100, 0, 0));

        Assert.Equal(new RgbColor(50, 0, 100), half[3, 2]);
    }

    [Fact]
    public void LeavesTheFrameAloneWithoutAnEffect()
    {
        var frame = Painted(new RgbColor(200, 100, 0));
        var layer = new EffectLayer(Keyboard());

        layer.Advance(new KeyActivity(), Now);
        layer.Paint(frame, Now);

        Assert.Equal(new RgbColor(200, 100, 0), frame[3, 2]);
        Assert.False(layer.Animating);
    }

    /// <summary>
    /// A key the drawing places but the lighting cannot address — the upper half of an ISO Enter
    /// has no LED of its own. Asking it for a cell would throw.
    /// </summary>
    [Fact]
    public void PassesOverKeysThatDriveNoCell()
    {
        var frame = Painted(new RgbColor(200, 100, 0));
        var layer = new EffectLayer(Keyboard()) { Effect = new FixedTint(new KeyTint(0, null, 0)) };

        layer.Advance(new KeyActivity(), Now);
        layer.Paint(frame, Now);

        // Nothing threw, and the two real keys were still served.
        Assert.Equal(RgbColor.Off, frame[3, 2]);
        Assert.Equal(RgbColor.Off, frame[3, 3]);
    }

    /// <summary>Once per frame, before any key is asked what it should look like.</summary>
    [Fact]
    public void AdvancesTheEffectOncePerFrame()
    {
        var effect = new FixedTint(new KeyTint(1, null, 0));
        var layer = new EffectLayer(Keyboard()) { Effect = effect };
        var frame = Painted(new RgbColor(10, 10, 10));

        layer.Advance(new KeyActivity(), Now);
        layer.Paint(frame, Now);
        layer.Advance(new KeyActivity(), Now);
        layer.Paint(frame, Now);

        Assert.Equal(2, effect.Advances);
    }

    /// <summary>
    /// Changing the selection must not let a half-finished wave run on into the next effect.
    /// </summary>
    [Fact]
    public void StartsTheNewEffectFromNothingWhenTheSelectionChanges()
    {
        var first = new ResettableEffect();
        var second = new ResettableEffect();
        var layer = new EffectLayer(Keyboard()) { Effect = first };

        layer.Effect = second;

        Assert.True(first.WasReset);
        Assert.True(second.WasReset);
    }

    private sealed class ResettableEffect : IKeyEffect
    {
        public bool WasReset { get; private set; }

        public bool Animating => false;

        public void Advance(KeyActivity activity, DateTimeOffset now) { }

        public KeyTint TintFor(KeyDefinition key, DateTimeOffset now) => KeyTint.Untouched;

        public void Reset() => WasReset = true;
    }

    /// <summary>
    /// The contract the engine leans on, and the one this got wrong first: an effect can say it
    /// is moving before a single frame has been painted.
    /// </summary>
    /// <remarks>
    /// <c>Animating</c> is what decides whether a frame is sent at all. When advancing was folded
    /// into painting, an effect could only announce itself on a frame that was being sent for some
    /// other reason — so a keystroke waited for the next insurance frame, up to three quarters of
    /// a second, before the lighting answered it. Measured that way: sixteen rounds with the key
    /// held, and the effect asleep through every one of them.
    /// </remarks>
    [Fact]
    public void SaysItIsMovingBeforeAnythingHasBeenPainted()
    {
        var layer = new EffectLayer(Keyboard()) { Effect = new FadeEffect() };
        var activity = new KeyActivity();
        activity.Observe(["Keyboard_A"], Now);

        layer.Advance(activity, Now);

        Assert.True(layer.Animating);
    }
}
