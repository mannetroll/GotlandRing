using System;
using UnityEngine;

[Serializable]
public sealed class DrivingSettings
{
 public float grip=28, lateralGrip=38, steering=42, highSpeedSteering=19, response=5, acceleration=1, braking=12, offRoadGrip=8;
 public bool awdMode=true, awdTractionControl=true;
 public float cockpitMovement=.5f, surfaceSound=.65f;
 public float awdGrip=1, awdFrontTorque=.5f, awdSteeringRate=90;
 const string Key="DrivingDynamics.v1";
 public static DrivingSettings Load(){try{var s=new DrivingSettings();JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(Key,"{}"),s);s.Clamp();return s;}catch{return new DrivingSettings();}}
 // Gravel resistance builds with motion so a stopped car can pull away.
 public static float ArcadeDrag(float speed,bool road)=>.10f+speed*speed*.0012f+(road?0:Mathf.Min(2.5f,speed*.4f));
 public float SteeringLimit(float speed)=>Mathf.Lerp(steering,highSpeedSteering,Mathf.Clamp01(Mathf.Abs(speed)/65f));
 public DrivingSettings Copy()=> (DrivingSettings)MemberwiseClone();
 public void Clamp(){cockpitMovement=Mathf.Clamp01(cockpitMovement);surfaceSound=Mathf.Clamp01(surfaceSound);grip=Mathf.Clamp(grip,10,40);lateralGrip=Mathf.Clamp(lateralGrip,10,60);steering=Mathf.Clamp(steering,25,55);highSpeedSteering=Mathf.Clamp(highSpeedSteering,8,25);response=Mathf.Clamp(response,1,9);acceleration=Mathf.Clamp(acceleration,.5f,1.8f);braking=Mathf.Clamp(braking,6,20);offRoadGrip=Mathf.Clamp(offRoadGrip,3,12);awdGrip=Mathf.Clamp(awdGrip,.7f,1.3f);awdFrontTorque=Mathf.Clamp(awdFrontTorque,.1f,.9f);awdSteeringRate=Mathf.Clamp(awdSteeringRate,40,150);}
 public void Save(){Clamp();PlayerPrefs.SetString(Key,JsonUtility.ToJson(this));PlayerPrefs.Save();}
}
