using UnityEngine;

public partial class RingDrive
{
    bool mouseSteering, discardMouseMotion;
    float mouseSteer;

    void ResetMouseSteering()
    {
        mouseSteer = 0;
        discardMouseMotion = true;
    }

    void ToggleMouseSteering()
    {
        mouseSteering = !mouseSteering;
        ResetMouseSteering();
        lookYaw = lookPitch = 0;
        Debug.Log("STEERING_INPUT " + (mouseSteering ? "MOUSE" : "KEYBOARD"));
    }

    void OnApplicationFocus(bool focused) => ResetMouseSteering();

    void UpdatePointer(Vector2 motion, bool centre)
    {
        if (paused || modelPreview || signTest || surfaceTest) return;
        // Ignore the cursor-capture frame after a toggle, pause or focus change.
        if (discardMouseMotion) { discardMouseMotion = false; return; }
        if (mouseSteering)
        {
            if (!autopilotEnabled && !automatic)
                mouseSteer = centre ? 0 : Mathf.Clamp(mouseSteer + motion.x * .04f, -1, 1);
            return;
        }
        float nextYaw = lookYaw + motion.x * 2;
        lookYaw = view == 2 ? Mathf.Repeat(nextYaw + 180, 360) - 180 : Mathf.Clamp(nextYaw, -115, 115);
        lookPitch = Mathf.Clamp(lookPitch - motion.y * 1.5f, view == 2 ? -13 : -45, view == 2 ? 57 : 40);
        if (centre) lookYaw = lookPitch = 0;
    }

    void DrawSteeringInput()
    {
        float y = autopilotEnabled ? 369 : 215;
        GUI.color = new Color(.035f, .055f, .07f, .9f);
        GUI.DrawTexture(new Rect(25, y, 370, mouseSteering ? 108 : 76), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.enabled = !settingsOpen;
        if (GUI.Button(new Rect(45, y + 10, 330, 30), $"Steering: {(mouseSteering ? "MOUSE" : "KEYBOARD")} [U]"))
            ToggleMouseSteering();
        GUI.enabled = true;
        GUI.Label(new Rect(45, y + 45, 330, 24), mouseSteering
            ? autopilotEnabled ? $"Mouse ready — {PilotShortcut} for manual driving" : "Mouse left/right • Right-click to centre"
            : "A/D or arrows steer • Mouse looks around", small);
        if (!mouseSteering) return;
        float demand = autopilotEnabled || paused ? 0 : mouseSteer;
        GUI.color = new Color(.25f, .32f, .37f);
        GUI.DrawTexture(new Rect(65, y + 82, 290, 4), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(209, y + 76, 2, 16), Texture2D.whiteTexture);
        GUI.color = new Color(.35f, .8f, 1);
        GUI.DrawTexture(new Rect(206 + demand * 145, y + 78, 8, 12), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
