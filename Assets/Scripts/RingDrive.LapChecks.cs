using System;
using UnityEngine;

public partial class RingDrive
{
 void CheckLapTiming(){
  int start=TrackLandmarks.StartFinishPoint;
  RestartLap();lap=1;best=0;lapStart=0;
  for(int step=1;step<=track.Count;step++){
   nearest=(start+step)%track.Count;UpdateLap(step*.1f);
   if(lap!=(step==track.Count?2:1))throw new Exception("Lap counter crossed before completing the full route at Gutemålrakan");
  }
  if(Mathf.Abs(best-track.Count*.1f)>.001f||checkpoints!=0)throw new Exception("Start/finish timing did not record a complete lap");
  nearest=start-1;UpdateLap(300);nearest=start;UpdateLap(301);
  if(lap!=2)throw new Exception("Rocking across the line counted another lap");
  checkpoints=3;lastIndex=start+1;nearest=start-1;UpdateLap(302);
  if(lap!=2)throw new Exception("Reverse finish crossing counted a lap");
  RecoverCar(start);UpdateLap(303);
  if(lap!=2)throw new Exception("Recovery counted a finish crossing");
  RestartLap();lap=1;best=0;
  if(nearest!=start||checkpoints!=0||Vector3.Distance(car.position,track[start])>1)throw new Exception("Restart did not use Gutemålrakan");
  Debug.Log("LAP_TIMING_TEST passed: complete circuit, forward finish, reverse crossing, recovery, restart");
 }
}
