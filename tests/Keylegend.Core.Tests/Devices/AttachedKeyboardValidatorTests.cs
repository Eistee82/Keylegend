using Keylegend.Core.Devices;

namespace Keylegend.Core.Tests.Devices;

public class AttachedKeyboardValidatorTests
{
    private static KeyDefinition Key(string id, int? row = 0, int? column = 0, double x = 0, double y = 0)
        => new(id, x, y, 19, 19, row, column);

    private static AttachedKeyboard KeyboardWith(params KeyDefinition[] keys)
        => new(
            Name: "Test keyboard",
            PhysicalLayout: "ISO-DE",
            Canvas: new Canvas(500, 200),
            Matrix: new MatrixSize(6, 22),
            Keys: keys);

    [Fact]
    public void AcceptsAWellFormedKeyboard()
    {
        var profile = KeyboardWith(Key("Keyboard_A", 3, 2), Key("Keyboard_B", 4, 7, x: 40));

        Assert.Empty(AttachedKeyboardValidator.Validate(profile));
    }

    /// <summary>
    /// The mistake a hand-written profile makes: one wrong width, and every key after it on the
    /// row slides under its neighbour.
    /// </summary>
    [Fact]
    public void RejectsOverlappingKeys()
    {
        var profile = KeyboardWith(Key("Keyboard_A", 3, 2), Key("Keyboard_B", 4, 7, x: 10));

        Assert.Contains(
            AttachedKeyboardValidator.Validate(profile),
            p => p.Contains("Keyboard_A") && p.Contains("Keyboard_B") && p.Contains("overlap"));
    }

    /// <summary>
    /// Keys that merely touch along an edge do not overlap — that is what every key on a
    /// keyboard does to its neighbour.
    /// </summary>
    [Fact]
    public void AcceptsKeysThatTouchAlongAnEdge()
    {
        var profile = KeyboardWith(Key("Keyboard_A", 3, 2), Key("Keyboard_B", 4, 7, x: 19));

        Assert.Empty(AttachedKeyboardValidator.Validate(profile));
    }

    /// <summary>
    /// The ISO Enter overlaps nothing, but its second rectangle sits in the row above and must
    /// be checked against the keys there.
    /// </summary>
    [Fact]
    public void RejectsAKeyOverlappingAnotherKeysExtraRectangle()
    {
        var enter = new KeyDefinition(
            "Keyboard_Enter", X: 100, Y: 19, Width: 23.75, Height: 19, Row: 3, Column: 14,
            Parts: [new KeyArea(95, 0, 28.5, 19)]);
        var intruder = Key("Keyboard_BracketRight", 2, 13, x: 100, y: 0);

        var problems = AttachedKeyboardValidator.Validate(KeyboardWith(enter, intruder));

        Assert.Contains(problems, p => p.Contains("overlap"));
    }

    [Fact]
    public void RejectsDuplicateKeyIds()
    {
        var profile = KeyboardWith(Key("Keyboard_A", 3, 2), Key("Keyboard_A", 4, 7, x: 40));

        Assert.Contains(
            AttachedKeyboardValidator.Validate(profile),
            p => p.Contains("Keyboard_A") && p.Contains("unique"));
    }

    [Fact]
    public void RejectsTwoKeysDrivingTheSameLed()
    {
        var profile = KeyboardWith(Key("Keyboard_A", 3, 2), Key("Keyboard_B", 3, 2, x: 40));

        Assert.Contains(
            AttachedKeyboardValidator.Validate(profile),
            p => p.Contains("(3,2)") && p.Contains("more than one key"));
    }

    [Fact]
    public void RejectsCellsOutsideTheMatrix()
    {
        var profile = KeyboardWith(Key("Keyboard_A", 9, 2), Key("Keyboard_B", 3, 99, x: 40));

        var problems = AttachedKeyboardValidator.Validate(profile);

        Assert.Contains(problems, p => p.Contains("row 9"));
        Assert.Contains(problems, p => p.Contains("column 99"));
    }

    [Fact]
    public void RejectsHalfSpecifiedCoordinates()
    {
        var profile = KeyboardWith(Key("Keyboard_A", row: 3, column: null));

        Assert.Contains(
            AttachedKeyboardValidator.Validate(profile),
            p => p.Contains("only one of row/column"));
    }

    [Fact]
    public void AllowsAKeyTheMatrixTableDoesNotKnow()
    {
        // A drawing can show a key the protocol's table has no cell for — a vendor key, say. That
        // is not a broken profile: the frame composer skips such a key, and the interface draws it
        // unlit. Rejecting it here would refuse the whole keyboard over one key that cannot light.
        var profile = KeyboardWith(Key("Keyboard_A", row: null, column: null));

        Assert.Empty(AttachedKeyboardValidator.Validate(profile));
    }

    [Fact]
    public void RejectsKeysOffTheCanvas()
    {
        var profile = KeyboardWith(Key("Keyboard_A", 3, 2, x: 495));

        Assert.Contains(
            AttachedKeyboardValidator.Validate(profile),
            p => p.Contains("outside the canvas"));
    }

}
