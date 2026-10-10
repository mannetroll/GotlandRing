using System;
using UnityEngine;

public partial class RingDrive
{
    void CheckTrainingArea()
    {
        var original = dynamics.Copy();
        foreach (bool physical in new[] {true, false})
        foreach (bool pilot in new[] {false, true})
        {
            var settings = original.Copy(); settings.awdMode = physical; ApplyDrivingSettings(settings);
            RecoverCar(120); SetAutopilot(pilot);
            best = 123; SetPaused(true);
            ToggleTrainingArea();
            if (!drivingSurface.Training || !paused || autopilotEnabled != pilot || best != 123 || checkpoints != 0)
                throw new Exception("Area toggle changed pause, autopilot or best lap state");
            var position = car.position;
            for (int i = 0; i < 50; i++) Physics.Simulate(.01f);
            if (Vector3.Distance(car.position, position) > .001f)
                throw new Exception($"Paused training car moved: physical={physical} initial={position:F4} final={car.position:F4} body={awd.Body.position:F4} kinematic={awd.Body.isKinematic}");
            SetPaused(false);
            for (int i = 0; i < 300; i++)
            {
                if (physical) { awd.Step(.01f, 0, 1, 0, dynamics); Physics.Simulate(.01f); SyncAwdMotion(); }
            }
            if (physical && awd.GroundedWheels != 4) throw new Exception("Training spawn lost tyre contact");
            int beforeLap = lap;
            float maxSpeed = 0;
            for (int i = 0; i < 3000; i++)
            {
                StepDriving(.01f, Time.time, 0, !pilot && i < 300 ? 1 : 0, !pilot && i >= 300 ? 1 : 0, false);
                if (physical) { Physics.Simulate(.01f); SyncAwdMotion(); }
                maxSpeed = Mathf.Max(maxSpeed, velocity.magnitude);
                if (!drivingSurface.Sample(car.position.x, car.position.z).OnRoad)
                    throw new Exception("Training driving left the 800 metre asphalt area");
            }
            if (maxSpeed < 5 || lap != beforeLap || best != 123 || autopilotEnabled != pilot)
                throw new Exception("Training driving lost controls or recorded a circuit lap");
            RecoverCar(nearest);
            if (velocity.magnitude > .01f || Vector3.Distance(car.position, position) > .05f)
                throw new Exception("Training recovery did not stop at its start");
            ToggleTrainingArea();
            if (drivingSurface.Training || nearest != TrackLandmarks.StartFinishPoint || checkpoints != 0 || lap != 1
                || velocity.magnitude > .01f || autopilotEnabled != pilot || best != 123)
                throw new Exception("Return to Gotland Ring did not reset the timed lap or preserve driving preferences");
            Debug.Log($"TRAINING_AREA_TEST pass=True physical={physical} pilot={pilot} maxSpeed={maxSpeed * 3.6f:F1}km/h");
        }
        ApplyDrivingSettings(original); best = 0; SetAutopilot(false); RestartLap();
        foreach (float x in new[] {-399f, 0, 399}) foreach (float z in new[] {-399f, 0, 399})
        {
            var point = DrivingSurface.TrainingCentre + new Vector3(x, 0, z);
            if (!trainingGround.GetComponent<BoxCollider>().Raycast(new Ray(point + Vector3.up * 5, Vector3.down), out var hit, 10)
                || Mathf.Abs(hit.point.y - point.y) > .001f)
                throw new Exception("Training asphalt is not flat at its centre or edges");
        }
        Debug.Log("TRAINING_AREA_TEST ALL PASSED: AWD/arcade, manual/pilot, pause, reset, return, lap isolation, flat contact");
    }
    void CheckSlideThrottle()
    {
        var original=dynamics.Copy();
        int cases=0;
        foreach(bool physical in new[]{true,false}) foreach(bool training in new[]{true,false})
        {
            var settings=original.Copy();settings.awdMode=physical;ApplyDrivingSettings(settings);
            drivingSurface.Training=training;
            void Reset(){
                RecoverCar(TrackLandmarks.StartFinishPoint);
                if(physical) for(int step=0;step<300;step++){awd.Step(.01f,0,1,0,dynamics);Physics.Simulate(.01f);}
            }
            foreach(float angle in new[]{-135f,-100f,100f,135f,180f})
            {
                Reset();
                // Seed a slide beyond sideways while the driver still has a forward gear selected.
                var motion=(car.forward*Mathf.Cos(angle*Mathf.Deg2Rad)+car.right*Mathf.Sin(angle*Mathf.Deg2Rad))*25;
                if(physical)awd.Body.linearVelocity=motion;else velocity=motion;
                for(int step=0;step<30;step++)
                {
                    StepDriving(.01f,Time.time,0,1,0,false);
                    if(throttle!=1||reversing)throw new Exception($"Slide changed the accelerator/direction: AWD={physical} training={training} angle={angle}");
                    if(physical){
                        foreach(var wheel in awd.Wheels)if(wheel.brakeTorque!=0)throw new Exception("Slide applied unrequested wheel brakes");
                        Physics.Simulate(.01f);
                    }
                }
                StepDriving(.01f,Time.time,0,1,1,false);
                if(physical)foreach(var wheel in awd.Wheels)
                    if(wheel.brakeTorque<=0||wheel.motorTorque!=0)throw new Exception("Explicit braking failed during a slide");
                cases++;
            }
            Reset();
            for(int step=0;step<150;step++){
                StepDriving(.01f,Time.time,0,0,0,true);
                if(physical){Physics.Simulate(.01f);SyncAwdMotion();}
            }
            if(!reversing||Vector3.Dot(velocity,car.forward)>-1)throw new Exception("Deliberate reverse did not engage");
            int forwardSteps=0;bool powered=false;
            while(Vector3.Dot(velocity,car.forward)<2&&forwardSteps++<500){
                // Forward throttle also wins if W and X are briefly held together.
                StepDriving(.01f,Time.time,0,1,0,forwardSteps<10);
                if(throttle!=1||reversing)throw new Exception("W did not immediately select forward throttle");
                if(physical){
                    foreach(var wheel in awd.Wheels){
                        if(wheel.brakeTorque!=0||wheel.motorTorque<0)throw new Exception("W applied brakes or reverse drive");
                        powered|=wheel.motorTorque>0;
                    }
                    Physics.Simulate(.01f);SyncAwdMotion();
                }
            }
            if(forwardSteps>=500||physical&&!powered)throw new Exception("Forward throttle did not drive the reversing car forwards");
            StepDriving(.01f,Time.time,0,0,0,true);
            if(reversing||throttle!=0)throw new Exception("X did not brake before changing from forward to reverse");
        }
        drivingSurface.Training=false;ApplyDrivingSettings(original);RestartLap();
        Debug.Log($"SLIDE_THROTTLE_TEST passed: {cases} spins beyond 90 degrees, held accelerator, explicit brakes, powered reverse-to-forward, W/X priority, AWD/arcade and circuit/training");
    }
}
