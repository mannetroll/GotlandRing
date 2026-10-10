using UnityEngine;

public partial class RingDrive
{
    SubaruAwdController awd;
    bool awdTest;

    void MakeAwdCar()
    {
        awd = car.gameObject.AddComponent<SubaruAwdController>();
        awd.Initialize(centerline);
        awd.SetSimulationActive(dynamics.awdMode);
    }

    void ConfigureAutopilot() => autopilot.Configure(dynamics.awdMode ? awd.PilotSettings(dynamics) : dynamics);

    void SyncAwdMotion()
    {
        velocity = awd.Body.linearVelocity;
        yaw = car.eulerAngles.y;
        rpm = awd.EngineRpm; gear = awd.Gear; boost = awd.Boost;
        slip = Mathf.Clamp01(awd.Slip);
        steer = awd.SteeringDegrees / awd.setup.SteeringLimit(velocity.magnitude);
    }

    void StepAwdDriving(float dt, float steeringInput, float brake)
    {
        float forward = awd.ForwardSpeed;
        if (!autopilotEnabled && !automatic)
        {
            // Binary keyboard input gets a speed-dependent steering demand; tyre forces still decide the turn.
            float keyboardLock = Mathf.Atan(awd.setup.wheelbase * 12f / (forward * forward + 1f)) * Mathf.Rad2Deg;
            steeringInput *= Mathf.Clamp01(keyboardLock / awd.setup.SteeringLimit(forward));
        }
        bool reverseRequested = !automatic && !autopilotEnabled && Input.GetKey(KeyCode.X);
        float drive = throttle;
        if (reverseRequested)
        {
            if (forward > .3f) { brake = 1; drive = 0; }
            else { drive = -1; reversing = true; }
        }
        else if (drive > 0 && forward < -.3f) { brake = 1; drive = 0; }
        else if (forward >= -.3f) reversing = false;
        awd.Step(dt, drive, brake, steeringInput, dynamics);
        throttle = Mathf.Abs(drive);
    }

    void ApplyDrivingSettings(DrivingSettings settings)
    {
        bool changedMode = dynamics.awdMode != settings.awdMode;
        dynamics = settings;
        awd.SetSimulationActive(dynamics.awdMode && !paused);
        ConfigureAutopilot();
        if (changedMode) { lap = 1; best = 0; RestartLap(); }
    }

    void ToggleHandlingMode()
    {
        var settings = dynamics.Copy(); settings.awdMode = !settings.awdMode;
        ApplyDrivingSettings(settings); dynamics.Save();
        Debug.Log("HANDLING_MODE " + (dynamics.awdMode ? "AWD" : "ARCADE"));
    }

    void AnimateCar(float dt)
    {
        float angle = dynamics.awdMode ? awd.SteeringDegrees
            : steer * Mathf.Lerp(dynamics.steering, dynamics.highSpeedSteering, Mathf.Clamp01(velocity.magnitude / 65));
        carModel.Animate(steer, angle, Vector3.Dot(velocity, car.forward), dt, view == 0);
        if (dynamics.awdMode) carModel.AnimateSuspension(awd.Wheels, dt);
        else carModel.ResetSuspension();
    }
}
