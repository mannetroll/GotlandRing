using System;
using System.Linq;
using UnityEngine;

public partial class RingDrive
{
 void CheckDriverHands(){
  var rig=carModel.driverSteering;var body=rig.body;var baked=new Mesh();
  Vector3[] Bake(){body.BakeMesh(baked);return baked.vertices.Select(v=>rig.transform.InverseTransformPoint(body.transform.TransformPoint(v))).ToArray();}
  int Nearest(Vector3[] points,Vector3 point){return Enumerable.Range(0,points.Length).OrderBy(i=>(points[i]-point).sqrMagnitude).First();}
  carModel.Animate(0,0,0,true);var rest=Bake();
  int torso=Nearest(rest,new Vector3(-.397f,.58f,-.28f));
  var markers=new[]{Nearest(rest,new Vector3(-.535f,.849f,.238f)),Nearest(rest,new Vector3(-.245f,.849f,.238f))};
  var grips=rig.arms.Select((a,i)=>a.hand.InverseTransformPoint(rig.transform.TransformPoint(rest[markers[i]]))).ToArray();
  var previous=rig.arms.Select(a=>a.hand.localPosition).ToArray();
  float maxStep=0,minReachMargin=1;
  for(float angle=-720;angle<=720;angle+=.5f){
   carModel.Animate(-angle/ImprezaModel.SteeringRatio,0,0,true);int released=0;
   for(int i=0;i<2;i++){
    var arm=rig.arms[i];var hand=arm.hand.localPosition;var elbow=arm.forearm.localPosition;
    float reach=Vector3.Distance(hand,arm.shoulder),length=Vector3.Distance(arm.elbow,arm.shoulder)+Vector3.Distance(arm.wrist,arm.elbow);
    minReachMargin=Mathf.Min(minReachMargin,length-reach);
    if(reach>length+.0001f||Vector3.Distance(arm.upper.localPosition,arm.shoulder)>.0001f||
     Vector3.Distance(arm.upper.localPosition+arm.upper.localRotation*(arm.elbow-arm.shoulder),elbow)>.0001f||
     Vector3.Distance(elbow+arm.forearm.localRotation*(arm.wrist-arm.elbow),hand)>.0001f)
     throw new Exception("Driver arm detached or target out of reach");
    float radius=Vector3.ProjectOnPlane(hand-rig.wheelCentre,rig.wheelAxis).magnitude;
    float original=Vector3.ProjectOnPlane(arm.wrist-rig.wheelCentre,rig.wheelAxis).magnitude;
    float lift=Vector3.Dot(arm.wrist-hand,rig.wheelAxis);
    if(Mathf.Abs(radius-original)>.0001f||lift<-.0001f||lift>.0351f)throw new Exception("Hand leaves the steering rim");
    if(lift>.001f)released++;
    if(angle>-720)maxStep=Mathf.Max(maxStep,Vector3.Distance(previous[i],hand));previous[i]=hand;
   }
   if(released>1)throw new Exception("Both hands release the wheel together");
  }
  if(maxStep>.013f)throw new Exception("Hand animation jumps during a regrip");
  foreach(float angle in new[]{-42f,-19,-2,0,2,19,42,0}){
   carModel.Animate(angle,0,0,true);var pose=Bake();
   if(Vector3.Distance(rest[torso],pose[torso])>.0001f)throw new Exception("Steering moves the seated torso");
   for(int i=0;i<2;i++){
    var expected=rig.transform.InverseTransformPoint(rig.arms[i].hand.TransformPoint(grips[i]));
    if(Vector3.Distance(expected,pose[markers[i]])>.001f)throw new Exception("Visible glove does not follow its hand bone");
    if(Mathf.Abs(angle)==2&&Vector3.Distance(rest[markers[i]],pose[markers[i]])<.045f)throw new Exception("Visible hands stay still during steering");
   }
   carModel.Animate(angle,0,0,true);var pausedPose=Bake();
   if(pose.Where((p,i)=>Vector3.Distance(p,pausedPose[i])>.0001f).Any())throw new Exception("Driver animation drifts while paused");
   if(angle==0&&pose.Where((p,i)=>Vector3.Distance(p,rest[i])>.0001f).Any())throw new Exception("Driver fails to return to the rest pose");
  }
  Destroy(baked);
  Debug.Log($"DRIVER_HANDS_TEST passed: visible gloves, elbows, fixed torso, rim contact, alternating regrips, pause, seek/centre; maxStep={maxStep:F4}m reachMargin={minReachMargin:F3}m");
 }
}
