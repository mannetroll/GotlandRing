using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public partial class RingDrive
{
    void CheckAwdManualStability(string output)
    {
        MakeTrainingArea();
        var surface = new DrivingSurface(centerline) { Training = true };
        var vehicle = new GameObject("AWD stability test vehicle");
        var test = vehicle.AddComponent<SubaruAwdController>();
        test.Initialize(surface); test.Body.interpolation = RigidbodyInterpolation.None;
        var settings = new DrivingSettings();
        void Settle()
        {
            test.ResetCar(DrivingSurface.TrainingCentre + new Vector3(0, .3f, -200), Quaternion.identity);
            for (int i = 0; i < 300; i++) { test.Step(.01f, 0, 1, 0, settings); Physics.Simulate(.01f); }
        }
        try
        {
            using (var curve = new StreamWriter(Path.Combine(output, "awd-grip-curve.csv")))
            {
                curve.WriteLine("slip_angle_deg,lateral_acceleration_g,front_slip,rear_slip");
                test.Body.constraints = RigidbodyConstraints.FreezeRotation;
                float previousForce = 0;
                foreach (float angle in new[] {1f, 2, 4, 6, 8, 10, 12, 16})
                {
                    Settle();
                    var motion = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad)) * 30;
                    float acceleration = 0;
                    for (int i = 0; i < 60; i++)
                    {
                        test.Body.linearVelocity = motion;
                        test.Step(.01f, 0, 0, 0, settings); Physics.Simulate(.01f);
                        if (i >= 30) acceleration += (motion.x - test.Body.linearVelocity.x) / (.01f * 9.81f * 30);
                    }
                    curve.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:F4},{2:F4},{3:F4}",
                        angle, acceleration, test.FrontLateralSlip, test.RearLateralSlip));
                    if (!float.IsFinite(acceleration) || (angle <= 12 ? acceleration <= previousForce : acceleration < previousForce * .97f))
                        throw new Exception("Tyre force must build progressively, then stay near its peak through a slide");
                    previousForce = acceleration;
                }
                test.Body.constraints = RigidbodyConstraints.None;
            }
            using (var telemetry = new StreamWriter(Path.Combine(output, "awd-steering-recovery.csv")))
            {
                float worstSlip = 0, worstYaw = 0;
                telemetry.WriteLine("speed_kph,manoeuvre,pedal,direction,time_s,steering_deg,sideslip_deg,yaw_rate_deg_s,actual_speed_kph");
                foreach (int speed in new[] {60, 100, 140})
                foreach (bool reversal in new[] {false, true})
                foreach (float throttle in new[] {0f, 1f})
                foreach (float direction in new[] {-1f, 1f})
                {
                    Settle(); test.Body.linearVelocity = Vector3.forward * (speed / 3.6f);
                    float peakSlip = 0, tailSlip = 0, tailYaw = 0;
                    for (int i = 0; i < 450; i++)
                    {
                        // Full-lock key pulses and partial-lock mouse reversals, followed by centring.
                        float demand = direction * (reversal ? .35f : 1);
                        float steering = i < 30 ? 0 : i < 60 ? demand : reversal && i < 120 ? -demand : 0;
                        test.Step(.01f, throttle, 0, steering, settings); Physics.Simulate(.01f);
                        foreach (var wheel in test.Wheels)
                            if (!wheel.GetGroundHit(out var hit) || !surface.Sample(hit.point.x, hit.point.z).OnRoad)
                                throw new Exception("Steering stability check left the level asphalt test surface");
                        float beta = Mathf.Abs(test.SideslipDegrees);
                        float yawRate = Vector3.Dot(test.Body.angularVelocity, vehicle.transform.up) * Mathf.Rad2Deg;
                        peakSlip = Mathf.Max(peakSlip, beta);
                        if (i >= 300) { tailSlip = Mathf.Max(tailSlip, beta); tailYaw = Mathf.Max(tailYaw, Mathf.Abs(yawRate)); }
                        if (i % 5 == 0) telemetry.WriteLine(string.Format(CultureInfo.InvariantCulture,
                            "{0},{1},{2},{3},{4:F2},{5:F3},{6:F3},{7:F3},{8:F2}", speed, reversal ? "reversal" : "pulse",
                            throttle, direction, i * .01f, test.SteeringDegrees, test.SideslipDegrees, yawRate, test.Body.linearVelocity.magnitude * 3.6f));
                    }
                    Debug.Log($"AWD_STEERING_RECOVERY speed={speed} reversal={reversal} throttle={throttle} direction={direction} peakSlip={peakSlip:F2} tailSlip={tailSlip:F2} tailYaw={tailYaw:F2}");
                    if (!float.IsFinite(peakSlip) || peakSlip > 22 || tailSlip > 2 || tailYaw > 8)
                        throw new Exception("Manual steering did not settle after centring");
                    worstSlip = Mathf.Max(worstSlip, tailSlip); worstYaw = Mathf.Max(worstYaw, tailYaw);
                }
                Debug.Log($"AWD_MANUAL_STABILITY pass=True cases=24 maxResidualSlip={worstSlip:F2}deg maxResidualYaw={worstYaw:F2}deg/s");
            }
        }
        finally
        {
            vehicle.SetActive(false); Destroy(vehicle);
        }
    }
}
