using System;
using System.Collections;
using UnityEngine;

public partial class RingDrive
{
    IEnumerator ReferenceLapTest()
    {
        Application.runInBackground = true;
        yield return null;
        try
        {
            foreach (bool awdMode in new[] { true, false }) foreach (bool training in new[] { false, true }) foreach (bool startPaused in new[] { false, true })
            {
                SetPaused(false);
                if (drivingSurface.Training != training) ToggleTrainingArea();
                var settings = dynamics.Copy(); settings.awdMode = awdMode; ApplyDrivingSettings(settings);
                SetAutopilot(!training); muted = startPaused;
                if(selectedCar != (startPaused ? 1 : 0)) SwitchCarModel();
                awd.SetSimulationActive(awdMode);
                if (awdMode) { awd.Body.linearVelocity = car.forward * 12; awd.Body.angularVelocity = Vector3.up * .12f; }
                SetPaused(startPaused);
                var position = car.position; var rotation = car.rotation;
                float start = lapStart, oldPause = pauseStarted, oldRpm = rpm;
                int oldLap = lap, oldCheckpoints = checkpoints, oldCar = selectedCar; bool pilot = autopilotEnabled;
                ToggleReferenceLap();
                if (!paused || car.gameObject.activeSelf || !awd.Body.isKinematic) throw new Exception("Replay did not suspend driving");
                referenceLap.Model.Animate(0,0,0,true);var cockpitStraight=referenceLap.Model.steeringWheel.localRotation;
                referenceLap.Seek(90);
                float roadAngle=Mathf.DeltaAngle(0,referenceLap.Model.frontSteering[0].localEulerAngles.y);
                if(Quaternion.Angle(referenceLap.Model.steeringWheel.localRotation,cockpitStraight*Quaternion.AngleAxis(-roadAngle*ImprezaModel.SteeringRatio,Vector3.forward))>.02f)
                    throw new Exception("Replay cockpit wheel disagrees with its road wheels");
                referenceLap.Paused = true; referenceLap.Tick(3, false);
                if (referenceLap.Elapsed != 90 || !referenceLap.Motor.Muted) throw new Exception("Replay pause advanced time or left audio running");
                referenceLap.Paused = false; referenceLap.Tick(.5f, true);
                if (Mathf.Abs(referenceLap.Elapsed - 90.5f) > .001f || !referenceLap.Motor.Muted) throw new Exception("Replay mute changed timing");
                referenceLap.Tick(0, false);
                if (referenceLap.Motor.Muted || referenceLap.Motor.Rpm < 3800) throw new Exception("Replay did not restore racing sound");
                referenceLap.Seek(referenceLap.Data.Duration); referenceLap.Tick(1, false);
                if (!referenceLap.Finished || !referenceLap.Motor.Muted || Mathf.Abs(referenceLap.LapTime - 175.88f) > .002f) throw new Exception("Replay completion clock or sound is incorrect");
                ToggleReferenceLap();
                if (paused != startPaused || !car.gameObject.activeSelf || autopilotEnabled != pilot || drivingSurface.Training != training
                    || Vector3.Distance(position, car.position) > .001f || Quaternion.Angle(rotation, car.rotation) > .01f
                    || selectedCar != oldCar || lap != oldLap || checkpoints != oldCheckpoints || rpm != oldRpm || awd.Body.isKinematic != (!awdMode || startPaused)
                    || startPaused && (lapStart != start || pauseStarted != oldPause)) throw new Exception("Replay changed the saved drive");
                if (awdMode && !startPaused && (Vector3.Distance(awd.Body.linearVelocity, car.forward * 12) > .001f || Vector3.Distance(awd.Body.angularVelocity, Vector3.up * .12f) > .001f))
                    throw new Exception("Replay lost driving momentum");
            }
            SetPaused(true); if (drivingSurface.Training) ToggleTrainingArea(); muted = false; ToggleReferenceLap();
            var data = referenceLap.Data;
            if (data.fps != 24 || data.frames.Length != 4464) throw new Exception("Reference alignment is incomplete");
            float previous = float.NegativeInfinity;
            foreach (var frame in data.frames)
            {
                if (!float.IsFinite(frame.distance) || frame.distance <= previous || frame.rpm < 3800 || frame.rpm > 6800
                    || frame.load < 0 || frame.load > 1 || frame.speed <= 0 || frame.gear < 1 || frame.gear > 5) throw new Exception("Invalid reference motion or audio sample");
                var p = referenceLap.Point(frame.distance);
                if (!centerline.Sample(p.x, p.z).OnRoad) throw new Exception("Reference path leaves the road");
                previous = frame.distance;
            }
            var startSample = data.Sample(data.lapStart); var finishSample = data.Sample(data.lapStart + data.lapDuration);
            // The visual landmark fit has metre-scale uncertainty at each crossing.
            if (Mathf.Abs(finishSample.distance - startSample.distance - centerline.HorizontalLength) > 10) throw new Exception("Reference clock does not span a complete circuit");
            dynamics.cockpitMovement=.5f;dynamics.surfaceSound=.65f;referenceLap.Paused=false;
            referenceLap.Seek(90);
            float maxMovement=0;
            for(int i=0;i<120;i++){
                referenceLap.Tick(1f/60,false);
                maxMovement=Mathf.Max(maxMovement,Vector3.Distance(referenceLap.Cockpit.transform.localPosition,referenceLap.Model.cockpitView.localPosition));
            }
            if(maxMovement<.0001f||maxMovement>.023f||referenceLap.Motor.Asphalt<.1f)throw new Exception("Replay movement or road feedback missing/out of bounds");
            referenceLap.Paused=true;var frozenView=referenceLap.Cockpit.transform.localPosition;referenceLap.Tick(1,false);
            if(referenceLap.Cockpit.transform.localPosition!=frozenView)throw new Exception("Replay camera moved while paused");
            dynamics.cockpitMovement=dynamics.surfaceSound=0;referenceLap.Tick(0,false);
            if(referenceLap.Cockpit.transform.localPosition!=referenceLap.Model.cockpitView.localPosition||referenceLap.Motor.Asphalt!=0)throw new Exception("Replay ignores view/sound sliders");
            dynamics.cockpitMovement=.5f;dynamics.surfaceSound=.65f;
            referenceLap.Seek(156.6f); referenceLap.Paused = true;
            if(referenceLap.Cockpit.transform.localPosition!=referenceLap.Model.cockpitView.localPosition)throw new Exception("Replay seek retained movement");
        }
        catch (Exception error) { Debug.LogException(error); Application.Quit(1); yield break; }
        yield return new WaitForSeconds(.7f);
        CaptureScreenshot("reference-lap-arho.png");
        yield return new WaitForSeconds(.3f);
        float savedLapStart = lapStart;
        try
        {
            if (FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length != 1) throw new Exception("Replay has duplicate audio listeners");
            if (Mathf.Abs(referenceLap.Bonnet.aspect - 1920f / 708) > .001f || Mathf.Abs(referenceLap.Cockpit.aspect - 960f / 372) > .001f) throw new Exception("Replay camera layout is stretched");
            ToggleReferenceLap(); SetPaused(false); ToggleReferenceLap(); savedLapStart = lapStart;
        }
        catch (Exception error) { Debug.LogException(error); Application.Quit(1); yield break; }
        float watchStarted = Time.time;
        yield return new WaitForSeconds(.5f);
        try
        {
            ToggleReferenceLap();
            if (Mathf.Abs(lapStart - savedLapStart - (Time.time - watchStarted)) > .03f) throw new Exception("Replay time leaked into driving lap time");
            SetPaused(true);
        }
        catch (Exception error) { Debug.LogException(error); Application.Quit(1); yield break; }
        yield return null;
        try
        {
            if (FindObjectsByType<ReferenceLapPlayer>(FindObjectsSortMode.None).Length != 0 || FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length != 1)
                throw new Exception("Replay objects or listener leaked after exit");
            Debug.Log("REFERENCE_LAP_TEST passed: 4464 samples, complete circuit, racing RPM, pause/mute/end, eight drive restoration cases, lap time isolation, cameras and cleanup");
            Application.Quit(0);
        }
        catch (Exception error) { Debug.LogException(error); Application.Quit(1); }
    }
}
