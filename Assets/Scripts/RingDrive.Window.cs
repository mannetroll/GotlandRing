using System.Collections;
using UnityEngine;

public partial class RingDrive
{
    Vector2Int windowedSize = new Vector2Int(1600, 900);
    bool changingDisplay;

    bool IsMac => Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor;
    string ShortcutModifier => IsMac ? "Cmd" : "Ctrl";
    bool ShortcutModifierHeld => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
        || (IsMac && (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)));

    void ToggleFullscreen()
    {
        if (!changingDisplay) StartCoroutine(ChangeDisplayMode());
    }

    IEnumerator ChangeDisplayMode()
    {
        changingDisplay = true;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        WindowsWindowAspectRatio.ChangingDisplayMode = true;
#endif
        bool fullscreen = !Screen.fullScreen;
        if (fullscreen)
        {
            windowedSize = new Vector2Int(Screen.width, Screen.height);
            var desktop = Screen.currentResolution;
            Screen.SetResolution(desktop.width, desktop.height, FullScreenMode.FullScreenWindow);
        }
        else
            Screen.SetResolution(windowedSize.x, windowedSize.y, FullScreenMode.Windowed);

        // Unity applies the change after this frame; macOS may also animate it.
        // Ignore repeated toggles during the transition so the window size stays intact.
        float deadline = Time.realtimeSinceStartup + 3;
        yield return null;
        while (Screen.fullScreen != fullscreen && Time.realtimeSinceStartup < deadline)
            yield return null;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
        changingDisplay = false;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        WindowsWindowAspectRatio.ChangingDisplayMode = false;
#endif
        Debug.Log($"DISPLAY_MODE requested={(fullscreen?"fullscreen":"windowed")} actual={Screen.fullScreenMode} size={Screen.width}x{Screen.height}");
    }
}
