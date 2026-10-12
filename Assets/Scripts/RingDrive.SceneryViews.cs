using System.Collections.Generic;
using UnityEngine;

public partial class RingDrive
{
    static readonly (int Point, float Yaw, string File, string Name)[] SceneryTrackViews = {
        (235, -30, "pit-catch-fence", "Pit lane / catch fencing"),
        (444, -15, "armco-barriers", "Nankang Hairpin / Armco"),
        (1835, 0, "concrete-barriers", "The Klaus Bowl / concrete barriers"),
        (TrackLandmarks.StartFinishPoint - 6, 0, "gutemalrakan-finish", "Gutemålrakan / start and finish"),
        (1240, 0, "havsornen-approach", "Havsörnen approach"),
        (1280, 0, "havsornen-quarry", "Havsörnen / limestone quarry"),
        (2264, 0, "arho-kerb", "Arho / blue and white kerbs"),
        (932, 0, "altarkarusellen-kerb", "Altarkarusellen"),
        (1883, 0, "kramertsskog-kerb", "F.S. Krämertsskog")
    };
    readonly List<(string Name, Vector3 Position, Quaternion Rotation, float Size)> sceneryViews = new();
    int sceneryView = -1;
    bool sceneryDriveWasPaused, sceneryCameraWasOrthographic;
    float sceneryCameraFov, sceneryCameraSize;
    Vector3 sceneryHeadPosition;
    Quaternion sceneryHeadRotation;
    bool SceneryViewing => sceneryView >= 0;

    void MakeSceneryViews()
    {
        var building = mainBuilding.position;
        AddSceneryView("Main building", building + new Vector3(53, 31, -75), building + Vector3.up * 3);
        AddSceneryView("Main building / turbine 2", building + new Vector3(-20, 240, -35), Vector3.zero, 140);
        foreach (var shot in SceneryTrackViews)
        {
            var target = track[shot.Point] + Vector3.up * 3;
            var forward = (track[(shot.Point + 8) % track.Count] - track[shot.Point]).normalized;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            AddSceneryView(shot.Name, target - forward * 100 + right * 70 + Vector3.up * 75, target);
        }
        foreach (int i in new[] {0, 6, 9})
        {
            var turbine = windTurbines[i]; var hub = turbineRotors[i].position;
            AddSceneryView($"Wind turbines / {turbine.Model}",
                hub + turbineSites[i].TransformDirection(new Vector3(45, -25, -170)) * (turbine.HubHeight / 78),
                hub - Vector3.up * (turbine.HubHeight * .2f));
        }
        AddSceneryView("Gotland Ring / all 12 wind turbines", new Vector3(50, 1500, -125), Vector3.zero, 980);
    }

    void AddSceneryView(string name, Vector3 position, Vector3 target, float size = 0) =>
        sceneryViews.Add((name, position, size > 0 ? Quaternion.Euler(90, 0, 0) : Quaternion.LookRotation(target - position), size));

    void NextSceneryView()
    {
        if (!SceneryViewing)
        {
            if (sceneryViews.Count == 0) MakeSceneryViews();
            sceneryDriveWasPaused = paused;
            sceneryCameraWasOrthographic = cam.orthographic;
            sceneryCameraFov = cam.fieldOfView; sceneryCameraSize = cam.orthographicSize;
            sceneryHeadPosition = head.localPosition; sceneryHeadRotation = head.localRotation;
            SetPaused(true); motor.Muted = true;
        }
        if (++sceneryView == sceneryViews.Count) { ExitSceneryViews(); return; }
        var shot = sceneryViews[sceneryView];
        head.SetPositionAndRotation(shot.Position, shot.Rotation);
        cam.orthographic = shot.Size > 0;
        if (cam.orthographic) cam.orthographicSize = shot.Size;
        cam.fieldOfView = 60;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    void ExitSceneryViews()
    {
        sceneryView = -1;
        cam.orthographic = sceneryCameraWasOrthographic;
        cam.orthographicSize = sceneryCameraSize; cam.fieldOfView = sceneryCameraFov;
        head.SetLocalPositionAndRotation(sceneryHeadPosition, sceneryHeadRotation);
        SetPaused(sceneryDriveWasPaused); motor.Muted = muted || paused;
    }

    void UpdateSceneryViews()
    {
        AnimateWindTurbines(Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Escape)) { ExitSceneryViews(); return; }
        if (Input.GetKeyDown(KeyCode.O)) { ExitSceneryViews(); ToggleReferenceLap(); return; }
        if (Input.GetKeyDown(KeyCode.F) && ShortcutModifierHeld) ToggleFullscreen();
        if (Input.GetKeyDown(KeyCode.F2)) CaptureScreenshot($"GotlandRing-scenery-{sceneryView + 1:00}.png");
    }

    void DrawSceneryViews()
    {
        GUI.color = new Color(.035f, .055f, .07f, .9f);
        GUI.DrawTexture(new Rect(25, 25, 700, 122), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(25, 819, 700, 56), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(45, 35, 660, 32), "GOTLAND RING / SCENERY", label);
        GUI.Label(new Rect(45, 74, 660, 32), $"{sceneryView + 1} / {sceneryViews.Count}   {sceneryViews[sceneryView].Name}", label);
        GUI.Label(new Rect(45, 114, 660, 24), "V: next view   C / Escape: return to car   F2: screenshot", small);
        if (GUI.Button(new Rect(25, 160, 205, 36), sceneryView + 1 == sceneryViews.Count ? "Return to car [V]" : "Next scenery [V]")) NextSceneryView();
        if (GUI.Button(new Rect(245, 160, 205, 36), "Return to car [C]")) ExitSceneryViews();
        GUI.Label(new Rect(45, 828, 660, 24), $"Drive paused while viewing / Mannetroll Solutions AB / {Application.version}", small);
        GUI.Label(new Rect(45, 851, 660, 24), carModel.credit, small);
    }
}
