using Keylegend.Core.Lighting;

namespace Keylegend.Core.Tests;

/// <summary>
/// The separation between the pure core and the platform adapters is what makes the
/// colouring logic testable without hardware. This test exists so that it stays that way.
/// </summary>
public class ArchitectureTests
{
    [Fact]
    public void CoreDependsOnNothingPlatformSpecific()
    {
        var referenced = typeof(RgbColor).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToArray();

        Assert.DoesNotContain("Keylegend.Windows", referenced);
        Assert.DoesNotContain("Keylegend.Chroma", referenced);
        Assert.DoesNotContain(referenced, name => name.StartsWith("System.Net", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.StartsWith("PresentationFramework", StringComparison.Ordinal));
    }
}
