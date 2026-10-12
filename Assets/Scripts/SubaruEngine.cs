using UnityEngine;

// Flywheel rotation is integrated from combustion and clutch reaction torque.
public sealed class SubaruEngine
{
    readonly SubaruVehicleSetup setup;
    const float RadiansPerRpm = 2 * Mathf.PI / 60;
    float angularSpeed, netTorque;
    public float Rpm => angularSpeed / RadiansPerRpm;

    public SubaruEngine(SubaruVehicleSetup vehicleSetup) { setup = vehicleSetup; Reset(); }
    public void Reset() { angularSpeed = setup.idleRpm * RadiansPerRpm; netTorque = 0; }

    public float DriveTorque(float dt, float pedal, float shaftRpm, bool clutchOpen)
    {
        float drag = setup.engineDrag * Mathf.Max(1, Rpm / 4000);
        float limiter = Mathf.Clamp01((setup.revLimitRpm - Rpm) / 150);
        float combustion = (Mathf.Max(0, setup.torque.Evaluate(Rpm)) + drag) * pedal * limiter;
        float idle = Mathf.Clamp((setup.idleRpm - Rpm) * .4f + drag, 0, 100) * (1 - pedal);
        netTorque = combustion + idle - drag;
        if (clutchOpen) return 0;

        // The automatic clutch reserves enough engine torque to reach the demanded launch/slide revs.
        float target = Mathf.Lerp(setup.idleRpm, setup.clutchTargetRpm, pedal);
        float capacity = Mathf.Clamp(Mathf.Max(netTorque * .15f,
            netTorque + (Rpm - target) * .16f), 0, setup.clutchCapacity);
        // Implicit coupling remains stable when the wheels abruptly slow or change gearbox ratio.
        float slip = angularSpeed - shaftRpm * RadiansPerRpm;
        return Mathf.Clamp((slip + netTorque * dt / setup.engineInertia)
            / (dt / setup.engineInertia + 1 / setup.clutchCoupling), 0, capacity);
    }

    public void Integrate(float dt, float clutchReactionTorque)
    {
        angularSpeed = Mathf.Max(0, angularSpeed + (netTorque - clutchReactionTorque) * dt / setup.engineInertia);
    }
}
