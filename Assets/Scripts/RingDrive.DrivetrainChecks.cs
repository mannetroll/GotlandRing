using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public partial class RingDrive
{
    void CheckTrainingTurnaround(string output)
    {
        SetAutopilot(false); mouseSteering = false;
        drivingSurface.Training = true;
        using var telemetry = new StreamWriter(Path.Combine(output, "awd-turnaround.csv"));
        telemetry.WriteLine("entry_kph,period_s,direction,time_s,speed_kph,heading_deg,slide_deg,rpm,rear_slip,steering_deg,heading_sweep_deg");
        int failures = 0;
        foreach (int entry in new[] {0, 50, 100})
        foreach (float period in new[] {.4f, .8f, 1.2f})
        foreach (int direction in new[] {-1, 1})
        {
            dynamics = new DrivingSettings();
            awd.ResetCar(DrivingSurface.TrainingCentre + new Vector3(0, .3f, -300), Quaternion.identity);
            for (int i = 0; i < 300; i++) { awd.Step(.01f, 0, 1, 0, dynamics); Physics.Simulate(.01f); }
            int launch = 0;
            while (awd.ForwardSpeed * 3.6f < entry && launch++ < 2000)
            { StepDriving(.01f, Time.time, 0, 1, 0, false); Physics.Simulate(.01f); }
            float heading = 0, previous = car.eulerAngles.y, minHeading = 0, maxHeading = 0;
            float headingSweep = 0, peakSlide = 0, rearPeakSeconds = 0;
            int steps = 0;
            for (; steps < 2000; steps++)
            {
                float steering = direction * (steps / Mathf.RoundToInt(period / .01f) % 2 == 0 ? 1 : -1);
                StepDriving(.01f, Time.time, steering, 1, 0, false);
                Physics.Simulate(.01f); SyncAwdMotion();
                heading += Mathf.DeltaAngle(previous, car.eulerAngles.y); previous = car.eulerAngles.y;
                // A steering flick changes the entry heading; measure the full turn between reached headings.
                minHeading = Mathf.Min(minHeading, heading); maxHeading = Mathf.Max(maxHeading, heading);
                headingSweep = maxHeading - minHeading;
                float slide = Mathf.Atan2(Vector3.Dot(velocity, car.right), awd.ForwardSpeed) * Mathf.Rad2Deg;
                if (velocity.magnitude > 5) peakSlide = Mathf.Max(peakSlide, Mathf.Abs(slide));
                if (awd.RearLateralSlip > awd.setup.lateralPeakSlip) rearPeakSeconds += .01f;
                foreach (var wheel in awd.Wheels)
                    if (wheel.brakeTorque != 0) throw new Exception("Turnaround applied unrequested brakes");
                if (!drivingSurface.Sample(car.position.x, car.position.z).OnRoad)
                    throw new Exception("Turnaround left the training asphalt");
                if (steps % 5 == 0) telemetry.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3:F2},{4:F2},{5:F2},{6:F2},{7:F0},{8:F3},{9:F2},{10:F2}", entry, period, direction,
                    steps * .01f, velocity.magnitude * 3.6f, heading, slide, awd.EngineRpm, awd.RearLateralSlip, awd.SteeringDegrees, headingSweep));
                if (headingSweep >= 180 && (period < .5f || peakSlide >= 45 && rearPeakSeconds > .3f)) break;
            }
            bool passed = headingSweep >= 180 && (period < .5f || peakSlide >= 45 && rearPeakSeconds > .3f);
            if (!passed) failures++;
            Debug.Log($"AWD_TURNAROUND pass={passed} entry={entry} period={period} direction={direction} time={steps*.01f:F2}s headingSweep={headingSweep:F1} slide={peakSlide:F1} rearPeak={rearPeakSeconds:F2}s");
        }
        drivingSurface.Training = false; dynamics = new DrivingSettings(); RestartLap();
        if (failures > 0) throw new Exception($"W with alternating A/D failed to turn around in {failures} cases");
    }

    void CheckAwdEngine()
    {
        var engine = new SubaruEngine(awd.setup);
        // An open clutch must allow revving with stationary wheels, then return to idle on lift.
        for (int i = 0; i < 500; i++)
        {
            float drive = engine.DriveTorque(.01f, 1, 0, true);
            if (drive != 0) throw new Exception("Open clutch transmitted drive torque");
            engine.Integrate(.01f, drive);
        }
        float freeRpm = engine.Rpm;
        if (freeRpm < 6500 || freeRpm > awd.setup.revLimitRpm + 100)
            throw new Exception($"Free engine revving/limiter failed: {freeRpm:F0} RPM");
        for (int i = 0; i < 1500; i++)
            engine.Integrate(.01f, engine.DriveTorque(.01f, 0, 0, true));
        if (Mathf.Abs(engine.Rpm - awd.setup.idleRpm) > 50)
            throw new Exception($"Engine did not settle to idle on lift: {engine.Rpm:F0} RPM");

        foreach (float shaft in new[] {0f, 2000f, 5000f})
        {
            engine.Reset();
            float drive = 0;
            for (int i = 0; i < 500; i++)
            {
                drive = engine.DriveTorque(.01f, 1, shaft, false);
                engine.Integrate(.01f, drive);
            }
            if (engine.Rpm < 4400 || drive < 150)
                throw new Exception($"Loaded engine lost revs or torque at shaft {shaft}: RPM={engine.Rpm:F0} Nm={drive:F0}");
        }
        float beforeDrop = engine.Rpm;
        engine.Integrate(.01f, engine.DriveTorque(.01f, 1, 0, false));
        float drop = beforeDrop - engine.Rpm;
        if (drop < 0 || drop > 150)
            throw new Exception($"Sudden wheel deceleration snapped engine RPM: {drop:F0} RPM in 10 ms");
        for (int i = 0; i < 500; i++)
            engine.Integrate(.01f, engine.DriveTorque(.01f, 1, 0, false));
        if (engine.Rpm < 4400) throw new Exception("Held throttle stalled the engine with stationary wheels");
        Debug.Log($"AWD_ENGINE passed: freeRev={freeRpm:F0} suddenShaftStopRpmDrop={drop:F1} loadedStationaryRpm={engine.Rpm:F0}");
    }

    void CheckAwdSlidePower(string output)
    {
        SetAutopilot(false);
        drivingSurface.Training = true;
        using var telemetry = new StreamWriter(Path.Combine(output, "awd-slide-power.csv"));
        telemetry.WriteLine("entry_kph,traction_control,steering_mode,time_s,speed_kph,forward_kph,gear,rpm,sideslip_deg,wheel_rpm,drive_nm,steering_deg,clutch_nm");
        float minimumRpm = float.MaxValue, longestCut = 0;
        int downshifts = 0, cases = 0, earlyUpshifts = 0;
        foreach (bool tractionControl in new[] {true, false})
        foreach (int entrySpeed in new[] {0, 50, 100})
        foreach (bool reversal in new[] {false, true})
        {
            dynamics = new DrivingSettings {awdTractionControl = tractionControl};
            awd.ResetCar(DrivingSurface.TrainingCentre + new Vector3(0, .3f, -250), Quaternion.identity);
            for (int i = 0; i < 300; i++) { awd.Step(.01f, 0, 1, 0, dynamics); Physics.Simulate(.01f); }
            int acceleration = 0;
            while (awd.ForwardSpeed * 3.6f < entrySpeed && acceleration++ < 2000)
            { StepDriving(.01f, Time.time, 0, 1, 0, false); Physics.Simulate(.01f); }
            if (awd.ForwardSpeed * 3.6f < entrySpeed) throw new Exception("Drivetrain check failed to reach its entry speed");
            float lowRpm = float.MaxValue, cut = 0, maxCut = 0;
            int previousGear = awd.Gear, gearChanges = 0;
            for (int i = 0; i < 2000; i++)
            {
                float steering = reversal ? (i / 80 % 2 == 0 ? 1 : -1) : 1;
                float axleRpm = 0;
                foreach (var wheel in awd.Wheels) axleRpm += Mathf.Abs(wheel.rpm) * .25f;
                float coupledRpm = axleRpm * awd.setup.finalDrive * awd.setup.gears[previousGear - 1];
                StepDriving(.01f, Time.time, steering, 1, 0, false);
                Physics.Simulate(.01f); SyncAwdMotion();
                if (awd.Gear < previousGear) downshifts++;
                if (awd.Gear > previousGear && coupledRpm < 6200) earlyUpshifts++;
                if (awd.Gear != previousGear) gearChanges++;
                previousGear = awd.Gear;
                float torque = awd.FrontTorque + awd.RearTorque;
                if (i >= 100)
                {
                    lowRpm = Mathf.Min(lowRpm, awd.EngineRpm);
                    cut = torque < 1 ? cut + .01f : 0;
                    maxCut = Mathf.Max(maxCut, cut);
                }
                foreach (var wheel in awd.Wheels)
                    if (wheel.brakeTorque != 0) throw new Exception("Held accelerator applied wheel brakes during a slide");
                if (!drivingSurface.Sample(car.position.x, car.position.z).OnRoad)
                    throw new Exception("Drivetrain check left the training asphalt");
                if (i % 5 == 0) telemetry.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3:F2},{4:F2},{5:F2},{6},{7:F0},{8:F2},{9:F1},{10:F1},{11:F2},{12:F1}",
                    entrySpeed, tractionControl, reversal ? "reversal" : "circle", i * .01f,
                    velocity.magnitude * 3.6f, awd.ForwardSpeed * 3.6f, awd.Gear, awd.EngineRpm,
                    awd.SideslipDegrees, awd.Wheels[0].rpm, torque, awd.SteeringDegrees, awd.ClutchTorque));
            }
            minimumRpm = Mathf.Min(minimumRpm, lowRpm); longestCut = Mathf.Max(longestCut, maxCut); cases++;
            Debug.Log($"AWD_SLIDE_POWER entry={entrySpeed} TC={tractionControl} reversal={reversal} minRpm={lowRpm:F0} longestDriveCut={maxCut:F2}s gearChanges={gearChanges}");
            if (!reversal && entrySpeed == 50 && gearChanges > 4)
                throw new Exception("Gearbox repeatedly shifted during a sustained low-speed turn");
        }
        drivingSurface.Training = false; dynamics = new DrivingSettings(); RestartLap();
        if (minimumRpm < 3000 || longestCut > .25f || downshifts == 0 || earlyUpshifts != 0)
            throw new Exception($"Held throttle lost its power band: minRpm={minimumRpm:F0} longestDriveCut={longestCut:F2}s downshifts={downshifts} earlyUpshifts={earlyUpshifts}");
        Debug.Log($"AWD_SLIDE_POWER passed: cases={cases} minimumRpm={minimumRpm:F0} longestDriveCut={longestCut:F2}s downshifts={downshifts} earlyUpshifts={earlyUpshifts}");
    }
}
