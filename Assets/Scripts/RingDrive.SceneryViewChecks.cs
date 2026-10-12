using System;
using System.Collections;
using UnityEngine;

public partial class RingDrive
{
    IEnumerator SceneryViewsTest()
    {
        Application.runInBackground = true;
        yield return null;
        foreach (bool awdMode in new[] {true, false})
        foreach (bool training in new[] {false, true})
        foreach (bool startPaused in new[] {false, true})
        {
            Vector3 position = default, momentum = default, spin = default;
            Quaternion rotation = default, cameraRotation = default;
            Vector3 cameraPosition = default;
            float elapsed = 0, fov = 0; int oldLap = 0, oldCheckpoints = 0, oldCar = 0;
            try
            {
                SetPaused(false);
                if (drivingSurface.Training != training) ToggleTrainingArea();
                ApplyDrivingSettings(new DrivingSettings {awdMode = awdMode});
                SetAutopilot(!training);
                if (selectedCar != (startPaused ? 1 : 0)) SwitchCarModel();
                view = startPaused ? 0 : 2; lookYaw = 12; lookPitch = 4; UpdateCameraPose();
                cam.fieldOfView = 81;
                velocity = car.forward * 20; rpm = 4500; gear = 3;
                momentum = velocity; spin = Vector3.up * .12f;
                if (awdMode) { awd.Body.linearVelocity = momentum; awd.Body.angularVelocity = spin; }
                SetPaused(startPaused);
                position = car.position; rotation = car.rotation;
                elapsed = (paused ? pauseStarted : Time.time) - lapStart;
                oldLap = lap; oldCheckpoints = checkpoints; oldCar = selectedCar;
                cameraPosition = head.localPosition; cameraRotation = head.localRotation; fov = cam.fieldOfView;
                NextSceneryView();
                if (!paused || !awd.Body.isKinematic || !motor.Muted) throw new Exception("Scenery view did not suspend driving and sound");
            }
            catch (Exception error) { Debug.LogException(error); Application.Quit(1); yield break; }
            yield return new WaitForSeconds(.08f);
            try
            {
                int count = 0;
                do
                {
                    if (!float.IsFinite(head.position.sqrMagnitude) || head.position.y < SceneryGroundHeight(head.position.x, head.position.z) + 10)
                        throw new Exception("Scenery camera is inside or too close to the ground");
                    if (car.position != position || Quaternion.Angle(car.rotation, rotation) > .01f || !paused)
                        throw new Exception("Scenery navigation moved the saved car");
                    count++; NextSceneryView();
                } while (SceneryViewing);
                if (count != 15 || paused != startPaused || drivingSurface.Training != training || autopilotEnabled != !training
                    || selectedCar != oldCar || lap != oldLap || checkpoints != oldCheckpoints || rpm != 4500 || gear != 3
                    || velocity != momentum || awd.Body.isKinematic != (!awdMode || startPaused)
                    || Mathf.Abs((paused ? pauseStarted : Time.time) - lapStart - elapsed) > .01f)
                    throw new Exception("Scenery tour changed the saved drive or lap time");
                if (cam.orthographic || cam.fieldOfView != fov || head.localPosition != cameraPosition || Quaternion.Angle(head.localRotation, cameraRotation) > .01f)
                    throw new Exception("Scenery tour did not restore the driving camera");
                if (awdMode && !startPaused && (Vector3.Distance(awd.Body.linearVelocity, momentum) > .001f || Vector3.Distance(awd.Body.angularVelocity, spin) > .001f))
                    throw new Exception("Scenery tour lost driving momentum");
                NextSceneryView(); NextSceneryView(); ExitSceneryViews();
                if (SceneryViewing || cam.orthographic || paused != startPaused) throw new Exception("Early scenery exit did not restore the drive");
                SetPaused(true);
            }
            catch (Exception error) { Debug.LogException(error); Application.Quit(1); yield break; }
        }
        if (drivingSurface.Training) ToggleTrainingArea();
        NextSceneryView();
        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            for (int i = 0; i < sceneryViews.Count; i++)
            {
                yield return new WaitForSeconds(.25f);
                CaptureScreenshot($"scenery-view-{i + 1:00}.png");
                yield return new WaitForSeconds(.15f);
                NextSceneryView();
            }
        }
        else ExitSceneryViews();
        Debug.Log("SCENERY_VIEWS_TEST passed: 15 elevated views, both cars, circuit/training, AWD/arcade, paused/running restoration, momentum, lap isolation, projection and early exit");
        Application.Quit();
    }
}
