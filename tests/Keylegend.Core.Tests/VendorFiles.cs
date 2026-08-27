namespace Keylegend.Core.Tests;

/// <summary>
/// Why the tests that read Razer's own files stop without reaching a verdict.
/// </summary>
/// <remarks>
/// <para>
/// Those files are the vendor's drawings of its keyboards, and the parser that reads them can
/// only be checked against the real ones: a fixture written here would prove that the parser
/// still understands the fixture. They may not be copied into this repository either — they are
/// someone else's artwork and the licence here is MIT.
/// </para>
/// <para>
/// So on a machine without Razer Synapse there is nothing to read, and the tests concerned say
/// so through <c>Assert.Skip</c> rather than passing. That distinction is the whole reason this
/// project is on xUnit v3: a run that reports them green would be claiming a parser was checked
/// when it was not, and the number that gets quoted afterwards would be wrong.
/// </para>
/// </remarks>
internal static class VendorFiles
{
    /// <summary>No drawings on this machine — Synapse is not installed, or keeps them elsewhere.</summary>
    public const string Absent =
        "Razer Synapse is not installed here, so its keyboard drawings are not on this machine. "
        + "There is nothing for this test to read, and the drawings may not be copied into the "
        + "repository. Run it on a machine that has Synapse.";

    /// <summary>Drawings are here, but none for the keyboard this test describes.</summary>
    public const string NoDrawingForTheDevice =
        "Razer Synapse is installed, but keeps no drawing for the keyboard this test describes "
        + "(a DeathStalker V2, German layout). Synapse downloads a model's drawing when that "
        + "model is first attached, so this needs a machine that has had one plugged in.";
}
