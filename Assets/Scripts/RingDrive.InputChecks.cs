using System;
using UnityEngine;

public partial class RingDrive
{
    void CheckMouseSteering()
    {
        SetAutopilot(false); RecoverCar(0);
        ToggleMouseSteering();
        UpdatePointer(new Vector2(500, 500), false);
        if (mouseSteer != 0) throw new Exception("Cursor capture moved steering");
        UpdatePointer(new Vector2(10, 500), false);
        StepDriving(.02f, Time.time, -1, 0, 0, false);
        float applied = dynamics.awdMode ? awd.SteeringDegrees : steer;
        if (applied <= 0 || throttle != 0 || lookYaw != 0 || lookPitch != 0)
            throw new Exception("Mouse steering mixed with keyboard steering, pedals or camera");
        float held = mouseSteer;
        UpdatePointer(new Vector2(0, -900), false);
        StepDriving(.02f, Time.time, 1, 0, 0, false);
        if (mouseSteer != held || throttle != 0 || lookPitch != 0)
            throw new Exception("Vertical mouse motion changed driving or view");
        if (dynamics.awdMode && (awd.FrontTorque != 0 || awd.RearTorque != 0 || awd.Wheels[0].brakeTorque != 0))
            throw new Exception("Mouse-only steering applied an AWD pedal");
        StepDriving(.1f, Time.time, 0, 1, 0, false);
        if (throttle != 1 || (dynamics.awdMode && awd.FrontTorque <= 0))
            throw new Exception("Keyboard accelerator stopped working with mouse steering");
        StepDriving(.1f, Time.time, 0, 0, 1, false);
        if (dynamics.awdMode && (awd.FrontTorque != 0 || awd.Wheels[0].brakeTorque <= 0))
            throw new Exception("Keyboard brake stopped working with mouse steering");
        UpdatePointer(new Vector2(500, 0), false);
        UpdatePointer(new Vector2(-1, 0), false);
        if (mouseSteer >= 1 || mouseSteer <= 0) throw new Exception("Mouse steering accumulated travel beyond its lock");
        UpdatePointer(Vector2.one, true);
        if (mouseSteer != 0) throw new Exception("Right-click did not centre mouse steering");
        SetPaused(true);
        UpdatePointer(new Vector2(500, 500), false);
        if (mouseSteer != 0) throw new Exception("Pause accepted mouse steering");
        SetPaused(false);
        UpdatePointer(new Vector2(500, 0), false);
        if (mouseSteer != 0) throw new Exception("Cursor recapture turned the car after pause");
        RecoverCar(0); SetAutopilot(true);
        ToggleMouseSteering(); ToggleMouseSteering();
        UpdatePointer(Vector2.zero, false);
        UpdatePointer(new Vector2(500, 500), false);
        StepDriving(.02f, Time.time, -1, 0, 1, true);
        if (!autopilotEnabled || mouseSteer != 0 || reversing || throttle <= .5f || pilotControls.Brake != 0)
            throw new Exception("Mouse toggle or manual inputs overrode autopilot");
        SetAutopilot(false); ToggleMouseSteering();
        UpdatePointer(Vector2.zero, false);
        UpdatePointer(new Vector2(2, 3), false);
        StepDriving(.5f, Time.time, -1, 0, 0, false);
        applied = dynamics.awdMode ? awd.SteeringDegrees : steer;
        if (applied >= 0 || lookYaw == 0 || lookPitch == 0)
            throw new Exception("Keyboard steering and mouse look did not resume");
        RecoverCar(0);
        Debug.Log($"MOUSE_STEERING_TEST mode={(dynamics.awdMode ? "AWD" : "ARCADE")} pass=True horizontal steering, independent pedals, camera routing, centring, pause and autopilot priority");
    }
}
