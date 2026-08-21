using Keylegend.Core.Input;

namespace Keylegend.Core.Tests.Input;

public class KeyRolesTests
{
    [Theory]
    [InlineData("Keyboard_F1")]
    [InlineData("Keyboard_F9")]
    [InlineData("Keyboard_F12")]
    public void RecognisesFunctionKeys(string keyId)
    {
        Assert.True(KeyRoles.IsFunctionKey(keyId));
        Assert.Equal(KeyCategory.FunctionKey, KeyRoles.StructuralCategory(keyId));
    }

    [Theory]
    [InlineData("Keyboard_F")]        // no number
    [InlineData("Keyboard_F0")]       // out of range
    [InlineData("Keyboard_F13")]      // beyond a standard board
    [InlineData("Keyboard_Function")] // the fn key is not a function key
    public void RejectsLookalikes(string keyId)
    {
        Assert.False(KeyRoles.IsFunctionKey(keyId));
        Assert.Null(KeyRoles.StructuralCategory(keyId));
    }

    [Theory]
    [InlineData("Keyboard_A")]
    [InlineData("Keyboard_Escape")]
    [InlineData("Keyboard_Num5")]
    public void OrdinaryKeysHaveNoStructuralCategory(string keyId)
        => Assert.Null(KeyRoles.StructuralCategory(keyId));
}
