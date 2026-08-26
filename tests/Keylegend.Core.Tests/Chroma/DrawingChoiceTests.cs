using Keylegend.Chroma;

namespace Keylegend.Core.Tests.Chroma;

/// <summary>
/// Which drawing is believed, checked against a cache written for the test.
/// </summary>
/// <remarks>
/// <para>
/// The choice is the whole job of <c>SvgLayoutSource</c>, and getting it wrong is not a crash but a
/// keyboard that quietly shows the wrong thing: the drawings for one model are identical except for
/// the characters printed on the caps, so a German board picking up the Italian drawing looks
/// perfectly fine and is wrong on every key that differs. That is not hypothetical — it is what
/// matching by shape did before the product and layout were read.
/// </para>
/// <para>
/// These run everywhere, because a cache of files is something a test can write. The tests that
/// read the real installation stay: they check that the vendor's files still look like this.
/// </para>
/// </remarks>
public class DrawingChoiceTests
{
    private const int DeathStalkerProductId = 661;

    private static SdkDeviceDescription Device(int productId = DeathStalkerProductId, int layoutId = 3)
        => new(
            "Razer DeathStalker V2", VendorId: 5426, ProductId: productId, LayoutId: layoutId,
            MatrixRows: 6, MatrixColumns: 22,
            Keys: [new SdkKey(1, false, 0, 1), new SdkKey(30, false, 1, 1)]);

    [Fact]
    public void FindsTheDrawingForTheAttachedDevice()
    {
        using var cache = DrawingCache.Create();
        cache.Write("a1b2c3", DeathStalkerProductId, layoutId: 3, DrawingCache.FullSizeIsoKeys);

        var found = SvgLayoutSource.Find(Device(), cache.Directories);

        Assert.NotNull(found);
        Assert.Equal(DeathStalkerProductId, found.ProductId);
        Assert.Equal(3, found.LayoutId);
        Assert.Equal(DrawingCache.FullSizeIsoKeys.Count, found.Keys.Count);
    }

    /// <summary>
    /// The decisive one. Two drawings of the same keyboard, differing only in layout — the one the
    /// service named has to win, because nothing in the picture tells them apart.
    /// </summary>
    [Fact]
    public void PicksTheLayoutTheServiceNamesRatherThanTheFirstThatFits()
    {
        using var cache = DrawingCache.Create();

        cache.Write("italian", DeathStalkerProductId, layoutId: 17, DrawingCache.FullSizeIsoKeys);
        cache.Write("german", DeathStalkerProductId, layoutId: 3, DrawingCache.FullSizeIsoKeys);
        cache.Write("spanish", DeathStalkerProductId, layoutId: 16, DrawingCache.FullSizeIsoKeys);

        var found = SvgLayoutSource.Find(Device(layoutId: 3), cache.Directories);

        Assert.NotNull(found);
        Assert.Equal(3, found.LayoutId);
    }

    /// <summary>Another model's drawing is not this keyboard, however well it fits.</summary>
    [Fact]
    public void IgnoresADrawingForAnotherProduct()
    {
        using var cache = DrawingCache.Create();
        cache.Write("other", productId: 555, layoutId: 3, DrawingCache.FullSizeIsoKeys);

        Assert.Null(SvgLayoutSource.Find(Device(), cache.Directories));
    }

    /// <summary>And a drawing of this model in a layout the device does not have is not it either.</summary>
    [Fact]
    public void IgnoresADrawingForAnotherLayout()
    {
        using var cache = DrawingCache.Create();
        cache.Write("italian", DeathStalkerProductId, layoutId: 17, DrawingCache.FullSizeIsoKeys);

        Assert.Null(SvgLayoutSource.Find(Device(layoutId: 3), cache.Directories));
    }

    /// <summary>
    /// A cache is full of files that are not drawings. Reading every one of them would turn a
    /// start-up into a disk scan, so anything too small to hold a keyboard is skipped unread.
    /// </summary>
    [Fact]
    public void SkipsFilesTooSmallToBeADrawing()
    {
        using var cache = DrawingCache.Create();
        cache.Write("tiny", DeathStalkerProductId, layoutId: 3, DrawingCache.FullSizeIsoKeys, padToBytes: 0);

        Assert.Null(SvgLayoutSource.Find(Device(), cache.Directories));
    }

    /// <summary>A file without the keyed group is not a drawing, whatever else it holds.</summary>
    [Fact]
    public void SkipsFilesWithoutTheKeyedGroup()
    {
        using var cache = DrawingCache.Create();
        cache.Write(
            "notadrawing", DeathStalkerProductId, layoutId: 3, DrawingCache.FullSizeIsoKeys,
            includeLedGroup: false);

        Assert.Null(SvgLayoutSource.Find(Device(), cache.Directories));
    }

    /// <summary>The real cache is mostly rubbish as far as this is concerned, and that is fine.</summary>
    [Fact]
    public void FindsTheDrawingAmongFilesThatAreNotOne()
    {
        using var cache = DrawingCache.Create();

        File.WriteAllText(Path.Combine(cache.Directory, "index"), new string('x', 30_000));
        File.WriteAllBytes(Path.Combine(cache.Directory, "blob"), new byte[25_000]);
        cache.Write("wrong-product", productId: 999, layoutId: 3, DrawingCache.FullSizeIsoKeys);
        cache.Write("right", DeathStalkerProductId, layoutId: 3, DrawingCache.FullSizeIsoKeys);

        var found = SvgLayoutSource.Find(Device(), cache.Directories);

        Assert.NotNull(found);
        Assert.Equal(DeathStalkerProductId, found.ProductId);
    }

    /// <summary>An empty cache, or none at all, is not an error — the caller says so and stops.</summary>
    [Fact]
    public void AnswersWithNothingWhenThereIsNoDrawing()
    {
        using var cache = DrawingCache.Create();

        Assert.Null(SvgLayoutSource.Find(Device(), cache.Directories));
        Assert.Null(SvgLayoutSource.Find(Device(), [Path.Combine(cache.Directory, "gone")]));
    }

    /// <summary>
    /// The whole chain, from a file on disk to a keyboard the program can light: the drawing is
    /// found, read, and turned into a profile whose keys carry the cells the protocol assigns.
    /// </summary>
    [Fact]
    public void BuildsAKeyboardFromACacheOnDisk()
    {
        using var cache = DrawingCache.Create();
        cache.Write("chain", DeathStalkerProductId, layoutId: 3, DrawingCache.FullSizeIsoKeys);

        var device = Device();
        var drawing = SvgLayoutSource.Find(device, cache.Directories);

        Assert.NotNull(drawing);

        var built = AttachedKeyboardBuilder.FromDrawing(device, drawing);

        Assert.NotNull(built);
        Assert.Equal("ISO-DE", built.PhysicalLayout);
        Assert.NotEmpty(built.Keys);
        Assert.All(built.Keys, key => Assert.NotNull(key.Row));
    }
}
