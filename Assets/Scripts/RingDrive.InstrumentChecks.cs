using System;
using System.Collections;
using System.IO;
using UnityEngine;

public partial class RingDrive
{
 IEnumerator InstrumentsTest(){
  Application.runInBackground=true;
  bool graphics=SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null;
  yield return null;
  var simulation=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
  try{
   MakeTrainingArea();drivingSurface.Training=true;
   foreach(bool awdMode in new[]{true,false}){
    dynamics=new DrivingSettings{awdMode=awdMode};awd.SetSimulationActive(awdMode);RecoverTrainingCar();
    modelPreview=false;
    for(int step=0;step<600;step++){
     StepDriving(.01f,step*.01f,0,1,0,false);Physics.Simulate(.01f);
    }
    modelPreview=true;if(awdMode)SyncAwdMotion();AnimateCar(0);
    if(velocity.magnitude<10||rpm<1500)throw new Exception("Instrument driving setup did not accelerate");
    CheckInstruments(carModel,rpm,velocity.magnitude,gear);
    SwitchCarModel();CheckInstruments(carModel,rpm,velocity.magnitude,gear);
   }
   awd.SetSimulationActive(false);view=0;lookYaw=lookPitch=0;
   for(int model=0;model<2;model++){
    velocity=Vector3.zero;rpm=900;gear=1;reversing=false;AnimateCar(0);
    CheckInstruments(carModel,900,0,1);
    if(graphics)CheckInstrumentsRendering();
    velocity=car.forward*24+car.right*18;rpm=6500;gear=4;AnimateCar(0);CheckInstruments(carModel,6500,30,4);
    var other=carModels[1-selectedCar].instruments;var otherMaterial=other.screen.sharedMaterials[other.materialSlot];
    float otherRpm=otherMaterial.GetFloat("_Rpm");
    reversing=true;velocity=-car.forward*5;rpm=2400;AnimateCar(0);CheckInstruments(carModel,2400,5,-1);
    if(graphics){var reverse=ReadCluster(carModel,"instruments-reverse");Destroy(reverse);}
    if(otherMaterial==carModel.instruments.screen.sharedMaterials[carModel.instruments.materialSlot]||otherMaterial.GetFloat("_Rpm")!=otherRpm)
     throw new Exception("Cockpit instruments shared state between models");
    SetPaused(true);AnimateCar(0);CheckInstruments(carModel,2400,5,-1);SetPaused(false);
    SwitchCarModel();CheckInstruments(carModel,2400,5,-1);
   }
   drivingSurface.Training=false;RecoverCar(TrackLandmarks.StartFinishPoint);awd.SetSimulationActive(false);
   velocity=car.forward*30;rpm=4500;gear=3;reversing=false;AnimateCar(0);UpdateCameraPose();
  }catch(Exception error){Debug.LogException(error);Application.Quit(1);yield break;}
  finally{Physics.simulationMode=simulation;modelPreview=true;}
  if(graphics)for(int model=0;model<2;model++){
   yield return new WaitForSeconds(.7f);CaptureScreenshot($"instruments-cockpit-{selectedCar}.png");
   yield return new WaitForSeconds(.3f);SwitchCarModel();
  }
  Debug.Log("INSTRUMENTS_TEST passed: AWD/arcade driving, both models, sideways speed, reverse, pause, isolated displays; "+(graphics?"GPU analogue needle rendering passed":"GPU checks skipped: no graphics device"));
  Application.Quit(0);
 }
 void CheckInstrumentsRendering(){
  carModel.instruments.Set(900,0,1,1);
  var idle=ReadCluster(carModel,"instruments-idle");
  RequireNeedle(idle,new Vector2(898,169),112,198,"idle tachometer");
  RequireNeedle(idle,new Vector2(600,183),125,225,"stopped speedometer");
  RequireNeedle(idle,new Vector2(230,181),43,80,"full fuel");
  carModel.instruments.Set(6500,30,4,.05f);
  var fast=ReadCluster(carModel,"instruments-fast");
  RequireNeedle(fast,new Vector2(898,169),112,30,"6500 RPM");
  RequireNeedle(fast,new Vector2(600,183),125,63,"108 km/h");
  RequireNeedle(fast,new Vector2(230,181),43,194,"low fuel");
  if(ClusterDifference(idle,fast,new Rect(.38f,.1f,.24f,.76f))<.002f||
      ClusterDifference(idle,fast,new Rect(.64f,.1f,.23f,.73f))<.002f)
   throw new Exception("Analogue speed/RPM needles did not change");
  Destroy(idle);Destroy(fast);
 }
 static void RequireNeedle(Texture2D image,Vector2 centre,float length,float angle,string label){
  var direction=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));int hits=0;
  for(int i=0;i<10;i++){
   var p=centre+direction*Mathf.Lerp(length>70?26:17,length*.82f,i/9f);
   var pixel=image.GetPixel(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y));
   if(pixel.r>.6f&&pixel.r>pixel.g*1.6f&&pixel.r>pixel.b*2)hits++;
  }
  if(hits<7)throw new Exception("Instrument needle does not match "+label+": "+hits+"/10 red samples");
 }
 static void CheckInstruments(ImprezaModel model,float rpm,float speed,int gear){
  var instruments=model.instruments;var material=instruments.screen.sharedMaterials[instruments.materialSlot];
  if(material!=instruments.Display)throw new Exception("Physical cluster is not using its live material");
  if(Mathf.Abs(material.GetFloat("_Rpm")-rpm)>.01f||Mathf.Abs(material.GetFloat("_Speed")-speed*3.6f)>.01f||material.GetFloat("_Gear")!=gear)
   throw new Exception("Instrument cluster disagrees with current driving/replay state");
 }
 static Texture2D ReadCluster(ImprezaModel model,string filename){
  var instruments=model.instruments;var material=instruments.screen.sharedMaterials[instruments.materialSlot];
  var target=RenderTexture.GetTemporary(1200,400,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
  Graphics.Blit(Texture2D.whiteTexture,target,material);
  RenderTexture.active=target;
  var image=new Texture2D(1200,400,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,400),0,0);image.Apply();
  RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
  File.WriteAllBytes(Path.Combine(Environment.GetEnvironmentVariable("GOTLAND_TEST_OUTPUT")??Application.persistentDataPath,filename+".png"),image.EncodeToPNG());return image;
 }
 static float ClusterDifference(Texture2D a,Texture2D b,Rect area){
  float difference=0;int count=0;
  for(int y=(int)(area.yMin*a.height);y<area.yMax*a.height;y++)for(int x=(int)(area.xMin*a.width);x<area.xMax*a.width;x++){
   var d=a.GetPixel(x,y)-b.GetPixel(x,y);difference+=Mathf.Abs(d.r)+Mathf.Abs(d.g)+Mathf.Abs(d.b);count++;
  }
  return difference/count;
 }
}
