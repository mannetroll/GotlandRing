using System;
using System.Collections;
using UnityEngine;

public partial class RingDrive
{
 IEnumerator SurfaceTest(){
  Application.runInBackground=true;muted=true;yield return null;
  int positive=0,negative=0;float minBank=100,maxBank=-100,maxContactError=0;
  try{
   for(int i=0;i<track.Count;i++){
    var row=centerline.Sections[i];float bank=Mathf.Atan((row.Road(row.RightWidth).y-row.Road(-row.LeftWidth).y)/(row.RightWidth+row.LeftWidth))*Mathf.Rad2Deg;
    if(bank<minBank){minBank=bank;negative=i;}if(bank>maxBank){maxBank=bank;positive=i;}
    if(i%13!=0)continue;
    foreach(float offset in new[]{-row.LeftWidth*.55f,0,row.RightWidth*.55f})foreach(int direction in new[]{1,-1}){
     var point=row.Road(offset);var heading=Vector3.Cross(row.Right,Vector3.up)*direction;yaw=Quaternion.LookRotation(heading).eulerAngles.y;
     PlaceCarOnSurface(point,0);
     if(Vector3.Dot(car.forward,heading)<.95f)throw new Exception("Surface alignment changed heading");
     foreach(var contact in new[]{new Vector3(-.7f,0,0),new Vector3(.7f,0,0),new Vector3(0,0,-1.25f),new Vector3(0,0,1.25f)}){
      var wheel=car.TransformPoint(contact);float error=Mathf.Abs(wheel.y-Ground(wheel.x,wheel.z)-.04f);maxContactError=Mathf.Max(maxContactError,error);
      if(error>.12f)throw new Exception($"Car does not follow bank/pitch: row={i} offset={offset} direction={direction} contact={contact} error={error:F4}");
     }
    }
   }
   foreach(int index in new[]{positive,negative}){
    RecoverCar(index);var row=centerline.Sections[index];
    if(Vector3.Dot(car.up,centerline.Sample(car.position.x,car.position.z).Normal)<.999f)throw new Exception("Recovery did not preserve banking");
    float acceleration=TrackData.BankAcceleration(car.up,row.Right);
    if(Mathf.Sign(acceleration)==Mathf.Sign(row.Slope))throw new Exception("Bank gravity points uphill");
   }
  }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
  view=2;lookPitch=-8;lookYaw=12;
  foreach(int index in new[]{positive,negative,track.Count-1,0}){
   RecoverCar(index);lookPitch=-8;lookYaw=12;UpdateCameraPose();
   yield return new WaitForSeconds(1);CaptureScreenshot($"surface-bank-{index:0000}.png");yield return new WaitForSeconds(.25f);
  }
  Debug.Log($"SURFACE_DRIVING_TEST passed: forward/reverse contact, recovery, bank gravity, lap seam; bank={minBank:F3}..{maxBank:F3}deg maxContactError={maxContactError:F4}m");
  Application.Quit();
 }
}
