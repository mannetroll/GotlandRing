using System.Collections.Generic;
using UnityEngine;

// Produces ordinary driving inputs. It never moves the car or bypasses its physics.
public sealed class AutopilotController
{
    const float Wheelbase = 2.52f;
    const float MaximumSpeed = 72f;
    readonly List<Vector3> points;
    readonly TrackData surface;
    readonly float[] segmentLength, speedPlan;
    DrivingSettings settings;

    public struct Controls
    {
        public float Steering, Throttle, Brake, TargetSpeed;
    }

    public AutopilotController(TrackData track, DrivingSettings dynamics)
    {
        surface = track;
        points = track.Points;
        segmentLength = new float[points.Count];
        speedPlan = new float[points.Count];
        for (int i = 0; i < points.Count; i++)
            segmentLength[i] = Flat(points[Next(i)] - points[i]).magnitude;
        Configure(dynamics);
    }

    int Next(int index) => (index + 1) % points.Count;
    static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

    public void Configure(DrivingSettings dynamics)
    {
        settings = dynamics.Copy();
        // Measure curvature over 12 m, suppressing centimetre-scale CSV rounding.
        // Reserve some lateral grip for correcting position/heading errors.
        for (int i = 0; i < points.Count; i++)
        {
            var a = Flat(points[i] - points[(i + points.Count - 2) % points.Count]);
            var b = Flat(points[(i + 2) % points.Count] - points[i]);
            float signedCurvature = 2 * Vector3.Cross(a, b).y
                / Mathf.Max(.001f, a.magnitude * b.magnitude * (a + b).magnitude);
            float curvature = Mathf.Abs(signedCurvature);
            var normal = surface.Sample(points[i].x, points[i].z).Normal;
            float bank = TrackData.BankAcceleration(normal, surface.Sections[i].Right);
            float gripBudget = (settings.grip + Mathf.Sign(signedCurvature) * bank) * .88f;
            float limit = Mathf.Min(MaximumSpeed, Mathf.Sqrt(gripBudget / Mathf.Max(curvature, .00001f)));
            // A low high-speed steering setting can constrain a corner before grip does.
            while (limit > 4 && Mathf.Atan(Wheelbase * curvature) * Mathf.Rad2Deg > SteeringDegrees(limit) * .9f)
                limit -= .5f;
            speedPlan[i] = limit;
        }
        // Backward propagation includes the start/finish seam. Two circuits propagate
        // every downstream corner's braking constraint to every possible approach.
        for (int pass = 0; pass < 2; pass++)
            for (int i = points.Count - 1; i >= 0; i--)
                speedPlan[i] = Mathf.Min(speedPlan[i], Mathf.Sqrt(speedPlan[Next(i)] * speedPlan[Next(i)]
                    + 2 * settings.braking * .88f * segmentLength[i]));
    }

    float SteeringDegrees(float speed) => Mathf.Lerp(settings.steering, settings.highSpeedSteering, Mathf.Clamp01(speed / 65));

    Vector3 PointAhead(int segment, float fraction, float distance)
    {
        distance += fraction * segmentLength[segment];
        for (int count = 0; count < points.Count && distance > segmentLength[segment]; count++)
        {
            distance -= segmentLength[segment];
            segment = Next(segment);
        }
        return Vector3.Lerp(points[segment], points[Next(segment)], distance / segmentLength[segment]);
    }

    public Controls Drive(Vector3 position, float yawDegrees, Vector3 velocity, TrackData.SurfaceSample road)
    {
        int nearest = road.Segment;
        float speed = velocity.magnitude;
        var forward = new Vector3(Mathf.Sin(yawDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(yawDegrees * Mathf.Deg2Rad));
        var segment = Flat(points[Next(nearest)] - points[nearest]);
        float fraction = Mathf.Clamp01(Vector3.Dot(Flat(position - points[nearest]), segment) / segment.sqrMagnitude);
        // Continuous target interpolation avoids steering jumps between CSV vertices.
        float lookAhead = Mathf.Clamp(5 + speed * .34f + speed * .12f / settings.response, 6, 32);
        var aim = Flat(PointAhead(nearest, fraction, lookAhead) - position);
        float angle = Vector3.SignedAngle(forward, aim, Vector3.up) * Mathf.Deg2Rad;
        float curvature = 2 * Mathf.Sin(angle) / Mathf.Max(aim.magnitude, 1);
        float steering = Mathf.Atan(Wheelbase * curvature) * Mathf.Rad2Deg / SteeringDegrees(speed);
        // When facing away from the track, commit to a turn instead of stalling at sin(pi).
        if (Mathf.Abs(angle) > Mathf.PI * .5f) steering = Mathf.Sign(angle);

        float target = Mathf.Lerp(speedPlan[nearest], speedPlan[Next(nearest)], fraction);
        // Preview a short distance to cover control/steering response at corner entry.
        float preview = speed * (.12f + .12f / settings.response);
        int p = nearest;
        float remaining = (1 - fraction) * segmentLength[p];
        for (int count = 0; count < points.Count && remaining < preview; count++)
        {
            p = Next(p);
            target = Mathf.Min(target, speedPlan[p]);
            remaining += segmentLength[p];
        }
        float headingError = Mathf.Abs(angle) * Mathf.Rad2Deg;
        if (headingError > 50) target = Mathf.Min(target, Mathf.Lerp(12, 4, Mathf.InverseLerp(50, 140, headingError)));
        float edgeFraction = Mathf.Abs(road.Offset) / road.Width;
        if (edgeFraction > .7f) target = Mathf.Min(target, Mathf.Lerp(18, 6, Mathf.InverseLerp(.7f, 1.6f, edgeFraction)));
        if (!road.OnRoad) target = Mathf.Min(target, Mathf.Sqrt(settings.offRoadGrip / Mathf.Max(Mathf.Abs(curvature), .01f)) * .8f);

        // Feed forward the deceleration along the planned envelope, then correct speed.
        float plannedAcceleration = (speedPlan[Next(nearest)] * speedPlan[Next(nearest)] - speedPlan[nearest] * speedPlan[nearest])
            / (2 * segmentLength[nearest]);
        float desiredAcceleration = (target - speed) * 3 + Mathf.Min(0, plannedAcceleration);
        float drag = .10f + speed * speed * .0012f + (road.OnRoad ? 0 : 2.5f);
        float brake = Mathf.Clamp01((-desiredAcceleration - drag) / settings.braking);
        float throttle = brake > .001f ? 0 : Mathf.Clamp01((desiredAcceleration + drag) / (settings.acceleration * 5.9f));
        if (Vector3.Dot(velocity, forward) < -.3f) { brake = 1; throttle = 0; }
        return new Controls { Steering = Mathf.Clamp(steering, -1, 1), Throttle = throttle, Brake = brake, TargetSpeed = target };
    }
}
