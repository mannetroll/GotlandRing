using System;
using System.Collections;
using UnityEngine;

public partial class RingDrive
{
    // Opt-in standalone regression: uses the same 100 Hz physics step as normal play,
    // batching steps between rendered frames so several complete laps take seconds.
    IEnumerator AutopilotTest()
    {
        Application.runInBackground = true;
        muted = true;
        yield return null;
        try { CheckLapTiming(); CheckRacingLine(); CheckMouseSteering(); CheckAutopilotState(); }
        catch (Exception e) { Debug.LogException(e); Application.Quit(1); yield break; }
        var original = dynamics.Copy();
        var setups = new[] {
            new DrivingSettings(),
            new DrivingSettings { grip=10, braking=6, acceleration=1.8f, response=1, steering=25, highSpeedSteering=8 },
            new DrivingSettings { grip=40, braking=20, acceleration=1.8f, response=1, steering=55, highSpeedSteering=25 },
            original
        };
        string[] names = { "defaults", "low-grip-weak-brakes", "high-power-slow-steering", "saved-settings" };
        foreach (var setup in setups) setup.awdMode = false;
        for (int scenario = 0; scenario < setups.Length; scenario++)
        {
            dynamics = setups[scenario];
            RestartLap(); lap = 1; best = 0; lapStart = 0;
            SetAutopilot(true);
            int requiredLaps = scenario == 0 ? 2 : 1;
            int steps = 0, brakeSteps = 0, leftSteps = 0, rightSteps = 0, visitedCount = 0;
            var visited = new bool[track.Count];
            float maxDeviation = 0, maxSpeed = 0, travelled = 0, minRoadMargin = float.MaxValue;
            float minBodyMargin = float.MaxValue, maxLineError = 0;
            while (lap <= requiredLaps && steps < 90000)
            {
                for (int batch = 0; batch < 250 && lap <= requiredLaps; batch++)
                {
                    var before = car.position;
                    StepDriving(.01f, ++steps * .01f);
                    var contact = centerline.Sample(car.position.x, car.position.z);
                    int index = contact.Segment;
                    maxDeviation = Mathf.Max(maxDeviation, contact.Distance);
                    minRoadMargin = Mathf.Min(minRoadMargin, contact.Width - Mathf.Abs(contact.Offset));
                    if (steps > 1000) maxLineError = Mathf.Max(maxLineError, pilotControls.LineError);
                    // Check a 4.32 x 2 m footprint against the actual asphalt,
                    // including the front/rear overhang when entering a bend.
                    if (steps % 10 == 0) for (int corner = 0; corner < 4; corner++)
                    {
                        var point = car.position + car.forward * (corner < 2 ? 2.16f : -2.16f)
                            + car.right * (corner % 2 == 0 ? 1 : -1);
                        var edge = centerline.Sample(point.x, point.z);
                        minBodyMargin = Mathf.Min(minBodyMargin, edge.Width - Mathf.Abs(edge.Offset));
                        if (!edge.OnRoad)
                        {
                            Debug.LogError($"AUTOPILOT_BODY_FAIL scenario={names[scenario]} time={steps*.01f:F2} index={index} corner={corner} margin={minBodyMargin:F2} speed={velocity.magnitude:F2} lineError={pilotControls.LineError:F2} offset={contact.Offset:F2}");
                            Application.Quit(1); yield break;
                        }
                    }
                    maxSpeed = Mathf.Max(maxSpeed, velocity.magnitude);
                    travelled += Vector3.Distance(before, car.position);
                    if (!visited[index]) { visited[index] = true; visitedCount++; }
                    if (pilotControls.Brake > .1f) brakeSteps++;
                    if (pilotControls.Steering < -.01f) leftSteps++;
                    if (pilotControls.Steering > .01f) rightSteps++;
                    if (!contact.OnRoad || !float.IsFinite(velocity.magnitude))
                    {
                        Debug.LogError($"AUTOPILOT_TEST FAIL scenario={names[scenario]} time={steps*.01f:F2} index={index} deviation={maxDeviation:F2} roadMargin={minRoadMargin:F2} speed={velocity.magnitude:F2}");
                        Application.Quit(1); yield break;
                    }
                }
                yield return null;
            }
            bool passed = lap > requiredLaps && visitedCount > track.Count * .98f && travelled > length * requiredLaps * .95f
                && brakeSteps > 100 && leftSteps > 100 && rightSteps > 100 && maxSpeed > 35;
            Debug.Log($"AUTOPILOT_TEST scenario={names[scenario]} laps={lap-1} elapsed={steps*.01f:F2}s best={best:F2}s maxSpeed={maxSpeed*3.6f:F1}km/h maxDeviation={maxDeviation:F2}m maxLineError={maxLineError:F2}m minRoadMargin={minRoadMargin:F2}m minBodyMargin={minBodyMargin:F2}m distance={travelled:F1}m visited={visitedCount}/{track.Count} brakeSteps={brakeSteps} leftSteps={leftSteps} rightSteps={rightSteps} pass={passed}");
            if (!passed) { Application.Quit(1); yield break; }
        }
        dynamics = original;
        RestartLap(); lap = 1; lapStart = 0;
        car.position += car.right * 10;
        yaw += 110;
        velocity = -new Vector3(Mathf.Sin(yaw*Mathf.Deg2Rad),0,Mathf.Cos(yaw*Mathf.Deg2Rad)) * 4;
        reversing = true;
        SetAutopilot(true);
        for (int step = 0; step < 6000; step++)
        {
            StepDriving(.01f, step * .01f);
            if (step % 250 == 0) yield return null;
        }
        float recoveryError = autopilot.Drive(car.position, yaw, velocity, centerline.Sample(car.position.x,car.position.z)).LineError;
        bool recovered = recoveryError < 2 && velocity.magnitude > 15 && !reversing && lap == 1;
        Debug.Log($"AUTOPILOT_RECOVERY lineError={recoveryError:F2} speed={velocity.magnitude:F2} pass={recovered}");
        if (!recovered) { Application.Quit(1); yield break; }
        Debug.Log("AUTOPILOT_TEST ALL PASSED: racing line, full laps, body clearance, both steering directions, braking, settings limits, pause, toggle, recovery");
        lapStart = Time.time;
        view = 2;
        // Resume the normal frame/fixed-update loop for a real rendered screenshot.
        autopilotTest = false;
        yield return new WaitForSeconds(2);
        CaptureScreenshot("autopilot-test.png");
        yield return new WaitForSeconds(1);
        Application.Quit();
    }

    void CheckRacingLine()
    {
        var line = autopilot.RacingLine;
        double lineCost = 0, centerCost = 0, offsetSquared = 0;
        float margin = float.MaxValue, maxOffset = 0;
        var csv = new System.Text.StringBuilder("distance_m,x_m,y_m,z_m,offset_m\n");
        for (int i = 0; i < track.Count; i++)
        {
            var road = centerline.Sample(line[i].x, line[i].z);
            margin = Mathf.Min(margin, road.Width - Mathf.Abs(road.Offset));
            maxOffset = Mathf.Max(maxOffset, Mathf.Abs(road.Offset));
            offsetSquared += road.Offset * road.Offset;
            if (!road.OnRoad || margin < 2.55f) throw new Exception("Racing line leaves its asphalt clearance at " + i);
            double Cost(System.Collections.Generic.IReadOnlyList<Vector3> path)
            {
                var a = path[i] - path[(i + track.Count - 2) % track.Count]; a.y = 0;
                var b = path[(i + 2) % track.Count] - path[i]; b.y = 0;
                float curvature = 2 * Vector3.Cross(a,b).y / (a.magnitude*b.magnitude*(a+b).magnitude);
                return curvature * curvature * (a.magnitude+b.magnitude) * .25;
            }
            lineCost += Cost(line); centerCost += Cost(track);
            csv.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F3},{1:F3},{2:F3},{3:F3},{4:F3}\n",
                centerline.Sections[i].Distance, line[i].x, line[i].y, line[i].z, road.Offset);
        }
        double rmsOffset = Math.Sqrt(offsetSquared / track.Count);
        if (lineCost >= centerCost || rmsOffset < 1) throw new Exception("Racing line does not improve corner curvature or use the track width");
        System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath,"racing-line.csv"),csv.ToString());
        Debug.Log($"RACING_LINE_TEST curvatureRatio={lineCost/centerCost:F3} rmsOffset={rmsOffset:F2}m maxOffset={maxOffset:F2}m minMargin={margin:F2}m pass=True");
    }

    void CheckAutopilotState()
    {
        SetAutopilot(true);
        if (!autopilotEnabled) throw new Exception("Autopilot did not enable");
        SetPaused(true);
        var position = car.position;
        StepDriving(.01f, Time.time);
        if (car.position != position) throw new Exception("Autopilot moved while paused");
        OpenSettings(); CloseSettings(false);
        if (!paused || !autopilotEnabled) throw new Exception("Settings lost paused/autopilot state");
        SetPaused(false);
        SetAutopilot(false);
        if (autopilotEnabled || pilotControls.Throttle != 0 || pilotControls.Brake != 0) throw new Exception("Autopilot did not relinquish control");
        Debug.Log("AUTOPILOT_STATE_TEST pass=True");
    }
}
