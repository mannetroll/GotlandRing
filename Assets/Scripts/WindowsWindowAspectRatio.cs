#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// Constrain the client area, excluding the title bar and DPI-scaled window borders.
public sealed class WindowsWindowAspectRatio : MonoBehaviour
{
    const double Aspect = 16.0 / 9.0;
    const int WindowProcedure = -4, WindowStyle = -16;
    const uint Sizing = 0x214, ExitSizeMove = 0x232, WindowPositionChanging = 0x46;
    public static volatile bool ChangingDisplayMode;
    IntPtr window, previousProcedure;
    Procedure procedure; // Keep the managed callback alive while Windows holds its pointer.
    volatile bool reportSize;

    [StructLayout(LayoutKind.Sequential)]
    struct Rect { public int left, top, right, bottom; public int Width => right-left; public int Height => bottom-top; }
    [StructLayout(LayoutKind.Sequential)]
    struct WindowPosition { public IntPtr window, insertAfter; public int x, y, width, height; public uint flags; }
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate IntPtr Procedure(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    delegate bool EnumWindow(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindow callback, IntPtr parameter);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("kernel32.dll")] static extern uint GetCurrentProcessId();
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr window, StringBuilder name, int count);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("user32.dll", EntryPoint="SetWindowLongPtrW", SetLastError=true)] static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);
    [DllImport("user32.dll", EntryPoint="CallWindowProcW")] static extern IntPtr CallWindowProc(IntPtr procedure, IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize() => new GameObject("Windows window aspect ratio").AddComponent<WindowsWindowAspectRatio>();

    IEnumerator Start()
    {
        uint process = GetCurrentProcessId();
        var className = new StringBuilder(128);
        float deadline = Time.realtimeSinceStartup + 10;
        while (window == IntPtr.Zero && Time.realtimeSinceStartup < deadline)
        {
            EnumWindows((candidate, _) => {
                GetWindowThreadProcessId(candidate, out uint owner);
                if (owner != process) return true;
                GetClassName(candidate, className, className.Capacity);
                if (className.ToString() != "UnityWndClass") return true;
                window = candidate;
                return false;
            }, IntPtr.Zero);
            if (window == IntPtr.Zero) yield return null;
        }
        if (window == IntPtr.Zero) { Debug.LogWarning("WINDOW_ASPECT: Unity window not found"); yield break; }
        previousProcedure = GetWindowLongPtr(window, WindowProcedure);
        procedure = HandleMessage;
        if (SetWindowLongPtr(window, WindowProcedure, Marshal.GetFunctionPointerForDelegate(procedure)) == IntPtr.Zero)
        {
            Debug.LogWarning("WINDOW_ASPECT: could not install resize constraint, error=" + Marshal.GetLastWin32Error());
            yield break;
        }
        if (!Screen.fullScreen)
            Screen.SetResolution(Screen.width, Mathf.RoundToInt(Screen.width * 9f / 16), FullScreenMode.Windowed);
        Debug.Log("WINDOW_ASPECT installed: 16:9 client area");
    }

    IntPtr HandleMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam)
    {
        // Unity can call the window procedure on its window thread. Use only native
        // geometry here; no Unity APIs, scene state or Screen.SetResolution calls.
        bool windowed = (GetWindowLongPtr(handle, WindowStyle).ToInt64() & 0x00C00000) != 0;
        if (!ChangingDisplayMode && windowed && (message == Sizing || message == WindowPositionChanging)
            && GetWindowRect(handle, out Rect outer) && GetClientRect(handle, out Rect client))
        {
            int borderWidth = outer.Width-client.Width, borderHeight = outer.Height-client.Height;
            if (message == Sizing)
            {
                var rect = Marshal.PtrToStructure<Rect>(lParam);
                int edge = wParam.ToInt32();
                int width = Math.Max(1, rect.Width-borderWidth), height = Math.Max(1, rect.Height-borderHeight);
                bool heightDriven = edge == 3 || edge == 6 || (edge != 1 && edge != 2
                    && Math.Abs(height-client.Height)*Aspect > Math.Abs(width-client.Width));
                if (heightDriven) width = (int)Math.Round(height*Aspect);
                else height = (int)Math.Round(width/Aspect);
                width += borderWidth; height += borderHeight;
                // Corners retain the opposite corner. Side drags expand the other
                // dimension around its centre so the edge stays under the pointer.
                if (edge == 3 || edge == 6) { rect.left += (rect.Width-width)/2; rect.right = rect.left+width; }
                else if (edge == 1 || edge == 4 || edge == 7) rect.left = rect.right-width;
                else rect.right = rect.left+width;
                if (edge == 1 || edge == 2) { rect.top += (rect.Height-height)/2; rect.bottom = rect.top+height; }
                else if (edge == 3 || edge == 4 || edge == 5) rect.top = rect.bottom-height;
                else rect.bottom = rect.top+height;
                Marshal.StructureToPtr(rect, lParam, false);
                return new IntPtr(1);
            }
            var position = Marshal.PtrToStructure<WindowPosition>(lParam);
            if ((position.flags & 1) == 0 && position.width > borderWidth && position.height > borderHeight)
            {
                int width = position.width-borderWidth, height = position.height-borderHeight;
                // Snap/maximize/programmatic resizing can bypass WM_SIZING. Fit
                // inside the proposed area rather than extending beyond the desktop.
                if (Math.Abs(width*9-height*16) > 8)
                {
                    if (width > height*Aspect) width = (int)Math.Round(height*Aspect);
                    else height = (int)Math.Round(width/Aspect);
                    position.width = width+borderWidth; position.height = height+borderHeight;
                    Marshal.StructureToPtr(position, lParam, false);
                }
            }
        }
        if (message == ExitSizeMove) reportSize = true;
        return CallWindowProc(previousProcedure, handle, message, wParam, lParam);
    }

    void Update()
    {
        if (!reportSize) return;
        reportSize = false;
        if (GetClientRect(window, out Rect rect)) Debug.Log($"WINDOW_ASPECT resized: {rect.Width}x{rect.Height} error={Math.Abs(rect.Width*9-rect.Height*16)} (rounding tolerance 8)");
    }

    void OnDestroy()
    {
        if (window != IntPtr.Zero && previousProcedure != IntPtr.Zero && procedure != null
            && GetWindowLongPtr(window, WindowProcedure) == Marshal.GetFunctionPointerForDelegate(procedure))
            SetWindowLongPtr(window, WindowProcedure, previousProcedure);
    }
}
#endif
