using System;
using System.Collections.Generic;
using UnityEngine;

// Live rendering of the reference alignment. The samples describe a camera replay,
// with estimated audio controls, rather than a physics or telemetry recording.
public sealed class ReferenceLapPlayer : MonoBehaviour
{
    [Serializable] public struct Frame { public float distance, speed, rpm, load; public int gear; }
    [Serializable] public sealed class Timeline
    {
        public int fps;
        public float lapStart, lapDuration;
        public Frame[] frames;
        public float Duration => frames.Length / (float)fps;
        public Frame Sample(float seconds)
        {
            float index = Mathf.Clamp(seconds * fps, 0, frames.Length - 1);
            int first = Mathf.FloorToInt(index), next = Mathf.Min(first + 1, frames.Length - 1);
            var a = frames[first]; var b = frames[next]; float blend = index - first;
            return new Frame { distance = Mathf.Lerp(a.distance, b.distance, blend),
                speed = Mathf.Lerp(a.speed, b.speed, blend), rpm = Mathf.Lerp(a.rpm, b.rpm, blend),
                load = Mathf.Lerp(a.load, b.load, blend), gear = a.gear };
        }
    }
    public Timeline Data { get; private set; }
    public float Elapsed { get; private set; }
    public bool Paused { get; set; }
    public bool Finished => Elapsed >= Data.Duration;
    public float LapTime => Mathf.Clamp(Elapsed - Data.lapStart, 0, Data.lapDuration);
    public BoxerAudio Motor { get; private set; }
    public Camera Bonnet { get; private set; }
    public Camera Cockpit { get; private set; }
    public ImprezaModel Model { get; private set; }
    readonly RideFeedback feedback=new RideFeedback();
    DrivingSettings settings;
    Vector3 bonnetPosition;
    TrackData track;
    IReadOnlyList<Vector3> line;
    GUIStyle title, caption, clock, credit, rightTitle, rightCaption;
    Rect canvas;
    float scale;

    public void Initialize(Camera source, TrackData surface, IReadOnlyList<Vector3> racingLine, DrivingSettings drivingSettings)
    {
        settings=drivingSettings;
        Data = JsonUtility.FromJson<Timeline>(Resources.Load<TextAsset>("Replay/ReferenceLap").text);
        track = surface; line = racingLine;
        Model = Instantiate(Resources.Load<GameObject>("RallyCar"), transform, false).GetComponent<ImprezaModel>();
        var background = new GameObject("Replay background").AddComponent<Camera>();
        background.transform.SetParent(transform, false); background.cullingMask = 0;
        background.clearFlags = CameraClearFlags.SolidColor; background.backgroundColor = new Color(.035f, .055f, .07f);
        background.depth = -10;
        bonnetPosition=Model.bonnetView.localPosition;
        Bonnet = View("Replay bonnet", source, Model.bonnetView.localPosition, Quaternion.Euler(-7, 0, 0), 48);
        Cockpit = View("Replay cockpit", source, Model.cockpitView.localPosition, Model.cockpitView.localRotation, 58);
        Bonnet.gameObject.AddComponent<AudioListener>();
        Motor = gameObject.AddComponent<BoxerAudio>();
        Seek(0); UpdateLayout();
    }
    Camera View(string name, Camera source, Vector3 position, Quaternion rotation, float fov)
    {
        var camera = new GameObject(name).AddComponent<Camera>(); camera.CopyFrom(source);
        camera.transform.SetParent(transform, false); camera.transform.localPosition = position; camera.transform.localRotation = rotation;
        camera.targetTexture = null; camera.fieldOfView = fov; camera.depth = -5; camera.enabled = true;
        return camera;
    }
    public Vector3 Point(float distance)
    {
        distance = Mathf.Repeat(distance, track.HorizontalLength);
        int low = 0, high = track.Sections.Count - 1;
        while (low < high) { int middle = (low + high + 1) / 2; if (track.Sections[middle].Distance <= distance) low = middle; else high = middle - 1; }
        int next = (low + 1) % line.Count;
        float end = next == 0 ? track.HorizontalLength : track.Sections[next].Distance;
        return Vector3.Lerp(line[low], line[next], (distance - track.Sections[low].Distance) / (end - track.Sections[low].Distance));
    }
    public void Seek(float seconds) { feedback.Reset(); Motor.Asphalt=Motor.Kerb=Motor.LooseGround=0; Elapsed = Mathf.Clamp(seconds, 0, Data.Duration); Pose(0); }
    public float Tick(float dt, bool muted)
    {
        float advance = Paused ? 0 : Mathf.Min(dt, Data.Duration - Elapsed);
        Elapsed += advance; Pose(advance); UpdateLayout();
        Motor.Muted = muted || Paused || Finished;
        return advance;
    }
    void Pose(float dt)
    {
        var sample = Data.Sample(Elapsed); var position = Point(sample.distance);
        var surface = track.Sample(position.x, position.z); position.y = surface.Height + .04f;
        var forward = Point(sample.distance + 3) - Point(sample.distance - 3);
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, surface.Normal), surface.Normal));
        var before = position - Point(sample.distance - 6); var after = Point(sample.distance + 6) - position;
        before.y = after.y = 0;
        float curvature = Vector3.SignedAngle(before, after, Vector3.up) * Mathf.Deg2Rad / 6;
        float angle = Mathf.Atan(2.52f * curvature) * Mathf.Rad2Deg;
        Model.Animate(angle, sample.speed, dt, true);
        Model.instruments.Set(sample.rpm, sample.speed, sample.gear);
        if(dt>0){
            var contacts=Vector3.zero;
            for(int wheel=0;wheel<4;wheel++)
                contacts+=RideFeedback.Weight(RideFeedback.SurfaceAt(track,transform.TransformPoint(new Vector3((wheel%2==0?-1:1)*.73f,0,(wheel<2?1:-1)*1.26f)),false))*.25f;
            feedback.Tick(dt,transform.forward*sample.speed,transform.rotation,contacts);
        }
        float amount=settings.cockpitMovement;
        Cockpit.transform.localPosition=Model.cockpitView.localPosition+feedback.Offset*amount;
        Cockpit.transform.localRotation=Model.cockpitView.localRotation*Quaternion.Euler(feedback.Angles*amount);
        Bonnet.transform.localPosition=bonnetPosition+feedback.RoadOffset*amount*.25f;
        Bonnet.transform.localRotation=Quaternion.Euler(-7,0,0)*Quaternion.Euler(feedback.RoadAngles*amount*.25f);
        Motor.Asphalt=feedback.Contacts.x*settings.surfaceSound;Motor.Kerb=feedback.Contacts.y*settings.surfaceSound;Motor.LooseGround=feedback.Contacts.z*settings.surfaceSound;
        Motor.Rpm = sample.rpm; Motor.Load = sample.load; Motor.Speed = sample.speed;
    }
    void UpdateLayout()
    {
        scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        canvas = new Rect((Screen.width - 1920 * scale) / 2, (Screen.height - 1080 * scale) / 2, 1920 * scale, 1080 * scale);
        Bonnet.pixelRect = new Rect(canvas.x, canvas.y + 372 * scale, canvas.width, 708 * scale);
        Cockpit.pixelRect = new Rect(canvas.x, canvas.y, 960 * scale, 372 * scale);
        Bonnet.aspect = 1920f / 708; Cockpit.aspect = 960f / 372;
    }
    public void Draw()
    {
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = Color.white;
            caption = new GUIStyle(title) { fontSize = 21, fontStyle = FontStyle.Normal };
            clock = new GUIStyle(title) { fontSize = 120, fontStyle = FontStyle.Normal };
            credit = new GUIStyle(caption) { fontSize = 17 };
            rightTitle = new GUIStyle(title) { alignment = TextAnchor.MiddleRight };
            rightCaption = new GUIStyle(caption) { alignment = TextAnchor.MiddleRight };
        }
        var matrix = GUI.matrix; var color = GUI.color;
        GUI.matrix = Matrix4x4.TRS(new Vector3(canvas.x, canvas.y, 0), Quaternion.identity, new Vector3(scale, scale, 1));
        GUI.color = Color.white;
        GUI.Label(new Rect(1100, 24, 770, 48), $"GOTLAND RING / {Application.version}", rightTitle);
        GUI.Label(new Rect(1000, 75, 870, 32), "SADAIR’S SPEAR • REFERENCE LAP", rightCaption);
        GUI.color = new Color(.035f, .055f, .07f, .9f); GUI.DrawTexture(new Rect(16, 722, 224, 40), Texture2D.whiteTexture);
        GUI.color = Color.white; GUI.Label(new Rect(16, 722, 224, 40), "GAME COCKPIT", caption);
        GUI.color = new Color(.66f, .83f, .79f);
        GUI.Label(new Rect(990, 745, 900, 42), Finished ? "LAP COMPLETE" : Paused ? "REPLAY PAUSED" : "REFERENCE LAP TIME", caption);
        int hundredths = Mathf.RoundToInt(LapTime * 100);
        GUI.color = Color.white; GUI.Label(new Rect(990, 798, 900, 145), $"{hundredths / 6000:00}:{hundredths / 100 % 60:00}.{hundredths % 100:00}", clock);
        GUI.Label(new Rect(990, 940, 900, 30), "POSITION-MATCHED GAME REPLAY", caption);
        GUI.color = new Color(.24f, .33f, .36f); GUI.DrawTexture(new Rect(1030, 981, 820, 3), Texture2D.whiteTexture);
        GUI.color = new Color(.85f, .62f, .37f); GUI.DrawTexture(new Rect(1030, 981, 820 * LapTime / Data.lapDuration, 3), Texture2D.whiteTexture);
        GUI.color = Color.white; GUI.Label(new Rect(985, 995, 910, 30), "O  Return to driving    Space / Esc  Pause    Home  Restart    M  Sound", credit);
        GUI.color = new Color(.61f, .69f, .71f);
        GUI.Label(new Rect(990, 1030, 900, 24), Model.credit, credit);
        GUI.Label(new Rect(990, 1052, 900, 24), "Track: © OpenStreetMap contributors • © Lantmäteriet", credit);
        GUI.matrix = matrix; GUI.color = color;
    }
}
