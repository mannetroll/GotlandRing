using UnityEngine;

public partial class RingDrive
{
    ReferenceLapPlayer referenceLap;
    System.Collections.Generic.IReadOnlyList<Vector3> referenceLine;
    bool referenceDriveWasPaused;

    void ToggleReferenceLap()
    {
        if (referenceLap)
        {
            referenceLap.gameObject.SetActive(false); Destroy(referenceLap.gameObject); referenceLap = null;
            car.gameObject.SetActive(true); ResetFeedback(); SetPaused(referenceDriveWasPaused);
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = paused;
            ResetMouseSteering();
            Debug.Log("REFERENCE_LAP stopped; driving restored");
            return;
        }
        referenceDriveWasPaused = paused; SetPaused(true);
        car.gameObject.SetActive(false);
        referenceLap = new GameObject("Reference lap replay").AddComponent<ReferenceLapPlayer>();
        referenceLap.Initialize(cam, centerline, referenceLine, dynamics);
        referenceLap.Tick(0, muted);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        Debug.Log("REFERENCE_LAP started; game-rendered reference timing, no recording");
    }
    void UpdateReferenceLap()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)) referenceLap.Paused = !referenceLap.Paused;
        if (Input.GetKeyDown(KeyCode.Home)) { referenceLap.Seek(0); referenceLap.Paused = false; }
        if (Input.GetKeyDown(KeyCode.M)) muted = !muted;
        if (Input.GetKeyDown(KeyCode.F) && ShortcutModifierHeld) ToggleFullscreen();
        if (Input.GetKeyDown(KeyCode.F2)) CaptureScreenshot("GotlandRing-reference-lap.png");
        float advance = referenceLap.Tick(Time.unscaledDeltaTime, muted);
        if (advance > 0) AnimateWindTurbines(advance);
    }
}
