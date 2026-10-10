using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public partial class RingDrive
{
 IEnumerator ModelPreview()
 {
  Application.runInBackground=true;
  yield return null;
  try{CheckCarAnimation();}
  catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
  view=0;lookYaw=lookPitch=0;UpdateCameraPose();carModel.Animate(0,0,0,0,true);
  yield return new WaitForSeconds(2);CaptureScreenshot("cockpit-detail.png");yield return new WaitForSeconds(.25f);
  lookYaw=-105;UpdateCameraPose();yield return new WaitForSeconds(1);CaptureScreenshot("cockpit-left.png");yield return new WaitForSeconds(.25f);
  lookYaw=105;UpdateCameraPose();yield return new WaitForSeconds(1);CaptureScreenshot("cockpit-right.png");yield return new WaitForSeconds(.25f);
  lookYaw=0;view=1;UpdateCameraPose();carModel.Animate(0,0,0,0,false);
  yield return new WaitForSeconds(1);CaptureScreenshot("bonnet-detail.png");yield return new WaitForSeconds(.25f);
  view=2;UpdateCameraPose();yield return new WaitForSeconds(1);CaptureScreenshot("chase-detail.png");yield return new WaitForSeconds(.25f);
  head.localPosition=new Vector3(3.6f,1.85f,5.1f);head.localRotation=Quaternion.LookRotation(new Vector3(0,.8f,0)-head.localPosition);
  yield return new WaitForSeconds(1);CaptureScreenshot("model-detail.png");yield return new WaitForSeconds(.25f);
  for(int i=0;i<3;i++){
   yield return new WaitForSeconds(1);CaptureScreenshot("paint-"+carModel.PaintName.ToLowerInvariant().Replace(' ','-')+".png");yield return new WaitForSeconds(.25f);
   carModel.CyclePaint();
  }
  carModel.Animate(.75f,25,0,0,false);yield return new WaitForSeconds(1);CaptureScreenshot("model-steering.png");yield return new WaitForSeconds(.25f);
  carModel.Animate(0,0,0,0,false);head.localPosition=new Vector3(-3.3f,1.95f,-5.1f);head.localRotation=Quaternion.LookRotation(new Vector3(0,.8f,0)-head.localPosition);
  yield return new WaitForSeconds(1);CaptureScreenshot("rear-detail.png");yield return new WaitForSeconds(.25f);
  yield return new WaitForSeconds(1);Debug.Log("RALLY_MODEL_TEST passed: wheel pivots, steering, reverse rotation, pause, cockpit visibility and all camera views");Application.Quit();
 }

 void CheckCarAnimation()
 {
  var tyres=carModel.wheelSpin.Select(t=>t.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name.StartsWith("Wheel."))).ToArray();
  var centers=tyres.Select(r=>r.bounds.center).ToArray();
  var position=car.position;var rotation=car.rotation;
  carModel.Animate(0,0,0,0,false);
  var initial=carModel.wheelSpin[0].localRotation;
  carModel.Animate(.75f,25,10,.017f,true);
  if(Quaternion.Angle(initial,carModel.wheelSpin[0].localRotation)<10)throw new Exception("Road wheels did not roll");
  foreach(var pivot in carModel.frontSteering)if(Vector3.Dot(pivot.forward,car.right)<.3f)throw new Exception("Front wheel steering axis is incorrect");
  if(carModel.cockpitHead.enabled)throw new Exception("Driver head obscures cockpit view");
  foreach(var r in car.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="driver"||r.name=="seatbelts"))
   if(!r.enabled)throw new Exception("Driver body or belts hidden in cockpit view");
  carModel.Animate(0,0,-10,.017f,false);
  if(Quaternion.Angle(initial,carModel.wheelSpin[0].localRotation)>.1f)throw new Exception("Reverse wheel rotation is incorrect");
  for(int n=0;n<30;n++){
   carModel.Animate(0,0,10,.01f,false);
   for(int i=0;i<tyres.Length;i++)if(Vector3.Distance(centers[i],tyres[i].bounds.center)>.03f)throw new Exception("Wheel orbits its axle: "+tyres[i].name);
  }
  var beforePause=carModel.wheelSpin.Select(t=>t.localRotation).ToArray();
  carModel.Animate(0,0,10,0,false);
  for(int i=0;i<beforePause.Length;i++)if(Quaternion.Angle(beforePause[i],carModel.wheelSpin[i].localRotation)>.01f)throw new Exception("Paused wheel animation moved");
  if(!carModel.cockpitHead.enabled)throw new Exception("Driver head missing in exterior view");
  if(car.position!=position || Quaternion.Angle(rotation,car.rotation)>.01f)throw new Exception("Visual animation moved the physics root");
  Debug.Log("RALLY_ANIMATION_TEST passed: forward/reverse roll, steering axes, axle centers, pause, camera visibility, stable physics root");
 }
}
