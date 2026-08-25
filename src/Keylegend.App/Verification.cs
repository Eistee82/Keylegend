using System.IO;
using System.Text;
using Keylegend.App.Localisation;
using Keylegend.Core.Devices;
using Keylegend.Core.Profiles;

namespace Keylegend.App;

/// <summary>
/// Checks that this copy of the program carries everything it needs, and answers with an exit
/// code. <c>--verify</c>, optionally followed by a path to write the findings to.
/// </summary>
/// <remarks>
/// <para>
/// For the release script, which assembles a staging directory and has to know whether what it
/// assembled would work. A build and a test pass against the build output; this runs the
/// published copy, which is a different thing and the place where two defects have hidden:
/// one executable overwriting the other, and data the project files did not carry into the
/// package.
/// </para>
/// <para>
/// It deliberately touches nothing outside the program. There is no keyboard on a build machine
/// and no Razer Synapse, so the attached device is out of scope here — what is in scope is
/// everything that travels inside the assemblies: the shipped profiles, the interface texts, and
/// the key matrix. Those are what a packaging mistake loses.
/// </para>
/// <para>
/// Findings go to a file rather than to the console because this is a windowed program: it has no
/// console attached, so anything written to standard output would go nowhere. The exit code is the
/// answer; the file is there to say what was wrong.
/// </para>
/// </remarks>
internal static class Verification
{
    private const string Flag = "--verify";

    /// <summary>Whether this run is a verification, and where to write the findings.</summary>
    public static bool Requested(string[] arguments, out string? reportPath)
    {
        reportPath = null;

        var at = Array.FindIndex(
            arguments, a => a.Equals(Flag, StringComparison.OrdinalIgnoreCase));

        if (at < 0)
        {
            return false;
        }

        if (at + 1 < arguments.Length && !arguments[at + 1].StartsWith("--", StringComparison.Ordinal))
        {
            reportPath = arguments[at + 1];
        }

        return true;
    }

    /// <summary>Runs every check. Zero if the copy is sound, one if it is not.</summary>
    public static int Run(string? reportPath)
    {
        var findings = new List<string>();
        var notes = new List<string>();

        // The shipped profiles live in the assembly as embedded resources. A package that lost
        // them starts and then colours nothing per application, with nothing to say why.
        try
        {
            var profiles = ShippedProfiles.All;

            if (ShippedProfiles.Problems.Length > 0)
            {
                findings.AddRange(ShippedProfiles.Problems.Select(p => $"profile: {p}"));
            }

            if (profiles.Length == 0)
            {
                findings.Add("no shipped profiles are embedded in this copy");
            }
            else
            {
                notes.Add($"{profiles.Length} shipped profiles");
            }
        }
        catch (Exception ex)
        {
            findings.Add($"the shipped profiles could not be read: {ex.Message}");
        }

        // The interface texts, one satellite assembly per language. A package that lost one
        // shows that language in English, which looks like a translation nobody wrote rather
        // than like a defect.
        var absent = Enum.GetValues<LanguageChoice>()
            .Where(language => !Texts.Carries(language))
            .ToList();

        if (absent.Count > 0)
        {
            findings.Add($"the texts are missing for: {string.Join(", ", absent)}");
        }
        else
        {
            notes.Add($"{Enum.GetValues<LanguageChoice>().Length - 1} languages");
        }

        // The key matrix is a table in code rather than data, so it cannot go missing in
        // packaging — but a check that says so is cheap, and it fails loudly if the table is
        // ever moved out into a file.
        if (StandardKeyMatrix.Cell("Keyboard_A") is null)
        {
            findings.Add("the key matrix does not know Keyboard_A");
        }

        Report(reportPath, findings, notes);

        return findings.Count == 0 ? 0 : 1;
    }

    private static void Report(string? path, List<string> findings, List<string> notes)
    {
        if (path is null)
        {
            return;
        }

        var text = new StringBuilder();

        text.AppendLine(findings.Count == 0
            ? "This copy is sound."
            : $"This copy has {findings.Count} problem(s):");

        foreach (var finding in findings)
        {
            text.AppendLine($"  - {finding}");
        }

        foreach (var note in notes)
        {
            text.AppendLine($"  {note}");
        }

        try
        {
            File.WriteAllText(path, text.ToString(), Encoding.UTF8);
        }
        catch (Exception)
        {
            // The exit code carries the answer either way, and a verification that fails
            // because it could not write its own notes would be reporting the wrong thing.
        }
    }
}
