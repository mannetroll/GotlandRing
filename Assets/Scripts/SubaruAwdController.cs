using UnityEngine;

// RingDrive supplies inputs; wheel forces and Unity's rigidbody solver move the car.
public sealed class SubaruAwdController : MonoBehaviour
{
    public SubaruVehicleSetup setup = new SubaruVehicleSetup();
    public Rigidbody Body { get; private set; }
    public WheelCollider[] Wheels { get; private set; }
    public int Gear { get; private set; } = 1;
    public int GroundedWheels { get; private set; }
    public float EngineRpm { get; private set; } = 900f;
    public float SteeringDegrees { get; private set; }
    public float ForwardSpeed => Vector3.Dot(Body.linearVelocity, transform.forward);
    public float Slip { get; private set; }
    public float FrontTorque => Wheels[0].motorTorque + Wheels[1].motorTorque;
    public float RearTorque => Wheels[2].motorTorque + Wheels[3].motorTorque;
    public float Boost { get; private set; }
    BoxCollider chassis;
    TrackData track;
    float appliedThrottle, shiftDelay;
    Vector3 heldVelocity, heldAngularVelocity;

    public void Initialize(TrackData road)
    {
        track = road;
        Body = gameObject.AddComponent<Rigidbody>();
        Body.mass = setup.Mass;
        Body.centerOfMass = new Vector3(0, setup.centreOfMassHeight, (setup.frontWeightFraction - .5f) * setup.wheelbase);
        Body.interpolation = RigidbodyInterpolation.Interpolate;
        Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Body.angularDamping = .12f;
        Body.maxAngularVelocity = 8f;
        chassis = gameObject.AddComponent<BoxCollider>();
        chassis.center = new Vector3(0, .68f, 0);
        chassis.size = new Vector3(1.60f, .70f, 4.10f);
        Body.ResetInertiaTensor();
        Wheels = new WheelCollider[4];
        for (int i = 0; i < 4; i++)
        {
            bool front = i < 2;
            var wheel = new GameObject(new[] {"AWD front left", "AWD front right", "AWD rear left", "AWD rear right"}[i]);
            wheel.transform.SetParent(transform, false);
            wheel.transform.localPosition = new Vector3((i % 2 == 0 ? -.5f : .5f) * setup.trackWidth,
                setup.wheelRadius + setup.suspensionTravel * .5f, (front ? .5f : -.5f) * setup.wheelbase);
            var w = Wheels[i] = wheel.AddComponent<WheelCollider>();
            w.radius = setup.wheelRadius; w.mass = 18; w.suspensionDistance = setup.suspensionTravel;
            w.suspensionSpring = new JointSpring { spring = front ? setup.frontSpring : setup.rearSpring,
                damper = front ? setup.frontDamper : setup.rearDamper, targetPosition = .5f };
            w.sprungMass = setup.Mass * (front ? setup.frontWeightFraction : 1 - setup.frontWeightFraction) * .5f;
            w.forceAppPointDistance = .15f; w.wheelDampingRate = .3f;
        }
        Wheels[0].ConfigureVehicleSubsteps(10, 5, 5);
    }

    public void SetSimulationActive(bool active)
    {
        if (active == !Body.isKinematic) return;
        if (!active) { heldVelocity = Body.linearVelocity; heldAngularVelocity = Body.angularVelocity; }
        foreach (var wheel in Wheels) wheel.enabled = active;
        chassis.enabled = active;
        Body.isKinematic = !active;
        if (active) { Body.linearVelocity = heldVelocity; Body.angularVelocity = heldAngularVelocity; Body.WakeUp(); }
    }

    public void ResetCar(Vector3 position, Quaternion rotation)
    {
        Body.position = position; Body.rotation = rotation;
        heldVelocity = heldAngularVelocity = Vector3.zero;
        if (!Body.isKinematic) { Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero; }
        Gear = 1; EngineRpm = setup.idleRpm;
        shiftDelay = appliedThrottle = SteeringDegrees = Boost = Slip = 0; GroundedWheels = 0;
        foreach (var wheel in Wheels)
        {
            wheel.motorTorque = 0; wheel.brakeTorque = 0; wheel.steerAngle = 0;
            wheel.enabled = false; wheel.enabled = !Body.isKinematic;
        }
        Physics.SyncTransforms();
    }

    public DrivingSettings PilotSettings(DrivingSettings settings) => new DrivingSettings {
        grip = 7.5f * settings.awdGrip, braking = 6.5f, acceleration = .70f,
        steering = setup.steeringLock, highSpeedSteering = setup.highSpeedSteering,
        response = settings.awdSteeringRate / setup.steeringLock, offRoadGrip = 3.5f
    };

    public void Step(float dt, float throttle, float brake, float steering, DrivingSettings settings)
    {
        float speed = Mathf.Abs(ForwardSpeed);
        SteeringDegrees = Mathf.MoveTowards(SteeringDegrees, Mathf.Clamp(steering, -1, 1) * setup.SteeringLimit(speed), settings.awdSteeringRate * dt);
        appliedThrottle = Mathf.MoveTowards(appliedThrottle, Mathf.Clamp(throttle, -1, 1), dt * 3f);
        bool reverse = appliedThrottle < 0;
        float ratio = (reverse ? setup.reverseRatio : setup.gears[Gear - 1]) * setup.finalDrive;
        float front = Mathf.Clamp01(settings.awdFrontTorque);
        float frontRpm = (Mathf.Abs(Wheels[0].rpm) + Mathf.Abs(Wheels[1].rpm)) * .5f;
        float rearRpm = (Mathf.Abs(Wheels[2].rpm) + Mathf.Abs(Wheels[3].rpm)) * .5f;
        float roadRpm = speed / setup.wheelRadius * 60f / (2 * Mathf.PI) * ratio;
        float wheelRpm = Mathf.Lerp(rearRpm, frontRpm, front) * ratio;
        // Approximate launch clutch; the gearbox uses automatic shifts of five manual ratios.
        float launchRpm = Mathf.Lerp(setup.idleRpm, 2400, Mathf.Abs(appliedThrottle));
        EngineRpm = Mathf.Max(wheelRpm, Mathf.Lerp(launchRpm, setup.idleRpm, Mathf.Clamp01(speed / 8)));
        shiftDelay = Mathf.Max(0, shiftDelay - dt);
        if (!reverse && appliedThrottle > 0 && shiftDelay == 0)
        {
            if (roadRpm > setup.shiftRpm && Gear < setup.gears.Length) { Gear++; shiftDelay = .22f; }
            else if (roadRpm < 2400 && Gear > 1) { Gear--; shiftDelay = .22f; }
        }
        Boost = Mathf.MoveTowards(Boost, Mathf.Abs(appliedThrottle) * Mathf.InverseLerp(1800, 3600, EngineRpm), dt * 1.5f);
        float torque = brake > 0 || shiftDelay > 0 || EngineRpm >= setup.revLimitRpm || (reverse && speed > 8)
            ? 0 : Mathf.Max(0, setup.torque.Evaluate(EngineRpm)) * ratio * setup.efficiency * appliedThrottle;
        GroundedWheels = 0; Slip = 0;
        for (int i = 0; i < 4; i++)
        {
            var wheel = Wheels[i]; bool frontAxle = i < 2;
            wheel.steerAngle = frontAxle ? SteeringDegrees : 0;
            float drive = torque * (frontAxle ? front : 1 - front) * .5f;
            float brakeTorque = brake * (frontAxle ? setup.frontBrakeTorque : setup.rearBrakeTorque);
            float grip = settings.awdGrip;
            if (wheel.GetGroundHit(out WheelHit hit))
            {
                GroundedWheels++;
                if (!track.Sample(hit.point.x, hit.point.z).OnRoad) grip *= .48f;
                float forwardSlip = Mathf.Abs(hit.forwardSlip);
                Slip = Mathf.Max(Slip, Mathf.Abs(hit.sidewaysSlip), forwardSlip);
                if (settings.awdTractionControl && forwardSlip > .22f) drive *= .22f / forwardSlip;
                if (speed > 3 && forwardSlip > .22f) brakeTorque *= .22f / forwardSlip;
            }
            wheel.forwardFriction = new WheelFrictionCurve { extremumSlip = .22f, extremumValue = 1,
                asymptoteSlip = .65f, asymptoteValue = .75f, stiffness = 1.05f * grip };
            wheel.sidewaysFriction = new WheelFrictionCurve { extremumSlip = .12f, extremumValue = 1,
                asymptoteSlip = .45f, asymptoteValue = .72f, stiffness = 1.05f * grip };
            wheel.motorTorque = drive; wheel.brakeTorque = brakeTorque;
        }
        AntiRoll(0, 1, setup.frontAntiRoll); AntiRoll(2, 3, setup.rearAntiRoll);
        Vector3 velocity = Body.linearVelocity;
        Body.AddForce(-velocity * velocity.magnitude * setup.dragCoefficient);
        if (GroundedWheels > 0)
            Body.AddForce(-transform.forward * Mathf.Clamp(ForwardSpeed, -1, 1) * setup.rollingResistance * setup.Mass * Physics.gravity.magnitude);
    }

    void AntiRoll(int left, int right, float stiffness)
    {
        var l = Wheels[left]; var r = Wheels[right];
        bool onLeft = l.GetGroundHit(out WheelHit lh), onRight = r.GetGroundHit(out WheelHit rh);
        float travelLeft = onLeft ? -l.transform.InverseTransformPoint(lh.point).y - l.radius : l.suspensionDistance;
        float travelRight = onRight ? -r.transform.InverseTransformPoint(rh.point).y - r.radius : r.suspensionDistance;
        float force = (travelLeft - travelRight) * stiffness;
        if (onLeft) Body.AddForceAtPosition(-l.transform.up * force, l.transform.position);
        if (onRight) Body.AddForceAtPosition(r.transform.up * force, r.transform.position);
    }
}
