using Keylegend.Core.Devices;
using Keylegend.Core.Input;
using Keylegend.Core.Lighting;
using Keylegend.Core.Lighting.Effects;

namespace Keylegend.Core.Tests.Lighting;

/// <summary>
/// The eight keystroke effects, each checked against the one thing it exists to do.
/// </summary>
/// <remarks>
/// No hardware, no window and no waiting: every effect is a pure function of events, geometry and
/// time, so a test sets the moment and asks for the value. That is the whole reason the interface
/// takes the time rather than reading a clock.
/// </remarks>
public class KeyEffectsTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset At(double seconds) => T0.AddSeconds(seconds);

    /// <summary>Ten keys in a row, each ten wide — so a distance of thirty is three key widths.</summary>
    private static AttachedKeyboard Row()
    {
        var keys = new List<KeyDefinition>();

        for (var i = 0; i < 10; i++)
        {
            keys.Add(new KeyDefinition($"Key{i}", i * 10, 0, 10, 10, Row: 0, Column: i));
        }

        return new AttachedKeyboard("Test", "ISO-DE", new Canvas(100, 10), new MatrixSize(6, 22), keys);
    }

    private static KeyDefinition Key(int index) => Row().Keys[index];

    /// <summary>Presses a key at one moment and lets go at another, driving the effect between.</summary>
    private static KeyActivity Pressed(string id, DateTimeOffset at)
    {
        var activity = new KeyActivity();
        activity.Observe([id], at);

        return activity;
    }

    // ------------------------------------------------------------------ Faden

    [Fact]
    public void FadeTurnsAKeyOffWhileItIsHeld()
    {
        var effect = new FadeEffect();
        var activity = Pressed("Key0", T0);

        effect.Advance(activity, T0);

        Assert.Equal(0, effect.TintFor(Key(0), T0).Factor);
    }

    /// <summary>The contract of the effect: from off back to full over exactly its duration.</summary>
    [Fact]
    public void FadeComesBackEvenlyOverItsDuration()
    {
        var effect = new FadeEffect(TimeSpan.FromSeconds(1));
        var activity = Pressed("Key0", T0);
        activity.Observe([], At(0.5));

        effect.Advance(activity, At(1.0));

        Assert.Equal(0.5, effect.TintFor(Key(0), At(1.0)).Factor, precision: 3);
        Assert.Equal(1.0, effect.TintFor(Key(0), At(1.5)).Factor, precision: 3);
    }

    [Fact]
    public void FadeLeavesKeysNobodyTouchedAlone()
    {
        var effect = new FadeEffect();
        var activity = Pressed("Key0", T0);

        effect.Advance(activity, T0);

        Assert.Equal(KeyTint.Untouched, effect.TintFor(Key(5), T0));
    }

    [Fact]
    public void FadeStopsAnimatingOnceTheLastKeyIsBack()
    {
        var effect = new FadeEffect(TimeSpan.FromSeconds(1));
        var activity = Pressed("Key0", T0);
        activity.Observe([], At(0.1));

        effect.Advance(activity, At(0.5));
        Assert.True(effect.Animating);

        effect.Advance(activity, At(3));
        Assert.False(effect.Animating);
    }

    // ------------------------------------------------------------------ Aufblitzen

    /// <summary>
    /// White at full brightness at the stroke: a key already showing pure blue has no brighter
    /// blue to go to.
    /// </summary>
    [Fact]
    public void FlashGoesWhiteAtTheMomentOfTheStroke()
    {
        var effect = new FlashEffect(TimeSpan.FromSeconds(0.2));
        var activity = Pressed("Key0", T0);

        effect.Advance(activity, T0);

        var struck = effect.TintFor(Key(0), T0);

        Assert.Equal(KeyTint.Brightest, struck.Colour);
        Assert.Equal(1.0, struck.Mix, precision: 3);
        Assert.True(effect.TintFor(Key(0), At(0.1)).Mix < 1.0);
    }

    [Fact]
    public void FlashIsOverWhenItsDurationIs()
    {
        var effect = new FlashEffect(TimeSpan.FromSeconds(0.2));
        var activity = Pressed("Key0", T0);

        effect.Advance(activity, At(0.3));

        Assert.Equal(KeyTint.Untouched, effect.TintFor(Key(0), At(0.3)));
        Assert.False(effect.Animating);
    }

    // ------------------------------------------------------------------ Nachleuchten

    [Fact]
    public void AfterglowStaysBrightWhileTheKeyIsHeld()
    {
        var effect = new AfterglowEffect(TimeSpan.FromSeconds(1), peak: 0.8);
        var activity = Pressed("Key0", T0);

        effect.Advance(activity, At(5));

        Assert.Equal(0.8, effect.TintFor(Key(0), At(5)).Mix, precision: 3);
    }

    [Fact]
    public void AfterglowDiesAwayAfterTheRelease()
    {
        var effect = new AfterglowEffect(TimeSpan.FromSeconds(1), peak: 0.8);
        var activity = Pressed("Key0", T0);
        activity.Observe([], At(0.1));
        effect.Advance(activity, At(0.6));

        var half = effect.TintFor(Key(0), At(0.6)).Mix;

        Assert.InRange(half, 0.3, 0.5);
        Assert.Equal(KeyTint.Untouched, effect.TintFor(Key(0), At(1.2)));
    }

    // ------------------------------------------------------------------ Wassertropfen

    /// <summary>When a key is at its brightest under an effect, and how bright it ever gets.</summary>
    private static (double When, double Most) Peak(IKeyEffect effect, KeyDefinition key)
    {
        var most = -1.0;
        var when = 0.0;

        for (var t = 0.0; t < 2.0; t += 0.005)
        {
            var mix = effect.TintFor(key, At(t)).Mix;

            if (mix > most)
            {
                most = mix;
                when = t;
            }
        }

        return (when, most);
    }

    /// <summary>
    /// Stated as the property rather than at fixed moments: the ring passes the near key before
    /// the far one. Written this way so that tuning the speed cannot quietly turn it into a test
    /// of nothing.
    /// </summary>
    [Fact]
    public void RippleReachesADistantKeyLaterThanANearOne()
    {
        var effect = new RippleEffect(Row());

        effect.Advance(Pressed("Key0", T0), T0);

        var near = Peak(effect, Key(1));
        var far = Peak(effect, Key(5));

        Assert.True(near.When < far.When, $"near peaked at {near.When} s, far at {far.When} s");
    }

    /// <summary>
    /// The complaint this was tuned against: the wave did not reach across the keyboard. It ran at
    /// a fixed six key heights a second and died about a third of the way over a full-size board.
    /// It now crosses whatever board it is on, because the distance from corner to corner is what
    /// it is given to travel.
    /// </summary>
    [Fact]
    public void RippleReachesTheFarSideOfTheBoard()
    {
        var effect = new RippleEffect(Row());

        effect.Advance(Pressed("Key0", T0), T0);

        var farthest = Peak(effect, Key(9));

        Assert.True(farthest.Most > 0.5, $"the far corner only ever reached {farthest.Most}");
    }

    [Fact]
    public void RippleLeavesTheBoardAsItFoundIt()
    {
        var effect = new RippleEffect(Row());
        var activity = Pressed("Key0", T0);
        activity.Observe([], At(0.05));

        effect.Advance(activity, At(5));

        Assert.False(effect.Animating);
        Assert.Equal(KeyTint.Untouched, effect.TintFor(Key(3), At(5)));
    }

    // ------------------------------------------------------------------ Dunkle Welle

    [Fact]
    public void DarkWaveDimsWhereTheRippleWouldBrighten()
    {
        var activity = Pressed("Key0", T0);

        var ripple = new RippleEffect(Row());
        var dark = new DarkWaveEffect(Row());

        ripple.Advance(activity, T0);
        dark.Advance(activity, T0);

        var lit = ripple.TintFor(Key(2), At(0.3)).Mix;
        var dimmed = dark.TintFor(Key(2), At(0.3)).Factor;

        Assert.True(lit > 0, $"the ripple should brighten, got {lit}");
        Assert.True(dimmed < 1.0, $"the dark wave should dim, got {dimmed}");
    }

    // ------------------------------------------------------------------ Einschlag

    [Fact]
    public void ImpactHitsTheStruckKeyHardestAndItsNeighboursLess()
    {
        var effect = new ImpactEffect(Row());
        var activity = Pressed("Key3", T0);

        effect.Advance(activity, T0);

        var struck = effect.TintFor(Key(3), At(0.03)).Mix;
        var beside = effect.TintFor(Key(4), At(0.06)).Mix;

        Assert.True(struck > beside, $"struck {struck} should beat its neighbour {beside}");
        Assert.True(beside > 0, $"the neighbour should be touched at all, got {beside}");
        Assert.Equal(KeyTint.Untouched, effect.TintFor(Key(9), At(0.06)));
    }

    // ------------------------------------------------------------------ Funken

    [Fact]
    public void SparksLandOnKeysNearTheStrokeAndBringTheirOwnColour()
    {
        var effect = new SparkEffect(Row(), new Random(1));
        var activity = Pressed("Key4", T0);

        effect.Advance(activity, T0);

        var touched = Row().Keys
            .Where(k => effect.TintFor(k, At(0.05)).Mix > 0)
            .ToList();

        Assert.NotEmpty(touched);
        Assert.All(touched, k => Assert.NotNull(effect.TintFor(k, At(0.05)).Colour));

        // Near the stroke, not scattered across the whole board.
        Assert.All(touched, k => Assert.InRange(Math.Abs(k.X - Key(4).X), 0, 40));
    }

    /// <summary>
    /// The one effect with chance in it, and therefore the one that has to be able to repeat
    /// itself: the same seed must produce the same sparks, or nothing about it could be tested.
    /// </summary>
    [Fact]
    public void SparksFallTheSameWayForTheSameSeed()
    {
        static IReadOnlyList<double> Run()
        {
            var effect = new SparkEffect(Row(), new Random(7));
            effect.Advance(Pressed("Key4", T0), T0);

            return [.. Row().Keys.Select(k => effect.TintFor(k, At(0.05)).Mix)];
        }

        Assert.Equal(Run(), Run());
    }

    // ------------------------------------------------------------------ Hitze

    [Fact]
    public void HeatBuildsUpWithRepeatedStrokes()
    {
        var effect = new HeatEffect();
        var activity = new KeyActivity();

        activity.Observe(["Key0"], T0);
        effect.Advance(activity, T0);
        var once = effect.TintFor(Key(0), T0).Mix;

        for (var i = 1; i <= 5; i++)
        {
            activity.Observe([], At(i * 0.1));
            effect.Advance(activity, At(i * 0.1));
            activity.Observe(["Key0"], At((i * 0.1) + 0.05));
            effect.Advance(activity, At((i * 0.1) + 0.05));
        }

        Assert.True(effect.TintFor(Key(0), At(0.6)).Mix > once);
    }

    [Fact]
    public void HeatCoolsDownAgain()
    {
        var effect = new HeatEffect();
        var activity = new KeyActivity();

        activity.Observe(["Key0"], T0);
        effect.Advance(activity, T0);
        var hot = effect.TintFor(Key(0), T0).Mix;

        activity.Observe([], At(0.1));

        for (var t = 1.0; t <= 20.0; t += 0.5)
        {
            effect.Advance(activity, At(t));
        }

        Assert.True(effect.TintFor(Key(0), At(20)).Mix < hot);
        Assert.False(effect.Animating);
    }

    // ------------------------------------------------------------------ was für alle gilt

    public static TheoryData<string> EveryEffect() =>
        ["fade", "flash", "afterglow", "ripple", "darkwave", "impact", "sparks", "heat"];

    private static IKeyEffect Build(string name) => name switch
    {
        "fade" => new FadeEffect(),
        "flash" => new FlashEffect(),
        "afterglow" => new AfterglowEffect(),
        "ripple" => new RippleEffect(Row()),
        "darkwave" => new DarkWaveEffect(Row()),
        "impact" => new ImpactEffect(Row()),
        "sparks" => new SparkEffect(Row(), new Random(3)),
        "heat" => new HeatEffect(),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    /// <summary>
    /// Nothing typed, nothing changed. Without this an effect could quietly hold the lighting
    /// awake for ever, because the send rate follows <c>Animating</c> alone.
    /// </summary>
    [Theory]
    [MemberData(nameof(EveryEffect))]
    public void LeavesAKeyboardNobodyIsTypingOnExactlyAsItWas(string name)
    {
        var effect = Build(name);
        var activity = new KeyActivity();

        effect.Advance(activity, T0);

        Assert.False(effect.Animating);
        Assert.All(Row().Keys, key => Assert.Equal(KeyTint.Untouched, effect.TintFor(key, T0)));
    }

    /// <summary>A change of selection must not leave anything of the old effect in flight.</summary>
    [Theory]
    [MemberData(nameof(EveryEffect))]
    public void ForgetsEverythingWhenReset(string name)
    {
        var effect = Build(name);

        effect.Advance(Pressed("Key2", T0), T0);
        effect.Reset();

        Assert.False(effect.Animating);
        Assert.All(Row().Keys, key => Assert.Equal(KeyTint.Untouched, effect.TintFor(key, At(0.05))));
    }
}
