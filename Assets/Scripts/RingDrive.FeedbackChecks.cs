using System;
using System.Collections;
using UnityEngine;

public partial class RingDrive
{
 IEnumerator FeedbackTest(){
  Application.runInBackground=true;
  yield return null;
  var simulation=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
  try{
   dynamics=new DrivingSettings();awd.SetSimulationActive(true);awd.Body.interpolation=RigidbodyInterpolation.None;
   RecoverCar(TrackLandmarks.StartFinishPoint);
   void Settle(){for(int i=0;i<300;i++){awd.Step(.01f,0,1,0,dynamics);Physics.Simulate(.01f);UpdateDrivingFeedback(.01f);}}
   Settle();
   if(rideFeedback.Contacts.x<.99f||rideFeedback.RoadOffset.magnitude>.00001f)throw new Exception("Resting road contacts or view vibration");
   awd.Body.position+=Vector3.up*10;Physics.SyncTransforms();
   for(int i=0;i<30;i++){awd.Step(.01f,1,0,0,dynamics);Physics.Simulate(.01f);UpdateDrivingFeedback(.01f);}
   if(rideFeedback.Contacts.sqrMagnitude>.0001f)throw new Exception("Airborne wheels retained surface feedback");
   RecoverCar(TrackLandmarks.StartFinishPoint);
   var section=centerline.Sections[TrackLandmarks.StartFinishPoint];
   awd.Body.position+=section.Right*section.RightWidth;Physics.SyncTransforms();Settle();
   if(Mathf.Abs(rideFeedback.Contacts.x-.5f)>.01f||Mathf.Abs(rideFeedback.Contacts.z-.5f)>.01f)
    throw new Exception($"Split road/shoulder contacts incorrect: {rideFeedback.Contacts}");
   MakeTrainingArea();drivingSurface.Training=true;RecoverTrainingCar();Settle();
   for(int i=0;i<200;i++){awd.Step(.01f,1,0,0,dynamics);Physics.Simulate(.01f);UpdateDrivingFeedback(.01f);}
   if(rideFeedback.Offset.z>-.002f||motor.Asphalt<.6f)throw new Exception("Live driving acceleration feedback missing");
   var position=car.position;var motion=awd.Body.linearVelocity;
   view=0;lookYaw=12;lookPitch=3;UpdateCameraPose();var active=head.localPosition;
   dynamics.cockpitMovement=0;UpdateCameraPose();
   if(Vector3.Distance(head.localPosition,carModel.cockpitView.localPosition)>.00001f||Vector3.Distance(active,head.localPosition)<.001f)
    throw new Exception("Cockpit movement slider does not control the camera");
   if(Quaternion.Angle(head.localRotation,Quaternion.Euler(carModel.cockpitView.localEulerAngles.x+3,12,0))>.01f)
    throw new Exception("Feedback changed mouse look");
   view=2;chaseSlipYaw=awd.ForwardSpeed>8?Mathf.Clamp(awd.SideslipDegrees,-20,20):0;UpdateCameraPose();var chase=head.localPosition;dynamics.cockpitMovement=1;UpdateCameraPose();
   if(head.localPosition!=chase||car.position!=position||awd.Body.linearVelocity!=motion)throw new Exception("Camera effects changed chase pose or physics");
   dynamics.surfaceSound=0;UpdateDrivingFeedback(.01f);
   if(motor.Asphalt!=0||motor.Kerb!=0||motor.LooseGround!=0)throw new Exception("Surface sound slider failed");
   RecoverTrainingCar();
   if(rideFeedback.Offset!=Vector3.zero||rideFeedback.Angles!=Vector3.zero)throw new Exception("Recovery retained view motion");
   Debug.Log("FEEDBACK_TEST passed: grounded/split/airborne wheel contacts, stationary settling, live acceleration, sliders, mouse look, chase camera, physics isolation and recovery");
  }catch(Exception error){Debug.LogException(error);Application.Quit(1);yield break;}
  finally{Physics.simulationMode=simulation;}
  Application.Quit(0);
 }
}
