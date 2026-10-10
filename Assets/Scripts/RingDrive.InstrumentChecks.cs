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
    CheckLcd(carModel,rpm,velocity.magnitude,gear);
    SwitchCarModel();CheckLcd(carModel,rpm,velocity.magnitude,gear);
   }
   awd.SetSimulationActive(false);view=0;lookYaw=lookPitch=0;
   for(int model=0;model<2;model++){
    velocity=Vector3.zero;rpm=900;gear=1;reversing=false;AnimateCar(0);
    CheckLcd(carModel,900,0,1);
    if(graphics)CheckLcdRendering();
    velocity=car.forward*24+car.right*18;rpm=6500;gear=4;AnimateCar(0);CheckLcd(carModel,6500,30,4);
    var other=carModels[1-selectedCar].instruments;var otherMaterial=other.screen.sharedMaterials[other.materialSlot];
    float otherRpm=otherMaterial.GetFloat("_Rpm");
    reversing=true;velocity=-car.forward*5;rpm=2400;AnimateCar(0);CheckLcd(carModel,2400,5,-1);
    if(graphics){var reverse=ReadLcd(carModel,"instruments-reverse");Destroy(reverse);}
    if(otherMaterial==carModel.instruments.screen.sharedMaterials[carModel.instruments.materialSlot]||otherMaterial.GetFloat("_Rpm")!=otherRpm)
     throw new Exception("Cockpit instruments shared state between models");
    SetPaused(true);AnimateCar(0);CheckLcd(carModel,2400,5,-1);SetPaused(false);
    SwitchCarModel();CheckLcd(carModel,2400,5,-1);
   }
   drivingSurface.Training=false;RecoverCar(TrackLandmarks.StartFinishPoint);awd.SetSimulationActive(false);
   velocity=car.forward*30;rpm=4500;gear=3;reversing=false;AnimateCar(0);UpdateCameraPose();
  }catch(Exception error){Debug.LogException(error);Application.Quit(1);yield break;}
  finally{Physics.simulationMode=simulation;modelPreview=true;}
  if(graphics)for(int model=0;model<2;model++){
   yield return new WaitForSeconds(.7f);CaptureScreenshot($"instruments-cockpit-{selectedCar}.png");
   yield return new WaitForSeconds(.3f);SwitchCarModel();
  }
  Debug.Log("INSTRUMENTS_TEST passed: AWD/arcade driving, both models, sideways speed, reverse, pause, isolated displays; "+(graphics?"GPU rev-bar/speed/RPM rendering passed":"GPU checks skipped: no graphics device"));
  Application.Quit(0);
 }
 void CheckLcdRendering(){
  var idle=ReadLcd(carModel,"instruments-idle");
  velocity=car.forward*24+car.right*18;rpm=6500;gear=4;AnimateCar(0);
  var fast=ReadLcd(carModel,"instruments-fast");
  if(fast.GetPixel(198,230).g<idle.GetPixel(198,230).g+.2f)throw new Exception("GPU rev bar did not fill");
  if(LcdDifference(idle,fast,new Rect(.06f,.2f,.53f,.34f))<.035f)throw new Exception("GPU speed digits did not change");
  if(LcdDifference(idle,fast,new Rect(.48f,.65f,.46f,.15f))<.035f)throw new Exception("GPU RPM digits did not change");
  if(fast.GetPixel(123,96).g>.2f||fast.GetPixel(291,186).g>.2f)throw new Exception("GPU decimal digits rounded their place divisor: expected 108 km/h and 6500 RPM");
  Destroy(idle);Destroy(fast);
 }
 static void CheckLcd(ImprezaModel model,float rpm,float speed,int gear){
  var lcd=model.instruments;var material=lcd.screen.sharedMaterials[lcd.materialSlot];
  if(Mathf.Abs(material.GetFloat("_Rpm")-rpm)>.01f||Mathf.Abs(material.GetFloat("_Speed")-speed*3.6f)>.01f||material.GetFloat("_Gear")!=gear)
   throw new Exception("Cockpit LCD disagrees with current driving/replay state");
 }
 static Texture2D ReadLcd(ImprezaModel model,string filename){
  var lcd=model.instruments;var material=lcd.screen.sharedMaterials[lcd.materialSlot];
  var target=RenderTexture.GetTemporary(384,256,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
  material.SetVector("_UvRect",new Vector4(0,0,1,1));Graphics.Blit(Texture2D.whiteTexture,target,material);
  material.SetVector("_UvRect",lcd.uvRect);RenderTexture.active=target;
  var image=new Texture2D(384,256,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,384,256),0,0);image.Apply();
  RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
  File.WriteAllBytes(Path.Combine(Application.persistentDataPath,filename+".png"),image.EncodeToPNG());return image;
 }
 static float LcdDifference(Texture2D a,Texture2D b,Rect area){
  float difference=0;int count=0;
  for(int y=(int)(area.yMin*a.height);y<area.yMax*a.height;y++)for(int x=(int)(area.xMin*a.width);x<area.xMax*a.width;x++){
   var d=a.GetPixel(x,y)-b.GetPixel(x,y);difference+=Mathf.Abs(d.r)+Mathf.Abs(d.g)+Mathf.Abs(d.b);count++;
  }
  return difference/count;
 }
}
