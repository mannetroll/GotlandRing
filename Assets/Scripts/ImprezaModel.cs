using UnityEngine;

public sealed class ImprezaModel : MonoBehaviour
{
 // Visual steering ratio: 42 degrees at the road wheels gives 504 at the rim.
 public const float SteeringRatio=12;
 public Transform steeringWheel;
 public DriverSteering driverSteering;
 public Transform[] frontSteering;
 public Transform[] wheelSpin;
 public Renderer cockpitHead;
 public Transform cockpitView;
 public Transform bonnetView;
 public float wheelRadius;
 public Material bodyPaint;
 public string displayName="Impreza Rally";
 public string credit="Rally Car: SpatialNeglect / CC BY-NC 4.0";
 public Color[] paintColors={Color.white,new Color(.95f,.035f,.055f),new Color(.035f,.19f,.95f)};
 public string[] paintNames={"White","Splash red","Rally blue"};
 public int initialPaint=1;
 Material paintInstance;
 int paintIndex;
 public string PaintName=>paintNames[paintIndex];
 public bool CanChangePaint=>paintColors.Length>1;
 Quaternion steeringRest;
 float roll;
 Vector3[] hubPositions;
 float[] wheelAngles;

 void Awake(){
  hubPositions=new Vector3[wheelSpin.Length];
  wheelAngles=new float[wheelSpin.Length];
  for(int i=0;i<wheelSpin.Length;i++)hubPositions[i]=wheelSpin[i].parent.localPosition;
  steeringRest=steeringWheel.localRotation;
  paintIndex=initialPaint;
  paintInstance=new Material(bodyPaint){color=paintColors[paintIndex]};
  foreach(var renderer in GetComponentsInChildren<MeshRenderer>()){
   var materials=renderer.sharedMaterials;bool changed=false;
   for(int i=0;i<materials.Length;i++)if(materials[i]==bodyPaint){materials[i]=paintInstance;changed=true;}
   if(changed)renderer.sharedMaterials=materials;
  }
 }

 public void CyclePaint(){
  paintIndex=(paintIndex+1)%paintColors.Length;
  paintInstance.color=paintColors[paintIndex];
  Debug.Log("CAR_PAINT "+PaintName);
 }

 void OnDestroy(){Destroy(paintInstance);}

 public void AnimateSuspension(WheelCollider[] wheels,float dt){
  for(int i=0;i<wheels.Length;i++){
   wheels[i].GetWorldPose(out var position,out _);
   var hub=wheelSpin[i].parent;var local=hubPositions[i];
   local.y=hub.parent.InverseTransformPoint(position).y+wheelRadius-wheels[i].radius;
   hub.localPosition=local;
   wheelAngles[i]=Mathf.Repeat(wheelAngles[i]+wheels[i].rpm*6*dt,360);
   wheelSpin[i].localRotation=Quaternion.Euler(wheelAngles[i],0,0);
  }
 }

 public void ResetSuspension(){for(int i=0;i<wheelSpin.Length;i++)wheelSpin[i].parent.localPosition=hubPositions[i];}

 public void Animate(float roadWheelAngle,float forwardSpeed,float dt,bool cockpit)
 {
  steeringWheel.localRotation=steeringRest*Quaternion.AngleAxis(-roadWheelAngle*SteeringRatio,Vector3.forward);
  driverSteering.Pose(-roadWheelAngle*SteeringRatio);
  foreach(var pivot in frontSteering)pivot.localRotation=Quaternion.Euler(0,roadWheelAngle,0);
  roll=Mathf.Repeat(roll+forwardSpeed*dt/wheelRadius*Mathf.Rad2Deg,360);
  foreach(var pivot in wheelSpin)pivot.localRotation=Quaternion.Euler(roll,0,0);
  cockpitHead.enabled=!cockpit;
 }
}
