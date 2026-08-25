namespace Keylegend.Core.Devices;

/// <summary>Raised when the attached keyboard cannot be described.</summary>
/// <remarks>
/// Carried to the top of the program, where it becomes the message the user sees and the reason
/// it stops: there is nothing to light without a profile, and nothing to fall back on either.
/// </remarks>
public sealed class DeviceProfileException : Exception
{
    public DeviceProfileException(string message) : base(message) { }

    public DeviceProfileException(string message, Exception inner) : base(message, inner) { }
}
