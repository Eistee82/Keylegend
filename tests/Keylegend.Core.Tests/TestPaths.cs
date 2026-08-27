using System.Reflection;

namespace Keylegend.Core.Tests;

/// <summary>
/// Locates files inside the repository. The root is baked into the assembly at build time
/// (see the csproj) rather than discovered by walking up the directory tree — that search is
/// ambiguous on a case-insensitive file system, where a source folder named <c>Devices</c>
/// matches just as well as the repository's <c>devices</c>.
/// </summary>
internal static class TestPaths
{
    public static string RepositoryRoot { get; } =
        typeof(TestPaths).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "RepositoryRoot")?.Value
        ?? throw new InvalidOperationException(
            "The RepositoryRoot assembly metadata is missing; check the test project file.");

    public static string ProfilesDirectory { get; } = Directory.Exists(Path.Combine(RepositoryRoot, "profiles"))
        ? Path.Combine(RepositoryRoot, "profiles")
        : throw new DirectoryNotFoundException($"No 'profiles' folder under {RepositoryRoot}.");

    /// <summary>
    /// The hand-measured key table, under <c>tests/…/Fixtures</c>.
    /// </summary>
    /// <remarks>
    /// Test data and nothing else: the program builds the attached keyboard at run time, so it
    /// never reads a keyboard from a file. What this file holds is the measurement everything is
    /// checked against — see <see cref="MeasuredKeys"/>.
    /// </remarks>
    public static string MeasuredKeys { get; } = Path.Combine(
        RepositoryRoot, "tests", "Keylegend.Core.Tests", "Fixtures",
        "razer-deathstalker-v2-de", "measured-keys.json");
}
