using UnityEngine;

public partial class RingDrive
{
    GUIStyle modeTitle, modeBody, modeEyebrow, modeButton, modeSecondary, modeFooter;

    void ToggleDrivingMenu()
    {
        if (droneSettingsOpen) { CloseDroneSettings(false); return; }
        if (settingsOpen) { CloseSettings(false); return; }
        bool viewing = referenceLap || SceneryViewing;
        if (referenceLap) ToggleReferenceLap();
        if (SceneryViewing) ExitSceneryViews();
        SetPaused(viewing || !paused);
    }

    void DrawDrivingMenu()
    {
        var matrix = GUI.matrix;
        var colour = GUI.color;
        float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1600 * scale) / 2,
            (Screen.height - 900 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
        if (modeTitle == null)
        {
            modeTitle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
            modeBody = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            modeEyebrow = new GUIStyle(modeBody) { fontSize = 14, fontStyle = FontStyle.Bold };
            modeButton = new GUIStyle(GUI.skin.button) { fontSize = 17 };
            modeSecondary = new GUIStyle(modeBody);modeSecondary.normal.textColor=GameUi.Muted;
            modeFooter = new GUIStyle(modeSecondary) { fontSize = 14 };
            modeEyebrow.normal.textColor=GameUi.Accent;
        }
        if(droneSettingsOpen){DrawDroneSettings();GUI.matrix=matrix;GUI.color=colour;return;}
        if(keyboardHelpOpen){DrawKeyboardHelp();GUI.matrix=matrix;GUI.color=colour;return;}
        float utilityY = 284;
        float footerY = utilityY + 124;
        float height = footerY + 70;
        var dialog = new Rect(400, (900 - height) / 2, 800, height);
        GameUi.Dialog(dialog);
        GUI.BeginGroup(dialog);
        GUI.color = Color.white;
        GUI.Label(new Rect(40, 30, 720, 28), "GOTLAND RING", modeEyebrow);
        GUI.color = Color.white;
        GUI.Label(new Rect(40, 64, 720, 50), "Practice paused", modeTitle);
        GUI.Label(new Rect(40, 118, 720, 72), "Circuit driving, timed laps and a training area for slide practice.", modeSecondary);
        {
            if (GameUi.PrimaryButton(new Rect(40, 218, 720, 44), "Resume driving [Esc]", modeButton)) SetPaused(false);
            if (GUI.Button(new Rect(40, utilityY, 350, 44), $"Driving dynamics [{KeyHint(KeyCode.F3)}]", modeButton)) OpenSettings();
            if (GUI.Button(new Rect(410, utilityY, 350, 44), "Drone camera", modeButton)) OpenDroneSettings();
            if (GUI.Button(new Rect(40, utilityY + 56, 350, 44), "Keyboard help & setup", modeButton)) OpenKeyboardHelp();
            if (GUI.Button(new Rect(410, utilityY + 56, 350, 44), "Quit", modeButton)) Application.Quit();
        }
        GUI.Label(new Rect(40, footerY, 720, 22), carModel.credit, modeFooter);
        GUI.Label(new Rect(40, footerY + 24, 720, 22), $"Version {Application.version} · Copyright © Mannetroll Solutions AB", modeFooter);
        GUI.EndGroup();
        GUI.matrix = matrix;
        GUI.color = colour;
    }

}
