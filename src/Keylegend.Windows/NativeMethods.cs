using System.Runtime.InteropServices;
using System.Text;

namespace Keylegend.Windows;

/// <summary>
/// The Win32 entry points this project uses. Everything here is read-only: key states are
/// queried, never intercepted. There is deliberately no hook installation anywhere in this
/// assembly.
/// </summary>
/// <remarks>
/// Classic <see cref="DllImportAttribute"/> rather than the source-generated variant, because
/// <see cref="ToUnicodeEx"/> needs a <see cref="StringBuilder"/> buffer, which the generator
/// does not marshal — and switching to raw pointers for it would require unsafe code for no
/// benefit at eight call sites.
/// </remarks>
internal static class NativeMethods
{
    // Virtual key codes, left and right kept apart - see KeyboardState for why that matters.
    public const int VK_LSHIFT = 0xA0;
    public const int VK_RSHIFT = 0xA1;
    public const int VK_LCONTROL = 0xA2;
    public const int VK_RCONTROL = 0xA3;
    public const int VK_LMENU = 0xA4;
    public const int VK_RMENU = 0xA5;
    public const int VK_LWIN = 0x5B;
    public const int VK_RWIN = 0x5C;

    public const int VK_SHIFT = 0x10;
    public const int VK_CONTROL = 0x11;
    public const int VK_MENU = 0x12;

    public const int VK_CAPITAL = 0x14;
    public const int VK_NUMLOCK = 0x90;
    public const int VK_SCROLL = 0x91;

    /// <summary>Maps a scan code to a virtual key, honouring left/right variants.</summary>
    public const uint MAPVK_VSC_TO_VK_EX = 3;

    /// <summary>
    /// Tells <see cref="ToUnicodeEx"/> to leave the keyboard state alone. Without it, asking
    /// about a dead key leaves that dead key pending in the driver and corrupts the user's
    /// next real keystroke. Available since Windows 10 version 1607.
    /// </summary>
    public const uint TOUNICODE_DO_NOT_CHANGE_KEYBOARD_STATE = 0x4;

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int ToUnicodeEx(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags,
        IntPtr dwhkl);

    // Raw Input, used only to ask which keyboards are attached — never to receive input from
    // them. ConnectedKeyboards reads the device names and nothing else, which is what keeps the
    // "no keyboard hooks, ever" promise intact: listing devices is not listening to them.
    //
    // The Chroma SDK cannot answer this. Its REST interface is organised by device class, and a
    // session hands back an id and a URI, nothing about the hardware behind them.
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetRawInputDeviceList(
        IntPtr pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint GetRawInputDeviceInfoW(
        IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint GetRawInputDeviceInfoW(
        IntPtr hDevice, uint uiCommand, StringBuilder pData, ref uint pcbSize);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    // --- Foreground application and full-screen detection ---

    public const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    public static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    /// <summary>
    /// Windows' own notion of "the user is busy". Reports when a Direct3D application owns the
    /// screen, which is how the shell knows to hold back notifications during a game.
    /// </summary>
    [DllImport("shell32.dll")]
    public static extern int SHQueryUserNotificationState(out int pquns);

    /// <summary>True while the key is physically held down.</summary>
    public static bool IsDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    /// <summary>True while the toggle is on (Caps Lock and friends).</summary>
    public static bool IsToggled(int virtualKey) => (GetKeyState(virtualKey) & 0x0001) != 0;
}
