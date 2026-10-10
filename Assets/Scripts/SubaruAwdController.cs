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
    public float SideslipDegrees => Mathf.Atan2(Vector3.Dot(Body.linearVelocity, transform.right),
        Mathf.Max(1, Mathf.Abs(ForwardSpeed))) * Mathf.Rad2Deg;
    public float FrontLateralSlip { get; private set; }
    public float RearLateralSlip { get; private set; }
    public float FrontTorque => Wheels[0].motorTorque + Wheels[1].motorTorque;
    public float RearTorque => Wheels[2].motorTorque + Wheels[3].motorTorque;
    public float Boost { get; private set; }
    BoxCollider chassis;
    DrivingSurface surface;
    float appliedThrottle, shiftDelay;
    Vector3 heldVelocity, heldAngularVelocity;

    public void Initialize(DrivingSurface road)
    {
        surface = road;
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
        // Arcade moves the transform directly; physics interpolation must not replay an older pose.
        Body.interpolation = active && Physics.simulationMode != SimulationMode.Script
            ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        Body.isKinematic = !active;
        if (active) { Body.linearVelocity = heldVelocity; Body.angularVelocity = heldAngularVelocity; Body.WakeUp(); }
    }

    public void ResetCar(Vector3 position, Quaternion rotation)
    {
        // Update the visible pose immediately, including a teleport while physics is paused.
        transform.SetPositionAndRotation(position, rotation);
        Body.position = position; Body.rotation = rotation;
        heldVelocity = heldAngularVelocity = Vector3.zero;
        if (!Body.isKinematic) { Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero; }
        Gear = 1; EngineRpm = setup.idleRpm;
        shiftDelay = appliedThrottle = SteeringDegrees = Boost = FrontLateralSlip = RearLateralSlip = 0; GroundedWheels = 0;
        foreach (var wheel in Wheels)
        {
            wheel.motorTorque = 0; wheel.brakeTorque = 0; wheel.steerAngle = 0;
            wheel.enabled = false; wheel.enabled = !Body.isKinematic;
        }
        Physics.SyncTransforms();
    }

    public DrivingSettings PilotSettings(DrivingSettings settings) => new DrivingSettings {
        grip = 8.8f * settings.awdGrip, braking = 6.5f, acceleration = .70f,
        steering = settings.steering, highSpeedSteering = settings.highSpeedSteering,
        response = settings.awdSteeringRate / settings.steering, offRoadGrip = 3.5f
    };

    public AutopilotController.Controls CorrectPilot(AutopilotController.Controls controls, DrivingSettings settings)
    {
        if (ForwardSpeed < 8) return controls;
        // The path controller aims along velocity; steer relative to the body and damp excess rotation.
        float slip = SideslipDegrees;
        float yawError = controls.Curvature * ForwardSpeed - Vector3.Dot(Body.angularVelocity, transform.up);
        // Allow rotation into the corner, then strengthen countersteering as the slide grows.
        float recovery = Mathf.InverseLerp(8, 10, Mathf.Abs(slip));
        float correction = slip * Mathf.Lerp(1.05f, 1.2f, recovery)
            + yawError * Mathf.Rad2Deg * .3f;
        controls.Steering = Mathf.Clamp(controls.Steering + correction / settings.SteeringLimit(Body.linearVelocity.magnitude), -1, 1);
        controls.Throttle *= 1 - .8f * Mathf.InverseLerp(7, 12, Mathf.Abs(slip));
        // Releasing some brake load lets the rear tyres recover during corner entry.
        controls.Brake *= 1 - .8f * Mathf.InverseLerp(6, 10, Mathf.Abs(slip));
        return controls;
    }

    public void Step(float dt, float throttle, float brake, float steering, DrivingSettings settings)
    {
        float speed = Mathf.Abs(ForwardSpeed);
        SteeringDegrees = Mathf.MoveTowards(SteeringDegrees, Mathf.Clamp(steering, -1, 1) * settings.SteeringLimit(Body.linearVelocity.magnitude), settings.awdSteeringRate * dt);
        appliedThrottle = Mathf.MoveTowards(appliedThrottle, Mathf.Clamp(throttle, -1, 1), dt * 3f);
        bool reverse = appliedThrottle < 0;
        float roadWheelRpm = speed / setup.wheelRadius * 60f / (2 * Mathf.PI);
        shiftDelay = Mathf.Max(0, shiftDelay - dt);
        if (!reverse && shiftDelay == 0)
        {
            int selected = Gear;
            float roadRpm = roadWheelRpm * setup.finalDrive * setup.gears[selected - 1];
            if (roadRpm > setup.shiftRpm && selected < setup.gears.Length) selected++;
            else
            {
                // Ask for the power band under load, including a multi-gear downshift on corner exit.
                float downshiftRpm = Mathf.Lerp(2400, 4200, brake > 0 ? 0 : Mathf.InverseLerp(.25f, .85f, throttle));
                while (selected > 1 && roadRpm < downshiftRpm)
                {
                    float lowerRpm = roadWheelRpm * setup.finalDrive * setup.gears[selected - 2];
                    // Leave room below the upshift point so the gearbox does not hunt between ratios.
                    if (lowerRpm > setup.shiftRpm - 400) break;
                    selected--; roadRpm = lowerRpm;
                }
            }
            if (selected != Gear) { Gear = selected; shiftDelay = .22f; }
        }
        float ratio = (reverse ? setup.reverseRatio : setup.gears[Gear - 1]) * setup.finalDrive;
        float front = Mathf.Clamp01(settings.awdFrontTorque);
        float frontRpm = (Mathf.Abs(Wheels[0].rpm) + Mathf.Abs(Wheels[1].rpm)) * .5f;
        float rearRpm = (Mathf.Abs(Wheels[2].rpm) + Mathf.Abs(Wheels[3].rpm)) * .5f;
        float wheelRpm = Mathf.Lerp(rearRpm, frontRpm, front) * ratio;
        // Approximate launch clutch; the gearbox uses automatic shifts of five manual ratios.
        float launchRpm = Mathf.Lerp(setup.idleRpm, 2400, Mathf.Abs(appliedThrottle));
        EngineRpm = Mathf.Max(wheelRpm, Mathf.Lerp(launchRpm, setup.idleRpm, Mathf.Clamp01(speed / 8)));
        Boost = Mathf.MoveTowards(Boost, Mathf.Abs(appliedThrottle) * Mathf.InverseLerp(1800, 3600, EngineRpm), dt * 1.5f);
        float torque = brake > 0 || shiftDelay > 0 || EngineRpm >= setup.revLimitRpm || (reverse && speed > 8)
            ? 0 : Mathf.Max(0, setup.torque.Evaluate(EngineRpm)) * ratio * setup.efficiency * appliedThrottle;
        GroundedWheels = 0; FrontLateralSlip = RearLateralSlip = 0;
        for (int i = 0; i < 4; i++)
        {
            var wheel = Wheels[i]; bool frontAxle = i < 2;
            wheel.steerAngle = frontAxle ? SteeringDegrees : 0;
            float drive = torque * (frontAxle ? front : 1 - front) * .5f;
            float brakeTorque = brake * (frontAxle ? setup.frontBrakeTorque : setup.rearBrakeTorque);
            float grip = settings.awdGrip;
            float lateralBudget = 1;
            if (wheel.GetGroundHit(out WheelHit hit))
            {
                GroundedWheels++;
                bool asphalt = surface.Sample(hit.point.x, hit.point.z).OnRoad;
                if (!asphalt) grip *= .48f;
                float forwardSlip = Mathf.Abs(hit.forwardSlip);
                if (frontAxle) FrontLateralSlip = Mathf.Max(FrontLateralSlip, Mathf.Abs(hit.sidewaysSlip));
                else RearLateralSlip = Mathf.Max(RearLateralSlip, Mathf.Abs(hit.sidewaysSlip));
                if (settings.awdTractionControl && forwardSlip > .22f) drive *= .22f / forwardSlip;
                if (speed > 3 && forwardSlip > .22f) brakeTorque *= .22f / forwardSlip;
                // Longitudinal demand uses some of the same grip as cornering.
                float longitudinalUse = Mathf.Clamp((Mathf.Abs(drive) + brakeTorque)
                    / (Mathf.Max(500, hit.force) * setup.wheelRadius * 1.05f * grip), 0, setup.maximumLongitudinalGripUse);
                lateralBudget = Mathf.Sqrt(1 - longitudinalUse * longitudinalUse);
            }
            wheel.forwardFriction = new WheelFrictionCurve { extremumSlip = .22f, extremumValue = 1,
                asymptoteSlip = .65f, asymptoteValue = .75f, stiffness = 1.05f * grip };
            // Progressive lateral force with a modest rear reserve; drive demand can still break rear grip.
            wheel.sidewaysFriction = new WheelFrictionCurve { extremumSlip = setup.lateralPeakSlip, extremumValue = 1,
                asymptoteSlip = setup.lateralPeakSlip * 3f, asymptoteValue = setup.slidingGripFraction,
                stiffness = (frontAxle ? setup.frontLateralStiffness : setup.rearLateralStiffness) * grip * lateralBudget };
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
