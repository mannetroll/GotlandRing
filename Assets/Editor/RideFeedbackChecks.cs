using System;
using UnityEngine;

public static class RideFeedbackChecks
{
 public static void Run(){
  var track=TrackData.Load(Resources.Load<TextAsset>("Track/Surface").text);
  int kerbPoints=0;
  try{
   foreach(var run in TrackLandmarks.KerbRuns)for(int i=run.First+1;i<run.Last-1;i++){
    var row=track.Sections[i];float width=run.Side<0?row.LeftWidth:row.RightWidth;
    var p=row.Ground(run.Side*(width+.5f));
    if(RideFeedback.SurfaceAt(track,p,false)!=RideFeedback.Surface.Kerb)throw new Exception($"Kerb feedback misses {run.Name} at {i}");
    p=row.Road(run.Side*(width-.5f));
    if(RideFeedback.SurfaceAt(track,p,false)!=RideFeedback.Surface.Asphalt)throw new Exception("Kerb feedback leaks onto asphalt");
    p=row.Ground(run.Side*(width+2));
    if(RideFeedback.SurfaceAt(track,p,false)!=RideFeedback.Surface.Loose)throw new Exception("Kerb feedback extends into loose ground");
    kerbPoints++;
   }
   foreach(float edge in new[]{-1f,1f}){
    var p=DrivingSurface.TrainingCentre+Vector3.right*edge*(DrivingSurface.TrainingSize*.5f-.2f);
    if(RideFeedback.SurfaceAt(track,p,true)!=RideFeedback.Surface.Asphalt)throw new Exception("Training asphalt boundary");
    p+=Vector3.right*edge*.4f;
    if(RideFeedback.SurfaceAt(track,p,true)!=RideFeedback.Surface.Loose)throw new Exception("Training loose-ground boundary");
   }
  }finally{UnityEngine.Object.DestroyImmediate(track.SurfaceMesh);}
  Vector3 reference=Vector3.zero;
  foreach(int rate in new[]{30,60,120}){
   var ride=new RideFeedback();float dt=1f/rate;
   for(int i=0;i<=2*rate;i++)ride.Tick(dt,Vector3.forward*(10+8*i*dt),Quaternion.identity,Vector3.zero);
   if(ride.Offset.z>-.018f||ride.Angles.x>-.6f)throw new Exception("Acceleration must lean the head back");
   if(rate==30)reference=ride.Offset;
   else if(Vector3.Distance(reference,ride.Offset)>.001f)throw new Exception("View movement changes with frame rate");
   ride.Reset();
   for(int i=0;i<=2*rate;i++)ride.Tick(dt,Vector3.forward*(24-6*i*dt),Quaternion.identity,Vector3.zero);
   if(ride.Offset.z<.013f||ride.Angles.x<.5f)throw new Exception("Braking must lean the head forward");
   ride.Reset();
   for(int i=0;i<=2*rate;i++){
    var heading=Quaternion.Euler(0,i*dt*20,0);
    ride.Tick(dt,heading*Vector3.forward*20,heading,Vector3.zero);
   }
   if(ride.Offset.x>-.012f||ride.Angles.z<.4f)throw new Exception("Cornering must lean away from lateral acceleration");
   var frozen=ride.Offset;ride.Tick(0,Vector3.zero,Quaternion.identity,Vector3.one);
   if(ride.Offset!=frozen)throw new Exception("Paused feedback moved");
   for(int i=0;i<2*rate;i++)ride.Tick(dt,Vector3.zero,Quaternion.identity,Vector3.forward);
   if(ride.Offset.magnitude>.0001f||ride.RoadAngles!=Vector3.zero)throw new Exception("Stationary view does not settle");
   for(int i=0;i<rate;i++)ride.Tick(dt,Vector3.forward*50,Quaternion.identity,Vector3.forward);
   if(ride.RoadOffset.magnitude<.00001f)throw new Exception("Loose-ground vibration is absent");
   for(int i=0;i<2*rate;i++)ride.Tick(dt,Vector3.forward*50,Quaternion.identity,Vector3.zero);
   if(ride.RoadOffset.magnitude>.00001f)throw new Exception("Airborne road vibration does not fade");
   for(int i=0;i<rate*2;i++){
    ride.Tick(dt,new Vector3(i%2==0?100:-100,0,50),Quaternion.identity,Vector3.forward);
    if(ride.Offset.magnitude>.0451f||ride.Angles.magnitude>2)throw new Exception("View movement exceeds subtle movement bounds");
   }
   ride.Reset();ride.Tick(dt,Vector3.forward*50,Quaternion.identity,Vector3.zero);
   if(ride.Offset!=Vector3.zero||ride.Angles!=Vector3.zero)throw new Exception("Recovery generated an acceleration kick");
  }
  Debug.Log($"RIDE_FEEDBACK_TEST passed: {kerbPoints} kerb locations, asphalt/loose boundaries, pad, acceleration/braking/corner direction, 30/60/120 Hz, pause, stationary/airborne settling, limits and recovery");
 }
}
