using Keylegend.Chroma;

namespace Keylegend.Core.Tests.Chroma;

/// <summary>
/// Naming the shape of the attached board from the layout id the lighting service reports.
/// </summary>
/// <remarks>
/// The name is what the window puts beside the model — "ISO-DE · 105 keys" — so it is the one
/// statement about the keyboard a user can check at a glance and the one that looks wrong first.
/// It is descriptive only: nothing is chosen by it, which is why an unknown id must fall back
/// rather than fail.
/// </remarks>
public class LayoutTemplatesTests
{
    /// <summary>The ids that matter, including the one this was developed against.</summary>
    [Theory]
    [InlineData(1, "ANSI-US")]
    [InlineData(3, "ISO-DE")]
    [InlineData(4, "ISO-FR")]
    [InlineData(12, "JIS-JP")]
    [InlineData(13, "ABNT2-BR")]
    [InlineData(16, "ISO-ES")]
    public void NamesTheLayoutsTheServiceReports(int layoutId, string expected)
        => Assert.Equal(expected, LayoutTemplates.NameOf(layoutId, iso: true, japanese: false));

    /// <summary>
    /// An id nobody has seen is no reason to give up: the drawing settles the shape either way,
    /// because an ISO board carries the extra key beside Enter and a JIS board the Japanese ones.
    /// </summary>
    [Theory]
    [InlineData(false, false, "ANSI-US")]
    [InlineData(true, false, "ISO")]
    [InlineData(false, true, "JIS-JP")]
    [InlineData(true, true, "JIS-JP")]
    public void FallsBackToWhatTheDrawingShows(bool iso, bool japanese, string expected)
        => Assert.Equal(expected, LayoutTemplates.NameOf(9999, iso, japanese));

    /// <summary>
    /// A known id wins over the drawing's shape, because it is the more specific statement: the
    /// drawing can tell ISO from ANSI, but only the id says which ISO.
    /// </summary>
    [Fact]
    public void TheReportedIdIsMoreSpecificThanTheShape()
    {
        Assert.Equal("ISO-DE", LayoutTemplates.NameOf(3, iso: true, japanese: false));
        Assert.Equal("ISO-DE", LayoutTemplates.NameOf(3, iso: false, japanese: false));
    }

    /// <summary>
    /// Never empty and never null. The name goes straight into the window's header, where a blank
    /// would read as a keyboard the program failed to identify.
    /// </summary>
    [Fact]
    public void AlwaysAnswersWithAName()
    {
        for (var layoutId = -1; layoutId < 40; layoutId++)
        {
            foreach (var iso in new[] { true, false })
            {
                foreach (var japanese in new[] { true, false })
                {
                    Assert.False(string.IsNullOrWhiteSpace(
                        LayoutTemplates.NameOf(layoutId, iso, japanese)));
                }
            }
        }
    }
}
