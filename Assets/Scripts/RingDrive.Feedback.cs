using UnityEngine;

public partial class RingDrive
{
 readonly RideFeedback rideFeedback=new RideFeedback();
 int settingsPage;
 void UpdateDrivingFeedback(float dt){
  Vector3 contacts=Vector3.zero;
  if(dynamics.awdMode){
   foreach(var wheel in awd.Wheels)
    if(wheel.GetGroundHit(out var hit))contacts+=RideFeedback.Weight(RideFeedback.SurfaceAt(centerline,hit.point,drivingSurface.Training))*.25f;
  }else{
   foreach(var wheel in awd.Wheels){
    var point=car.TransformPoint(wheel.transform.localPosition);
    contacts+=RideFeedback.Weight(RideFeedback.SurfaceAt(centerline,point,drivingSurface.Training))*.25f;
   }
  }
  rideFeedback.Tick(dt,dynamics.awdMode?awd.Body.linearVelocity:velocity,car.rotation,contacts);
  motor.Asphalt=rideFeedback.Contacts.x*dynamics.surfaceSound;
  motor.Kerb=rideFeedback.Contacts.y*dynamics.surfaceSound;
  motor.LooseGround=rideFeedback.Contacts.z*dynamics.surfaceSound;
 }
 void ApplyCameraFeedback(){
  float amount=dynamics.cockpitMovement;
  if(view==0){head.localPosition+=rideFeedback.Offset*amount;head.localRotation*=Quaternion.Euler(rideFeedback.Angles*amount);}
  else if(view==1){head.localPosition+=rideFeedback.RoadOffset*amount*.25f;head.localRotation*=Quaternion.Euler(rideFeedback.RoadAngles*amount*.25f);}
 }
 void ResetFeedback(){rideFeedback.Reset();if(motor){motor.Asphalt=motor.Kerb=motor.LooseGround=0;}}
}
