using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public partial class RingDrive
{
    struct TrailerPose
    {
        public Vector3 position, velocity;
        public Quaternion rotation;
        public Vector3[] wheels;
        public float rpm, load, steering, slip;
        public int gear;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartTrailerCapture()
    {
        if (!Array.Exists(Environment.GetCommandLineArgs(), a => a == "--trailer-capture")) return;
        var game = FindFirstObjectByType<RingDrive>();
        game.enabled = false;
        game.StartCoroutine(game.CaptureTrailer());
    }

    TrailerPose TrailerState()
    {
        var wheels = new Vector3[4];
        for (int i = 0; i < 4; i++) {
            awd.Wheels[i].GetWorldPose(out var position, out _);
            wheels[i] = car.InverseTransformPoint(position);
        }
        return new TrailerPose {
            position = car.position, rotation = car.rotation, velocity = awd.Body.linearVelocity,
            rpm = awd.EngineRpm, gear = awd.Gear, load = throttle,
            steering = awd.SteeringDegrees, slip = awd.SideslipDegrees, wheels = wheels
        };
    }

    static TrailerPose TrailerSample(List<TrailerPose> poses, float seconds)
    {
        float index = Mathf.Clamp(seconds * 100, 0, poses.Count - 1);
        int first = (int)index, next = Mathf.Min(first + 1, poses.Count - 1);
        var a = poses[first]; var b = poses[next]; float t = index - first;
        var wheels = new Vector3[4];
        for (int i = 0; i < 4; i++) wheels[i] = Vector3.Lerp(a.wheels[i], b.wheels[i], t);
        return new TrailerPose {
            position = Vector3.Lerp(a.position, b.position, t),
            rotation = Quaternion.Slerp(a.rotation, b.rotation, t),
            velocity = Vector3.Lerp(a.velocity, b.velocity, t),
            rpm = Mathf.Lerp(a.rpm, b.rpm, t), load = Mathf.Lerp(a.load, b.load, t),
            steering = Mathf.Lerp(a.steering, b.steering, t), slip = Mathf.Lerp(a.slip, b.slip, t), gear = a.gear, wheels = wheels
        };
    }

    IEnumerator CaptureTrailer()
    {
        const int fps = 30, rate = 48000, width = 1920, height = 1080;
        string output = Environment.GetEnvironmentVariable("GOTLAND_TRAILER_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("Set GOTLAND_TRAILER_OUTPUT to the capture directory.");
        Directory.CreateDirectory(output);
        bool preview = Array.Exists(Environment.GetCommandLineArgs(), a => a == "--trailer-preview");
        Application.runInBackground = true; QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = false;
        motor.Muted = true; motor.GetComponent<AudioSource>().Stop();
        yield return null;
        Physics.simulationMode = SimulationMode.Script;
        dynamics = new DrivingSettings(); awd.SetSimulationActive(true);
        awd.Body.interpolation = RigidbodyInterpolation.None;
        RecoverCar(TrackLandmarks.StartFinishPoint); lap = 1; best = 0; lapStart = 0;
        SetAutopilot(true);
        for (int i = 0; i < 300; i++) { awd.Step(.01f, 0, 1, 0, dynamics); Physics.Simulate(.01f); }
        var circuit = new List<TrailerPose>(27000) { TrailerState() };
        for (int step = 1; step <= 26000; step++)
        {
            StepDriving(.01f, step * .01f, 0, 0, 0, false);
            Physics.Simulate(.01f); SyncAwdMotion();
            circuit.Add(TrailerState());
            if (step % 1000 == 0) yield return null;
        }
        Debug.Log($"TRAILER circuit recorded {circuit.Count} poses, laps={lap}");

        MakeTrainingArea(); drivingSurface.Training = true; SetAutopilot(false);
        awd.ResetCar(DrivingSurface.TrainingCentre + new Vector3(0, .3f, -350), Quaternion.identity);
        yield return null;
        for (int i = 0; i < 300; i++) { awd.Step(.01f, 0, 1, 0, dynamics); Physics.Simulate(.01f); }
        for (int i = 0; i < 2000 && awd.ForwardSpeed * 3.6f < 100; i++)
        { awd.Step(.01f, 1, 0, 0, dynamics); Physics.Simulate(.01f); }
        throttle = 1;
        var sliding = new List<TrailerPose>(801) { TrailerState() };
        float peakSlip = 0;
        for (int step = 0; step < 800; step++)
        {
            float steering = (step / 80) % 2 == 0 ? 1 : -1;
            awd.Step(.01f, 1, 0, steering, dynamics); Physics.Simulate(.01f);
            peakSlip = Mathf.Max(peakSlip, Mathf.Abs(awd.SideslipDegrees));
            sliding.Add(TrailerState());
        }
        Debug.Log($"TRAILER slide recorded, peakSideslip={peakSlip:F1}deg");
        awd.SetSimulationActive(false);
        drivingSurface.Training = false;
        cam.transform.SetParent(null, true); cam.enabled = false; cam.aspect = width / (float)height;
        cam.allowHDR = false;
        var render = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        render.Create(); cam.targetTexture = render;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        var engine = BoxerAudio.CreateMixer(rate); var surfaceAudio = new SurfaceAudioMixer();
        var audio = new float[rate / fps * 2];
        using var sound = new BinaryWriter(File.Create(Path.Combine(output, "engine.wav")));
        WriteTrailerWavHeader(sound, rate, 20 * rate * 2 * 2);
        using var telemetry = new StreamWriter(Path.Combine(output, "shots.csv"));
        telemetry.WriteLine("frame,shot,time,speed_kph,rpm,gear,sideslip_deg,camera_distance_m");
        var passPose = TrailerSample(circuit, 252.8f);
        Vector3 passCentre = passPose.position;
        Vector3 passForward = passPose.velocity.normalized;
        Vector3 passRight = Vector3.Cross(Vector3.up, passForward).normalized;
        Vector3 passCamera = passCentre + passRight * 5.0f;
        passCamera.y = Ground(passCamera.x, passCamera.z) + 1.0f;
        int[] duration = { 90, 150, 150, 150, 60 };
        float[] start = { 248.5f, 47, .3f, 249.5f, 251 };
        string[] names = { "opening", "cockpit", "slide", "gutemalrakan", "closing" };
        int frame = 0;
        for (int shot = 0; shot < duration.Length; shot++)
        {
            int model = shot >= 2 ? 1 : 0;
            if (selectedCar != model) SwitchCarModel();
            drivingSurface.Training = shot == 2; ResetFeedback();
            var poses = shot == 2 ? sliding : circuit;
            for (int local = 0; local < duration[shot]; local++, frame++)
            {
                float seconds = local / (float)fps;
                var pose = TrailerSample(poses, start[shot] + seconds);
                car.SetPositionAndRotation(pose.position, pose.rotation);
                Vector3 direction = pose.velocity.normalized, right = Vector3.Cross(Vector3.up, direction).normalized;
                carModel.Animate(pose.steering, Vector3.Dot(pose.velocity, car.forward), 1f / fps, shot == 1);
                for (int wheel = 0; wheel < 4; wheel++) {
                    var hub = carModel.wheelSpin[wheel].parent;
                    var localPosition = hub.localPosition;
                    localPosition.y = hub.parent.InverseTransformPoint(car.TransformPoint(pose.wheels[wheel])).y
                        + carModel.wheelRadius - awd.Wheels[wheel].radius;
                    hub.localPosition = localPosition;
                }
                carModel.instruments.Set(pose.rpm, pose.velocity.magnitude, pose.gear);
                Vector3 target = pose.position + Vector3.up * .8f;
                cam.fieldOfView = shot == 1 ? 64 : shot == 3 ? 54 : 48;
                if (shot == 1)
                {
                    rideFeedback.Tick(1f / fps, pose.velocity, pose.rotation, Vector3.right);
                    cam.transform.position = carModel.cockpitView.position + car.TransformVector(rideFeedback.Offset * .5f);
                    cam.transform.rotation = carModel.cockpitView.rotation * Quaternion.Euler(rideFeedback.Angles * .5f);
                }
                else
                {
                    if (shot == 0 || shot == 4)
                        cam.transform.position = target + direction * 8 + right * Mathf.Lerp(4, 6, seconds / 3) + Vector3.up * 1.0f;
                    else if (shot == 2)
                        cam.transform.position = target - direction * 8.5f - right * 5.5f + Vector3.up * 1.3f;
                    else cam.transform.position = passCamera;
                    cam.transform.rotation = Quaternion.LookRotation(target - cam.transform.position, Vector3.up);
                }
                AnimateWindTurbines(1f / fps);
                float distance = Vector3.Distance(cam.transform.position, target);
                float radialSpeed = Vector3.Dot(pose.velocity, (target - cam.transform.position).normalized);
                float doppler = shot == 3 ? 343 / (343 + radialSpeed) : 1;
                engine.Render(audio, 2, pose.rpm * doppler, pose.load, pose.velocity.magnitude, false);
                surfaceAudio.Render(audio, 2, rate, pose.velocity.magnitude, shot == 1 ? .65f : .2f, 0, 0, false);
                float gain = shot == 3 ? Mathf.Clamp(18 / (distance + 8), .07f, 1.0f) : shot == 1 ? .85f : .95f;
                float pan = shot == 3 ? Mathf.Clamp(cam.transform.InverseTransformPoint(target).x / 12, -.6f, .6f) : 0;
                float edge = Mathf.Min(Mathf.Clamp01(seconds / .035f), Mathf.Clamp01((duration[shot] / (float)fps - seconds) / .035f));
                for (int n = 0; n < audio.Length; n++)
                    sound.Write((short)(Mathf.Clamp(audio[n] * gain * edge * (n % 2 == 0 ? 1 - pan : 1 + pan), -1, 1) * short.MaxValue));
                telemetry.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:F3},{3:F2},{4:F0},{5},{6:F2},{7:F2}",
                    frame, names[shot], frame / (float)fps, pose.velocity.magnitude * 3.6f, pose.rpm, pose.gear, pose.slip, distance));
                if (!preview || local == duration[shot] / 2)
                {
                    cam.Render(); RenderTexture.active = render;
                    image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(output, $"frame-{frame:0000}.jpg"), image.EncodeToJPG(96));
                }
                if (local % 30 == 0) { Debug.Log($"TRAILER frame={frame}/600 shot={names[shot]}"); yield return null; }
            }
        }
        cam.targetTexture = null; render.Release(); Destroy(render); Destroy(image);
        Debug.Log("TRAILER_CAPTURE_DONE frames=600 duration=20s fps=30 resolution=1920x1080");
        Application.Quit();
    }

    static void WriteTrailerWavHeader(BinaryWriter writer, int rate, int bytes)
    {
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(bytes + 36);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
        writer.Write((short)1); writer.Write((short)2); writer.Write(rate); writer.Write(rate * 4);
        writer.Write((short)4); writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(bytes);
    }
}
