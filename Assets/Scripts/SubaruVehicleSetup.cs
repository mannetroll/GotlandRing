using UnityEngine;

// Factory figures and estimated parameters are distinguished in docs/AWD-PHYSICS.md.
[System.Serializable]
public sealed class SubaruVehicleSetup
{
    public float kerbMass = 1285f, driverMass = 75f;
    public float wheelbase = 2.52f, trackWidth = 1.46f, wheelRadius = .3057f;
    public float frontWeightFraction = .58f, centreOfMassHeight = .48f;
    public float suspensionTravel = .20f, frontSpring = 34000f, rearSpring = 29000f;
    public float frontDamper = 3800f, rearDamper = 3200f;
    public float frontAntiRoll = 4500f, rearAntiRoll = 3200f;
    public float frontBrakeTorque = 1500f, rearBrakeTorque = 850f;
    public float[] gears = {3.454f, 1.947f, 1.366f, .972f, .738f};
    public float finalDrive = 4.111f, reverseRatio = 3.333f, efficiency = .88f;
    public float idleRpm = 900f, revLimitRpm = 6800f, shiftRpm = 6300f;
    public float frontLateralPeakSlip = .16f, rearLateralPeakSlip = .14f;
    public float slidingGripFraction = .90f;
    public float dragCoefficient = .42f, rollingResistance = .012f;
    // Intermediate samples estimated; anchors: 290 Nm/4000 RPM and 160 kW/5600 RPM.
    public AnimationCurve torque = new AnimationCurve(
        new Keyframe(900, 105), new Keyframe(2000, 175), new Keyframe(3000, 250),
        new Keyframe(4000, 290), new Keyframe(5000, 280), new Keyframe(5600, 272.837f),
        new Keyframe(6200, 230), new Keyframe(6800, 175));
    public float Mass => kerbMass + driverMass;
}
