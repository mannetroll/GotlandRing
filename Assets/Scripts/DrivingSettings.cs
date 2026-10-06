using System;
using UnityEngine;

[Serializable]
public sealed class DrivingSettings
{
 public float grip=28, lateralGrip=38, steering=42, highSpeedSteering=19, response=5, acceleration=1, braking=12, offRoadGrip=8;
 const string Key="DrivingDynamics.v1";
 public static DrivingSettings Load(){try{var s=JsonUtility.FromJson<DrivingSettings>(PlayerPrefs.GetString(Key,""))??new DrivingSettings();s.Clamp();return s;}catch{return new DrivingSettings();}}
 public DrivingSettings Copy()=> (DrivingSettings)MemberwiseClone();
 public void Clamp(){grip=Mathf.Clamp(grip,10,40);lateralGrip=Mathf.Clamp(lateralGrip,10,60);steering=Mathf.Clamp(steering,25,55);highSpeedSteering=Mathf.Clamp(highSpeedSteering,8,25);response=Mathf.Clamp(response,1,9);acceleration=Mathf.Clamp(acceleration,.5f,1.8f);braking=Mathf.Clamp(braking,6,20);offRoadGrip=Mathf.Clamp(offRoadGrip,3,12);}
 public void Save(){Clamp();PlayerPrefs.SetString(Key,JsonUtility.ToJson(this));PlayerPrefs.Save();}
}
