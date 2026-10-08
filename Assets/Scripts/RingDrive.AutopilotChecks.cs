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
        try { CheckAutopilotState(); }
        catch (Exception e) { Debug.LogException(e); Application.Quit(1); yield break; }
        var original = dynamics.Copy();
        var setups = new[] {
            new DrivingSettings(),
            new DrivingSettings { grip=10, braking=6, acceleration=1.8f, response=1, steering=25, highSpeedSteering=8 },
            new DrivingSettings { grip=40, braking=20, acceleration=1.8f, response=1, steering=55, highSpeedSteering=25 },
            original
        };
        string[] names = { "defaults", "low-grip-weak-brakes", "high-power-slow-steering", "saved-settings" };
        for (int scenario = 0; scenario < setups.Length; scenario++)
        {
            dynamics = setups[scenario];
            RestartLap(); lap = 1; best = 0; lapStart = 0;
            SetAutopilot(true);
            int requiredLaps = scenario == 0 ? 2 : 1;
            int steps = 0, brakeSteps = 0, leftSteps = 0, rightSteps = 0, visitedCount = 0;
            var visited = new bool[track.Count];
            float maxDeviation = 0, maxSpeed = 0, travelled = 0, minRoadMargin = float.MaxValue;
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
            Debug.Log($"AUTOPILOT_TEST scenario={names[scenario]} laps={lap-1} elapsed={steps*.01f:F2}s best={best:F2}s maxSpeed={maxSpeed*3.6f:F1}km/h maxDeviation={maxDeviation:F2}m minRoadMargin={minRoadMargin:F2}m distance={travelled:F1}m visited={visitedCount}/{track.Count} brakeSteps={brakeSteps} leftSteps={leftSteps} rightSteps={rightSteps} pass={passed}");
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
        float recoveryError = DistanceToTrack(car.position, out _);
        bool recovered = recoveryError < 2 && velocity.magnitude > 15 && !reversing && lap == 1;
        Debug.Log($"AUTOPILOT_RECOVERY deviation={recoveryError:F2} speed={velocity.magnitude:F2} pass={recovered}");
        if (!recovered) { Application.Quit(1); yield break; }
        Debug.Log("AUTOPILOT_TEST ALL PASSED: full laps, both steering directions, braking, settings limits, pause, toggle, recovery");
        lapStart = Time.time;
        view = 2;
        // Resume the normal frame/fixed-update loop for a real rendered screenshot.
        autopilotTest = false;
        yield return new WaitForSeconds(2);
        CaptureScreenshot("autopilot-test.png");
        yield return new WaitForSeconds(1);
        Application.Quit();
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
