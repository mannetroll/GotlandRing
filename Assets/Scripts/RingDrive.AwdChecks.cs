using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;

public partial class RingDrive
{
    IEnumerator AwdTest()
    {
        Application.runInBackground = true; muted = true;
        yield return null;
        var previousSimulation = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        awd.Body.interpolation = RigidbodyInterpolation.None;
        dynamics = new DrivingSettings();
        string output = Environment.GetEnvironmentVariable("GOTLAND_TEST_OUTPUT") ?? Application.persistentDataPath;
        Directory.CreateDirectory(output);
        try
        {
            CheckAwdRoadContact();
            CheckAwdBanks();
            CheckAwdControls();
            CheckAwdAcceleration(output);
        }
        catch (Exception e) { Debug.LogException(e); Application.Quit(1); yield break; }

        RestartLap(); lap = 1; best = 0; lapStart = 0;
        SetAutopilot(true);
        for (int i = 0; i < 300; i++) { awd.Step(.01f, 0, 1, 0, dynamics); Physics.Simulate(.01f); }
        int steps = 0, visitedCount = 0, powered = 0, brakeSteps = 0, leftSteps = 0, rightSteps = 0;
        var visited = new bool[track.Count];
        float maxSideslip = 0, slideSeconds = 0, rearPeakSeconds = 0;
        float minBodyMargin = float.MaxValue;
        bool capturedSlide = false;
        float travelled = 0, minMargin = float.MaxValue, minUpright = 1, maxSpeed = 0, maxLineError = 0;
        using (var telemetry = new StreamWriter(Path.Combine(output, "awd-lap.csv")))
        {
            telemetry.WriteLine("time_s,segment,speed_kph,target_kph,line_error_m,road_margin_m,front_torque_nm,rear_torque_nm,grounded,steering_deg,x_m,y_m,z_m,sideslip_deg,front_slip,rear_slip,throttle,brake");
            while (lap == 1 && steps < 70000)
            {
                for (int batch = 0; batch < 250 && lap == 1; batch++)
                {
                    var before = car.position;
                    StepDriving(.01f, ++steps * .01f);
                    Physics.Simulate(.01f); SyncAwdMotion();
                    var sample = centerline.Sample(car.position.x, car.position.z);
                    nearest = sample.Segment; UpdateLap(steps * .01f);
                    travelled += Vector3.Distance(before, car.position);
                    if (!visited[nearest]) { visited[nearest] = true; visitedCount++; }
                    float margin = sample.Width - Mathf.Abs(sample.Offset);
                    minMargin = Mathf.Min(minMargin, margin);
                    minUpright = Mathf.Min(minUpright, Vector3.Dot(car.up, sample.Normal));
                    maxSpeed = Mathf.Max(maxSpeed, velocity.magnitude);
                    maxLineError = Mathf.Max(maxLineError, pilotControls.LineError);
                    for (int i = 0; i < 4; i++) if (awd.Wheels[i].motorTorque > 1) powered |= 1 << i;
                    float beta = Mathf.Abs(awd.SideslipDegrees);
                    if (velocity.magnitude > 12)
                    {
                        maxSideslip = Mathf.Max(maxSideslip, beta);
                        if (beta > 3 && Mathf.Abs(pilotControls.Curvature) > .001f) slideSeconds += .01f;
                        if (awd.RearLateralSlip > awd.setup.rearLateralPeakSlip) rearPeakSeconds += .01f;
                    }
                    if (pilotControls.Brake > .1f) brakeSteps++;
                    if (pilotControls.Steering > .02f) rightSteps++;
                    if (pilotControls.Steering < -.02f) leftSteps++;
                    if (steps % 10 == 0) for (int corner = 0; corner < 4; corner++)
                    {
                        var point = car.position + car.forward * (corner < 2 ? 2.16f : -2.16f)
                            + car.right * (corner % 2 == 0 ? 1 : -1);
                        var edge = centerline.Sample(point.x, point.z);
                        minBodyMargin = Mathf.Min(minBodyMargin, edge.Width - Mathf.Abs(edge.Offset));
                    }
                    if (steps % 10 == 0) telemetry.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0:F2},{1},{2:F2},{3:F2},{4:F3},{5:F3},{6:F1},{7:F1},{8},{9:F2},{10:F3},{11:F3},{12:F3},{13:F3},{14:F4},{15:F4},{16:F3},{17:F3}",
                        steps * .01f, nearest, velocity.magnitude * 3.6f, pilotControls.TargetSpeed * 3.6f,
                        pilotControls.LineError, margin, awd.FrontTorque, awd.RearTorque, awd.GroundedWheels,
                        awd.SteeringDegrees, car.position.x, car.position.y, car.position.z,
                        awd.SideslipDegrees, awd.FrontLateralSlip, awd.RearLateralSlip, pilotControls.Throttle, pilotControls.Brake));
                    if (!sample.OnRoad || minUpright < .85f || !float.IsFinite(velocity.magnitude))
                    {
                        Debug.LogError($"AWD_LAP_FAIL time={steps*.01f:F2} segment={nearest} speed={velocity.magnitude*3.6f:F1} margin={margin:F2} lineError={pilotControls.LineError:F2} grounded={awd.GroundedWheels} upright={minUpright:F3}");
                        Application.Quit(1); yield break;
                    }
                }
                if (steps % 5000 == 0) Debug.Log($"AWD_LAP_PROGRESS time={steps*.01f:F0}s segment={nearest} speed={velocity.magnitude*3.6f:F1}km/h visited={visitedCount}/{track.Count}");
                yield return null;
                if (!capturedSlide && !Application.isBatchMode && steps > 5000
                    && Mathf.Abs(awd.SideslipDegrees) > 4 && Mathf.Abs(pilotControls.Curvature) > .004f
                    && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                {
                    view = 2; UpdateCameraPose();
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, "awd-cornering.png"));
                    capturedSlide = true;
                }
            }
        }
        bool passed = lap == 2 && travelled > length * .98f && visitedCount > track.Count * .98f
            && powered == 15 && brakeSteps > 100 && leftSteps > 100 && rightSteps > 100 && maxSpeed > 25
            && minBodyMargin > 0 && maxSideslip < 12 && slideSeconds > 3;
        string report = string.Format(CultureInfo.InvariantCulture,
            "AWD_LAP_TEST pass={0} lapSeconds={1:F2} distance={2:F1}m visited={3}/{4} maxSpeed={5:F1}km/h minRoadMargin={6:F2}m maxLineError={7:F2}m minUpright={8:F4} drivenMask={9} brakingSteps={10} maxSideslip={11:F2}deg slideSeconds={12:F2} rearPeakSeconds={13:F2} minBodyMargin={14:F2}m",
            passed, steps*.01f, travelled, visitedCount, track.Count, maxSpeed*3.6f, minMargin, maxLineError, minUpright, powered, brakeSteps, maxSideslip, slideSeconds, rearPeakSeconds, minBodyMargin);
        Debug.Log(report); File.WriteAllText(Path.Combine(output,"awd-results.txt"), report + "\n");
        if (!passed) { Application.Quit(1); yield break; }
        Physics.simulationMode = previousSimulation;
        if (!Application.isBatchMode && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            awdTest = false; view = 2; lapStart = Time.time;
            awd.Body.interpolation = RigidbodyInterpolation.Interpolate;
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"awd-driving.png"));
            yield return new WaitForSeconds(1);
        }
        Debug.Log("AWD_TEST ALL PASSED: contact, banking, four-wheel drive, reverse, pause, mode/model switch, controlled corner slip, body clearance, full lap");
        Application.Quit();
    }

    void CheckAwdRoadContact()
    {
        var collider = GameObject.Find("Estimated banked track surface").GetComponent<MeshCollider>();
        float maximumError = 0;
        for (int i = 0; i < track.Count; i += 11)
            foreach (float fraction in new[] {-.7f, 0, .7f})
            {
                var section = centerline.Sections[i];
                var point = section.Road(fraction * (fraction < 0 ? section.LeftWidth : section.RightWidth));
                if (!collider.Raycast(new Ray(point + Vector3.up * 5, Vector3.down), out var hit, 10))
                    throw new Exception("Physical road has a gap at section " + i);
                maximumError = Mathf.Max(maximumError, Mathf.Abs(hit.point.y - point.y));
            }
        if (maximumError > .015f) throw new Exception("Physical road differs from visible road: " + maximumError);
        Debug.Log($"AWD_ROAD_CONTACT pass=True maxError={maximumError:F5}m");
    }

    void CheckAwdBanks()
    {
        int positive = 0, negative = 0;
        for (int i = 0; i < track.Count; i++)
        {
            if (centerline.Sections[i].BankingDegrees > centerline.Sections[positive].BankingDegrees) positive = i;
            if (centerline.Sections[i].BankingDegrees < centerline.Sections[negative].BankingDegrees) negative = i;
        }
        foreach (int index in new[] {0, positive, negative, track.Count - 1})
        {
            RecoverCar(index);
            for (int step = 0; step < 300; step++) { awd.Step(.01f, 0, 1, 0, dynamics); Physics.Simulate(.01f); }
            var normal = centerline.Sample(car.position.x, car.position.z).Normal;
            if (awd.GroundedWheels != 4 || Vector3.Dot(car.up, normal) < .97f)
                throw new Exception($"AWD did not settle on banking: section={index} wheels={awd.GroundedWheels} alignment={Vector3.Dot(car.up,normal)}");
        }
        Debug.Log("AWD_BANK_CONTACT pass=True four contacts at start, both banking extremes and lap seam");
    }

    void CheckAwdControls()
    {
        RecoverCar(0);
        for (int i = 0; i < 300; i++) { awd.Step(.01f, 0, 1, 0, dynamics); Physics.Simulate(.01f); }
        var start = car.position; int reverseMask = 0;
        for (int i = 0; i < 150; i++)
        {
            awd.Step(.01f, -1, 0, 0, dynamics); Physics.Simulate(.01f);
            for (int w = 0; w < 4; w++) if (awd.Wheels[w].motorTorque < -1) reverseMask |= 1 << w;
        }
        if (reverseMask != 15 || Vector3.Dot(car.position - start, car.forward) > -1)
            throw new Exception("Reverse did not power all four wheels or move backwards");
        SyncAwdMotion(); CheckCarSwitchState(); CheckCarSwitchState();
        var motion = awd.Body.linearVelocity; var angular = awd.Body.angularVelocity;
        var position = car.position; var rotation = car.rotation;
        SetPaused(true);
        for (int i = 0; i < 100; i++) Physics.Simulate(.01f);
        if (Vector3.Distance(car.position,position) > .001f || Quaternion.Angle(car.rotation,rotation) > .01f)
            throw new Exception("AWD moved while paused");
        SetPaused(false);
        if (Vector3.Distance(awd.Body.linearVelocity,motion) > .001f || Vector3.Distance(awd.Body.angularVelocity,angular) > .001f)
            throw new Exception("Pause lost AWD velocity");
        for (int i = 0; i < 500; i++) { awd.Step(.01f,-1,1,0,dynamics); Physics.Simulate(.01f); }
        if (awd.Body.linearVelocity.magnitude > .2f || awd.FrontTorque != 0 || awd.RearTorque != 0)
            throw new Exception("Braking did not stop reverse drive");
        var arcade = dynamics.Copy(); arcade.awdMode = false; ApplyDrivingSettings(arcade);
        if (!awd.Body.isKinematic || awd.Wheels[0].enabled) throw new Exception("Arcade mode kept wheel physics active");
        var physical = dynamics.Copy(); physical.awdMode = true; ApplyDrivingSettings(physical);
        if (awd.Body.isKinematic || !awd.Wheels[0].enabled) throw new Exception("AWD mode did not activate wheel physics");
        Debug.Log("AWD_CONTROLS pass=True reverse, model swap, pause/resume, brake cut, handling-mode switch");
    }

    void CheckAwdAcceleration(string output)
    {
        // The start lies on the circuit's long straight. Steering follows its centreline.
        RecoverCar(0);
        for (int i = 0; i < 300; i++) { awd.Step(.01f,0,1,0,dynamics); Physics.Simulate(.01f); }
        float elapsed = 0;
        float Steering()
        {
            var sample = centerline.Sample(car.position.x,car.position.z);
            var target = car.InverseTransformPoint(track[(sample.Segment+8)%track.Count]);
            return Mathf.Atan2(2*awd.setup.wheelbase*target.x,target.x*target.x+target.z*target.z)
                *Mathf.Rad2Deg/awd.setup.SteeringLimit(awd.ForwardSpeed);
        }
        while (awd.Body.linearVelocity.magnitude*3.6f < 100 && elapsed < 20)
        {
            awd.Step(.01f,1,0,Steering(),dynamics); Physics.Simulate(.01f); elapsed += .01f;
        }
        float startSpeed = awd.Body.linearVelocity.magnitude*3.6f;
        var brakeStart = car.position;
        for (int i=0;i<1000 && awd.Body.linearVelocity.magnitude>.14f;i++)
        {
            awd.Step(.01f,0,1,Steering(),dynamics); Physics.Simulate(.01f);
        }
        float distance = Vector3.Distance(brakeStart,car.position);
        bool passed = elapsed > 4 && elapsed < 12 && startSpeed >= 100 && distance > 20 && distance < 80
            && awd.Body.linearVelocity.magnitude < .14f && awd.GroundedWheels == 4
            && centerline.Sample(car.position.x,car.position.z).OnRoad;
        string report=string.Format(CultureInfo.InvariantCulture,
            "AWD_STRAIGHT_TEST pass={0} acceleration0To100={1:F2}s brakeStart={2:F2}km/h stoppingDistance={3:F2}m",passed,elapsed,startSpeed,distance);
        Debug.Log(report);File.WriteAllText(Path.Combine(output,"awd-straight-results.txt"),report+"\n");
        if(!passed)throw new Exception("Stock-baseline acceleration/braking failed");
    }
}
