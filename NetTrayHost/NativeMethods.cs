using System.Runtime.InteropServices;

namespace NetTrayHost
{
    internal static class NativeMethods
    {
        internal const int SW_HIDE = 0;
        internal const int SW_SHOW = 5;

        internal const int GWL_EXSTYLE = -20;
        internal const int WS_EX_APPWINDOW = 0x00040000;
        internal const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetConsoleWindow();
    }
}
