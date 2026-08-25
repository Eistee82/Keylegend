using System.Text.Json;
using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.Devices;

/// <summary>
/// Guards the matrix positions taken from the lighting protocol. The decisive test is the last
/// one: the only profile that was calibrated against real hardware must agree with this table on
/// every single key. If it ever stops agreeing, the table is wrong — not the measurement.
/// </summary>
public class StandardKeyMatrixTests
{
    [Theory]
    [InlineData("Keyboard_Escape", 0, 1)]
    [InlineData("Keyboard_F1", 0, 3)]
    [InlineData("Keyboard_1", 1, 2)]
    [InlineData("Keyboard_A", 3, 2)]
    [InlineData("Keyboard_Space", 5, 7)]
    [InlineData("Keyboard_LeftCtrl", 5, 1)]
    public void PlacesKnownKeys(string keyId, int row, int column)
    {
        Assert.Equal((row, column), StandardKeyMatrix.Cell(keyId));
    }

    [Fact]
    public void ReportsNothingForAKeyItDoesNotPlace()
    {
        Assert.Null(StandardKeyMatrix.Cell("Keyboard_ThisIsNotAKey"));
    }

    /// <summary>
    /// Column 0 is the macro-key column that some models carry down their left edge. Nothing
    /// else belongs there — an ordinary key in column 0 would sit off the edge of every keyboard
    /// that has no macro keys.
    /// </summary>
    [Fact]
    public void OnlyMacroKeysSitInColumnZero()
    {
        var strays = StandardKeyMatrix.All
            .Where(c => c.Value.Column == 0 && !c.Key.StartsWith("Keyboard_Macro", StringComparison.Ordinal))
            .Select(c => c.Key)
            .ToArray();

        Assert.Empty(strays);
        Assert.Contains(StandardKeyMatrix.All, c => c.Key == "Keyboard_Macro1" && c.Value.Column == 0);
    }

    [Fact]
    public void EveryCellIsInsideTheMatrix()
    {
        foreach (var (key, cell) in StandardKeyMatrix.All)
        {
            Assert.True(cell.Row is >= 0 and < StandardKeyMatrix.Rows, $"{key} row {cell.Row}");
            Assert.True(cell.Column is >= 0 and < StandardKeyMatrix.Columns, $"{key} column {cell.Column}");
        }
    }

    /// <summary>
    /// Two keys in one cell would make one of them unlightable — unless they can never appear on
    /// the same keyboard. The layout-specific keys are exactly that case: a board is Japanese or
    /// Korean or European, never two of them, so those keys may share cells. Nothing else may.
    /// </summary>
    [Fact]
    public void OnlyLayoutExclusiveKeysShareACell()
    {
        static bool LayoutExclusive(string id)
            => id.StartsWith("Keyboard_Jp", StringComparison.Ordinal)
            || id.StartsWith("Keyboard_Kor", StringComparison.Ordinal)
            || id.StartsWith("Keyboard_NonUs", StringComparison.Ordinal)
            // fn sits where an ordinary keyboard has the right Windows key, and no keyboard has
            // both — verified below against every shipped profile.
            || id is "Keyboard_Function" or "Keyboard_RightGui";

        var doubled = StandardKeyMatrix.All
            .GroupBy(c => c.Value)
            .Where(g => g.Count() > 1 && !g.All(k => LayoutExclusive(k.Key)))
            .Select(g => $"{g.Key} <- {string.Join(", ", g.Select(k => k.Key))}")
            .ToArray();

        Assert.Empty(doubled);
    }

    /// <summary>
    /// Two keys may share a cell only when no keyboard carries both of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every pair here is a pair of keys from layouts that never appear on the same board. The
    /// Korean keys sit on the cells the Japanese and European ones use, because a Korean board has
    /// neither; and fn takes the cell of the right Windows key, because the protocol has no right
    /// Windows key and a board carries one or the other.
    /// </para>
    /// <para>
    /// The list is written out rather than reasoned about, so that a pair nobody intended shows up
    /// as a failure. This used to be checked against every shipped device profile — there are none
    /// now, and the table is the only place the answer was ever really kept.
    /// </para>
    /// </remarks>
    [Fact]
    public void OnlyKeysFromMutuallyExclusiveLayoutsShareACell()
    {
        string[][] expected =
        [
            ["Keyboard_Function", "Keyboard_RightGui"],
            ["Keyboard_JpYen", "Keyboard_Kor1"],
            ["Keyboard_Kor2", "Keyboard_NonUsTilde"],
            ["Keyboard_Kor3", "Keyboard_NonUsBackslash"],
            ["Keyboard_JpRo", "Keyboard_Kor4"],
            ["Keyboard_JpMuhenkan", "Keyboard_Kor5"],
            ["Keyboard_JpHenkan", "Keyboard_Kor6"],
            ["Keyboard_JpKana", "Keyboard_Kor7"],
        ];

        var sharing = StandardKeyMatrix.All
            .GroupBy(c => c.Value)
            .Where(g => g.Count() > 1)
            .Select(g => g.Select(k => k.Key).Order(StringComparer.Ordinal).ToArray())
            .OrderBy(g => string.Join("+", g), StringComparer.Ordinal)
            .ToArray();

        var wanted = expected
            .Select(g => g.Order(StringComparer.Ordinal).ToArray())
            .OrderBy(g => string.Join("+", g), StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            wanted.Select(g => string.Join(" + ", g)),
            sharing.Select(g => string.Join(" + ", g)));
    }

    /// <summary>
    /// The one profile calibrated at the device, key by key. This is the anchor for the whole
    /// table: it was measured over the same path the program actually lights by.
    /// </summary>
    [Fact]
    public void AgreesWithEveryKeyMeasuredAtTheDevice()
    {
        var checkedKeys = 0;
        var disagreements = new List<string>();

        foreach (var key in MeasuredKeys.Load())
        {
            if (key.Row is not { } row || key.Column is not { } column)
            {
                continue;
            }

            if (StandardKeyMatrix.Cell(key.Id) is not { } cell)
            {
                continue;
            }

            checkedKeys++;

            if (cell != (row, column))
            {
                disagreements.Add($"{key.Id}: measured ({row}, {column}), table {cell}");
            }
        }

        // A whole keyboard's worth of cells that all agree is not something an edited or generated
        // file arrives at by accident, which is what makes the count worth asserting on its own.
        Assert.True(checkedKeys > 100, $"Expected the full keyboard, compared only {checkedKeys}.");
        Assert.Empty(disagreements);
    }
}
