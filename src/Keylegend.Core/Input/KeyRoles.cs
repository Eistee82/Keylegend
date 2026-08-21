namespace Keylegend.Core.Input;

/// <summary>
/// Categories that follow from which key it is rather than from what it produces.
/// </summary>
/// <remarks>
/// Almost everything is classified by the character a key produces, which is what keeps the
/// program layout-independent. Function keys are the exception: they produce nothing at all,
/// so there is nothing to classify. They are recognised by identity instead.
/// </remarks>
public static class KeyRoles
{
    /// <summary>Whether the key is F1 to F12.</summary>
    public static bool IsFunctionKey(string keyId)
    {
        ArgumentNullException.ThrowIfNull(keyId);

        const string prefix = "Keyboard_F";

        if (!keyId.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var suffix = keyId.AsSpan(prefix.Length);

        return int.TryParse(suffix, out var number) && number is >= 1 and <= 12;
    }

    /// <summary>
    /// The category a key has purely by virtue of being that key, or <c>null</c> if it has
    /// none and the produced character decides.
    /// </summary>
    public static KeyCategory? StructuralCategory(string keyId)
        => IsFunctionKey(keyId) ? KeyCategory.FunctionKey : null;
}
