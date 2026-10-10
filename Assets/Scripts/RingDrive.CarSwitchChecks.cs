using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public partial class RingDrive
{
 IEnumerator CarSwitchTest(){
  Application.runInBackground=true;
  Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
  yield return null;
  try{
   velocity=car.forward*28;rpm=4200;gear=3;lap=2;checkpoints=2;best=181;
   CheckCarAnimation();SetAutopilot(true);carModel.CyclePaint();string originalPaint=carModel.PaintName;
   for(int cameraView=0;cameraView<3;cameraView++){
    view=cameraView;lookYaw=12;lookPitch=3;SetPaused(cameraView==1);
    CheckCarSwitchState();CheckCarAnimation();CheckCarSwitchState();
    if(carModel.PaintName!=originalPaint)throw new Exception("Model switch lost original paint selection");
   }
   SetPaused(false);SetAutopilot(false);velocity=Vector3.zero;rpm=900;gear=1;lookYaw=lookPitch=0;
  }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
  for(int model=0;model<carModels.Length;model++){
   view=0;UpdateCameraPose();carModel.Animate(0,0,0,true);
   yield return new WaitForSeconds(1);CaptureScreenshot($"car-{model}-cockpit.png");yield return new WaitForSeconds(.3f);
   foreach(float angle in new[]{-4f,4f,-19f,19f,-42f,42f}){
    carModel.Animate(angle,0,0,true);
    yield return new WaitForSeconds(.5f);CaptureScreenshot($"car-{model}-cockpit-{(angle<0?"left":"right")}-{Mathf.Abs(angle):0}.png");yield return new WaitForSeconds(.3f);
   }
   view=1;UpdateCameraPose();carModel.Animate(0,0,0,false);
   yield return new WaitForSeconds(1);CaptureScreenshot($"car-{model}-bonnet.png");yield return new WaitForSeconds(.3f);
   view=2;UpdateCameraPose();
   yield return new WaitForSeconds(1);CaptureScreenshot($"car-{model}-chase.png");yield return new WaitForSeconds(.3f);
   head.localPosition=new Vector3(3.6f,1.85f,5.1f);head.localRotation=Quaternion.LookRotation(new Vector3(0,.8f,0)-head.localPosition);
   yield return new WaitForSeconds(1);CaptureScreenshot($"car-{model}-front.png");yield return new WaitForSeconds(.3f);
   SwitchCarModel();
  }
  Debug.Log("CAR_SWITCH_TEST passed: both models, moving/paused state, camera modes, paint retention, wheel/steering animation, one active model, unchanged audio and physics");
  Application.Quit();
 }

 void CheckCarSwitchState(){
  var position=car.position;var rotation=car.rotation;var motion=velocity;var audio=motor;var camera=cam;
  float engineRpm=rpm,started=lapStart,record=best,heading=yaw,lookingYaw=lookYaw,lookingPitch=lookPitch;
  int previousModel=selectedCar,previousGear=gear,previousLap=lap,previousCheckpoints=checkpoints,previousView=view;
  bool wasPaused=paused,wasAutopilot=autopilotEnabled;
  SwitchCarModel();
  if(selectedCar==previousModel||carModels.Count(m=>m.gameObject.activeInHierarchy)!=1)throw new Exception("Model selection or visibility failed");
  if(car.position!=position||Quaternion.Angle(rotation,car.rotation)>.01f||velocity!=motion||yaw!=heading||rpm!=engineRpm||gear!=previousGear)
   throw new Exception("Model switch changed driving state");
  if(lapStart!=started||lap!=previousLap||checkpoints!=previousCheckpoints||best!=record||paused!=wasPaused||autopilotEnabled!=wasAutopilot)
   throw new Exception("Model switch changed lap, pause or autopilot state");
  if(cam!=camera||motor!=audio||view!=previousView||lookYaw!=lookingYaw||lookPitch!=lookingPitch||car.GetComponents<BoxerAudio>().Length!=1)
   throw new Exception("Model switch changed camera/audio state");
  if(view<2){
   var anchor=view==0?carModel.cockpitView:carModel.bonnetView;
   if(Vector3.Distance(head.position,anchor.position)>.001f)throw new Exception("Camera does not follow the selected model anchor");
  }
 }
}
