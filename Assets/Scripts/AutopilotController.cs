using System.Collections.Generic;
using UnityEngine;

// Produces ordinary driving inputs. It never moves the car or bypasses its physics.
public sealed class AutopilotController
{
    const float Wheelbase = 2.52f;
    const float MaximumSpeed = 72f;
    const float EdgeClearance = 2.6f;
    readonly List<Vector3> points;
    readonly TrackData surface;
    readonly float[] segmentLength, speedPlan;
    DrivingSettings settings;
    public IReadOnlyList<Vector3> RacingLine => points;

    public struct Controls
    {
        public float Steering, Throttle, Brake, TargetSpeed, LineError, Curvature;
    }

    public AutopilotController(TrackData track, DrivingSettings dynamics)
    {
        surface = track;
        points = BuildRacingLine(track);
        segmentLength = new float[points.Count];
        speedPlan = new float[points.Count];
        for (int i = 0; i < points.Count; i++)
            segmentLength[i] = Flat(points[Next(i)] - points[i]).magnitude;
        Configure(dynamics);
    }

    int Next(int index) => (index + 1) % points.Count;
    static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

    static List<Vector3> BuildRacingLine(TrackData track)
    {
        int count = track.Points.Count;
        var offsets = new float[count];
        // Minimize squared changes in tangent within the usable asphalt. Coarse
        // passes place entries/apexes/exits together; finer passes smooth them.
        // The clearance includes car width and room for steering corrections.
        foreach (int stride in new[] { 32, 16, 8, 4, 2, 1 })
        {
            var indices = new List<int>();
            for (int i = 0; i < count; i += stride) indices.Add(i);
            int n = indices.Count;
            var path = new Vector3[n];
            var distance = new float[n];
            for (int i = 0; i < n; i++)
            {
                int index = indices[i], next = indices[(i + 1) % n];
                path[i] = Flat(track.Sections[index].Road(offsets[index]));
                distance[i] = next > index ? track.Sections[next].Distance - track.Sections[index].Distance
                    : track.HorizontalLength - track.Sections[index].Distance;
            }
            for (int pass = 0; pass < 300; pass++)
            {
                float largestMove = 0;
                for (int i = 0; i < n; i++)
                {
                    var direction = track.Sections[indices[i]].Right;
                    float gradient = 0, diagonal = 0;
                    for (int term = -1; term <= 1; term++)
                    {
                        int j = (i + n + term) % n, a = (j + n - 1) % n, b = (j + 1) % n;
                        float coefficient = term < 0 ? 1 / distance[j]
                            : term > 0 ? 1 / distance[a] : -1 / distance[a] - 1 / distance[j];
                        var bend = (path[b] - path[j]) / distance[j] - (path[j] - path[a]) / distance[a];
                        float weight = 2 / (distance[a] + distance[j]);
                        gradient += weight * coefficient * Vector3.Dot(bend, direction);
                        diagonal += weight * coefficient * coefficient;
                    }
                    int index = indices[i];
                    var row = track.Sections[index];
                    float offset = Mathf.Clamp(offsets[index] - gradient / diagonal,
                        -row.LeftWidth + EdgeClearance, row.RightWidth - EdgeClearance);
                    float move = offset - offsets[index];
                    offsets[index] = offset; path[i] += direction * move;
                    largestMove = Mathf.Max(largestMove, Mathf.Abs(move));
                }
                if (largestMove < .0001f) break;
            }
            // Interpolate the periodic solution onto the next, finer grid.
            for (int i = 0; i < n; i++)
            {
                int first = indices[i], last = i + 1 < n ? indices[i + 1] : count;
                for (int j = first + 1; j < last; j++)
                {
                    float fraction = (track.Sections[j].Distance - track.Sections[first].Distance) / distance[i];
                    var row = track.Sections[j];
                    offsets[j] = Mathf.Clamp(Mathf.Lerp(offsets[first], offsets[last % count], fraction),
                        -row.LeftWidth + EdgeClearance, row.RightWidth - EdgeClearance);
                }
            }
        }
        var result = new List<Vector3>(count);
        for (int i = 0; i < count; i++) result.Add(track.Sections[i].Road(offsets[i]));
        return result;
    }

    public void Configure(DrivingSettings dynamics)
    {
        settings = dynamics.Copy();
        var curvaturePlan = new float[points.Count];
        // Measure curvature over 12 m, suppressing centimetre-scale CSV rounding.
        // Reserve some lateral grip for correcting position/heading errors.
        for (int i = 0; i < points.Count; i++)
        {
            var a = Flat(points[i] - points[(i + points.Count - 2) % points.Count]);
            var b = Flat(points[(i + 2) % points.Count] - points[i]);
            curvaturePlan[i] = 2 * Vector3.Cross(a, b).y
                / Mathf.Max(.001f, a.magnitude * b.magnitude * (a + b).magnitude);
        }
        for (int i = 0; i < points.Count; i++)
        {
            float signedCurvature = curvaturePlan[i];
            float curvature = Mathf.Abs(signedCurvature);
            var normal = surface.Sample(points[i].x, points[i].z).Normal;
            float bank = TrackData.BankAcceleration(normal, surface.Sections[i].Right);
            float gripBudget = (settings.grip + Mathf.Sign(signedCurvature) * bank) * .88f;
            float limit = Mathf.Min(MaximumSpeed, Mathf.Sqrt(gripBudget / Mathf.Max(curvature, .00001f)));
            // Respect both steering angle and how quickly the configured
            // actuator can change direction through a corner transition.
            int previous = (i + points.Count - 2) % points.Count, next = (i + 2) % points.Count;
            float angleChange = Mathf.Abs(Mathf.Atan(Wheelbase * curvaturePlan[next])
                - Mathf.Atan(Wheelbase * curvaturePlan[previous])) * Mathf.Rad2Deg;
            float distance = segmentLength[previous] + segmentLength[Next(previous)] + segmentLength[i] + segmentLength[Next(i)];
            while (limit > 4 && (Mathf.Atan(Wheelbase * curvature) * Mathf.Rad2Deg > settings.SteeringLimit(limit) * .9f
                || angleChange * limit / (distance * settings.SteeringLimit(limit)) > settings.response * .65f))
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
        // Project onto the racing line near the current road section. Its
        // diagonal approaches do not share the centreline's nearest segment.
        int nearest = road.Segment;
        float fraction = 0, lineDistance = float.MaxValue;
        for (int delta = -4; delta <= 4; delta++)
        {
            int index = (road.Segment + points.Count + delta) % points.Count;
            var segment = Flat(points[Next(index)] - points[index]);
            float along = Mathf.Clamp01(Vector3.Dot(Flat(position - points[index]), segment) / segment.sqrMagnitude);
            float distance = Flat(position - Vector3.Lerp(points[index], points[Next(index)], along)).sqrMagnitude;
            if (distance < lineDistance) { lineDistance = distance; nearest = index; fraction = along; }
        }
        float speed = velocity.magnitude;
        var forward = new Vector3(Mathf.Sin(yawDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(yawDegrees * Mathf.Deg2Rad));
        // Physical tyre slip separates body heading from the direction of travel.
        if (settings.awdMode && Vector3.Dot(velocity, forward) > 8) forward = Flat(velocity).normalized;
        // Continuous target interpolation avoids steering jumps between CSV vertices.
        float lookAhead = Mathf.Clamp(4 + speed * .28f + speed * .06f / settings.response, 6, 26);
        if (settings.awdMode) lookAhead = Mathf.Clamp(6 + speed * .45f, 10, 32);
        var aim = Flat(PointAhead(nearest, fraction, lookAhead) - position);
        float angle = Vector3.SignedAngle(forward, aim, Vector3.up) * Mathf.Deg2Rad;
        float curvature = 2 * Mathf.Sin(angle) / Mathf.Max(aim.magnitude, 1);
        float steering = Mathf.Atan(Wheelbase * curvature) * Mathf.Rad2Deg / settings.SteeringLimit(speed);
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
        float lineError = Mathf.Sqrt(lineDistance);
        // Correct an AWD line error progressively so braking does not unload the rear abruptly.
        if (settings.awdMode) target *= Mathf.Lerp(1, .45f, Mathf.InverseLerp(1.5f, 8, lineError));
        else if (lineError > 2) target = Mathf.Min(target, Mathf.Lerp(18, 6, Mathf.InverseLerp(2, 9, lineError)));
        if (!road.OnRoad) target = Mathf.Min(target, Mathf.Sqrt(settings.offRoadGrip / Mathf.Max(Mathf.Abs(curvature), .01f)) * .8f);

        // Feed forward the deceleration along the planned envelope, then correct speed.
        float plannedAcceleration = (speedPlan[Next(nearest)] * speedPlan[Next(nearest)] - speedPlan[nearest] * speedPlan[nearest])
            / (2 * segmentLength[nearest]);
        float desiredAcceleration = (target - speed) * 3 + Mathf.Min(0, plannedAcceleration);
        float drag = .10f + speed * speed * .0012f + (road.OnRoad ? 0 : 2.5f);
        float brake = Mathf.Clamp01((-desiredAcceleration - drag) / settings.braking);
        float throttle = brake > .001f ? 0 : Mathf.Clamp01((desiredAcceleration + drag) / (settings.acceleration * 5.9f));
        if (Vector3.Dot(velocity, forward) < -.3f) { brake = 1; throttle = 0; }
        return new Controls { Steering = Mathf.Clamp(steering, -1, 1), Throttle = throttle, Brake = brake, TargetSpeed = target, LineError = lineError, Curvature = curvature };
    }
}
