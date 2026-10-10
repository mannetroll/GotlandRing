using UnityEngine;

// View and sound cues only: these never apply forces or change tyre grip.
public sealed class RideFeedback
{
 public enum Surface { Asphalt, Kerb, Loose }
 public Vector3 Offset { get; private set; }
 public Vector3 Angles { get; private set; }
 public Vector3 RoadOffset { get; private set; }
 public Vector3 RoadAngles { get; private set; }
 public Vector3 Contacts { get; private set; } // Asphalt, kerb, loose-ground wheel fractions.
 Vector3 previousVelocity, acceleration, lean, tilt;
 float distance;
 bool primed;

 public void Reset(){primed=false;distance=0;previousVelocity=acceleration=lean=tilt=Vector3.zero;Offset=Angles=RoadOffset=RoadAngles=Contacts=Vector3.zero;}
 public void Tick(float dt,Vector3 velocity,Quaternion rotation,Vector3 contacts){
  if(dt<=0)return;
  float speed=velocity.magnitude;
  var change=primed?Quaternion.Inverse(rotation)*(velocity-previousVelocity)/dt:Vector3.zero;
  previousVelocity=velocity;primed=true;
  change.y=0;change=Vector3.ClampMagnitude(change,15);if(speed<.2f)change=Vector3.zero;
  acceleration=Vector3.Lerp(acceleration,change,1-Mathf.Exp(-dt/ .14f));
  Contacts=Vector3.Lerp(Contacts,contacts,1-Mathf.Exp(-dt/ .06f));
  float blend=1-Mathf.Exp(-dt/ .12f);
  lean=Vector3.Lerp(lean,new Vector3(-acceleration.x*.002f,0,-acceleration.z*.0025f),blend);
  tilt=Vector3.Lerp(tilt,new Vector3(-acceleration.z*.09f,0,acceleration.x*.07f),blend);
  distance=Mathf.Repeat(distance+speed*dt,1000);
  float rolling=Mathf.InverseLerp(.5f,12,speed);
  float rough=(Contacts.x*.0012f+Contacts.y*.009f+Contacts.z*.014f)*rolling;
  float wave=Mathf.Sin(distance*2*Mathf.PI/2.5f)*.65f+Mathf.Sin(distance*2*Mathf.PI/ .8f)*.35f;
  RoadOffset=new Vector3(Mathf.Sin(distance*2*Mathf.PI/1.25f)*rough*.22f,wave*rough,0);
  RoadAngles=new Vector3(wave*rough*24,0,Mathf.Sin(distance*2*Mathf.PI/5)*rough*32);
  Offset=Vector3.ClampMagnitude(lean+RoadOffset,.045f);Angles=tilt+RoadAngles;
 }
 public static Surface SurfaceAt(TrackData track,Vector3 point,bool training){
  if(training)return DrivingSurface.OnTrainingAsphalt(point.x,point.z)?Surface.Asphalt:Surface.Loose;
  var sample=track.Sample(point.x,point.z);
  if(sample.OnRoad)return Surface.Asphalt;
  float outside=Mathf.Abs(sample.Offset)-sample.Width;
  if(outside>=.02f&&outside<=.02f+TrackLandmarks.KerbWidth)
   foreach(var run in TrackLandmarks.KerbRuns)
    if(sample.Segment>=run.First&&sample.Segment<run.Last&&Mathf.Sign(sample.Offset)==run.Side)return Surface.Kerb;
  return Surface.Loose;
 }
 public static Vector3 Weight(Surface surface)=>surface==Surface.Asphalt?Vector3.right:surface==Surface.Kerb?Vector3.up:Vector3.forward;
}
