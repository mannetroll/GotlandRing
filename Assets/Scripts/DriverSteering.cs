using System;
using UnityEngine;

public sealed class DriverSteering : MonoBehaviour
{
 [Serializable] public sealed class Arm {
  public Transform upper,forearm,hand;
  public Vector3 shoulder,elbow,wrist;
 }
 public Arm[] arms;
 public Vector3 wheelCentre,wheelAxis;
 public SkinnedMeshRenderer body;

 // Angle-driven poses also work when seeking a replay or switching car models.
 public void Pose(float wheelAngle){
  for(int i=0;i<arms.Length;i++){
   var arm=arms[i];float offset=i==0?20:-20;
   float cycle=Mathf.Floor((wheelAngle-offset)/150)*150;
   float release=Mathf.InverseLerp(cycle+60+offset,cycle+90+offset,wheelAngle);
   float gripAngle=wheelAngle-cycle-150*Mathf.SmoothStep(0,1,release);
   var rotation=Quaternion.AngleAxis(gripAngle,wheelAxis);
   float lift=.035f*Mathf.Pow(Mathf.Sin(release*Mathf.PI),2);
   var wrist=wheelCentre+rotation*(arm.wrist-wheelCentre)-wheelAxis*lift;
   var reach=wrist-arm.shoulder;float distance=reach.magnitude;var direction=reach/distance;
   float upper=Vector3.Distance(arm.shoulder,arm.elbow),lower=Vector3.Distance(arm.elbow,arm.wrist);
   float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
   var restDirection=(arm.wrist-arm.shoulder).normalized;
   var pole=Vector3.ProjectOnPlane(arm.elbow-arm.shoulder,restDirection);
   var bend=Vector3.ProjectOnPlane(pole,direction).normalized;
   var elbow=arm.shoulder+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
   arm.upper.localRotation=Quaternion.FromToRotation(arm.elbow-arm.shoulder,elbow-arm.shoulder);
   arm.forearm.localPosition=elbow;
   arm.forearm.localRotation=Quaternion.FromToRotation(arm.wrist-arm.elbow,wrist-elbow);
   arm.hand.localPosition=wrist;arm.hand.localRotation=rotation;
  }
 }
}
