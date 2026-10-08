using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public partial class RingDrive
{
 IEnumerator WindTest(){
  Application.runInBackground=true;
  Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
  yield return null;
  var towers=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Wind turbine tower").ToArray();
  var towerBounds=towers.Select(r=>r.bounds).ToArray();
  var positions=turbineRotors.Select(r=>r.position).ToArray();
  var angles=turbineRotors.Select(r=>r.localEulerAngles.z).ToArray();
  var blades=turbineRotors.SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>()).Where(r=>r.name=="Wind turbine blade").ToArray();
  try{
   if(turbineSites.Count!=12||towers.Length!=12||blades.Length!=36)throw new Exception("Missing registered turbine geometry");
   foreach(var turbine in windTurbines){
    int i=turbine.Index;var site=turbineSites[i];var rotor=turbineRotors[i];
    if(site.position!=turbine.Position||!site.name.Contains(turbine.Id)||Mathf.Abs(rotor.position.y-turbine.HubY)>.002f)throw new Exception("Registered base/hub placement changed: "+turbine.Id);
    if(Vector3.Dot(-rotor.forward,Vector3.left)<.999f)throw new Exception("Rotor front does not face west: "+turbine.Id);
    var tower=towers.Single(r=>Mathf.Abs(r.bounds.center.x-turbine.Position.x)<.01f&&Mathf.Abs(r.bounds.center.z-turbine.Position.z)<.01f);
    if(Mathf.Abs(tower.bounds.min.y-turbine.Position.y)>.005f||Mathf.Abs(tower.bounds.max.y-turbine.HubY)>.005f)throw new Exception("Tower height differs from registry: "+turbine.Id);
    foreach(var mesh in rotor.GetComponentsInChildren<MeshFilter>().Where(m=>m.name=="Wind turbine blade")){
     float radius=mesh.sharedMesh.vertices.Max(v=>new Vector2(v.x,v.y).magnitude);
     if(Mathf.Abs(radius-turbine.Radius)>.002f||Mathf.Abs(rotor.position.y+radius-turbine.RotorTopY)>.002f)throw new Exception("Rotor diameter/tip height differs from registry: "+turbine.Id);
    }
   }
  }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
  head.position=turbineRotors[3].position+turbineSites[3].TransformDirection(new Vector3(35,-25,-155));
  head.rotation=Quaternion.LookRotation(turbineRotors[3].position-Vector3.up*14-head.position);
  float started=Time.time;CaptureScreenshot("wind-rotor-0.png");
  yield return new WaitForSeconds(1);
  try{
   foreach(var blade in blades){
    if(blade.isPartOfStaticBatch)throw new Exception("Moving blade was included in static scenery batching");
    var localCenter=blade.GetComponent<MeshFilter>().sharedMesh.bounds.center;
    if(Vector3.Distance(blade.bounds.center,blade.transform.TransformPoint(localCenter))>.01f)
     throw new Exception("Rendered blade does not orbit its shared hub");
   }
   for(int i=0;i<turbineRotors.Count;i++){
    float delta=Mathf.DeltaAngle(angles[i],turbineRotors[i].localEulerAngles.z);
    if(Mathf.Abs(delta+WindTurbineRpm*6*(Time.time-started))>1.5f||turbineRotors[i].position!=positions[i])throw new Exception("Rotor speed, direction or hub position changed");
   }
   for(int i=0;i<towers.Length;i++)if(towers[i].bounds!=towerBounds[i])throw new Exception("Tower moved with the blades");
  }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
  CaptureScreenshot("wind-rotor-1.png");yield return new WaitForSeconds(.25f);
  SetPaused(true);var rotations=turbineRotors.Select(r=>r.rotation).ToArray();
  yield return new WaitForSeconds(.5f);
  try{
   for(int i=0;i<turbineRotors.Count;i++)if(Quaternion.Angle(rotations[i],turbineRotors[i].rotation)>.01f)throw new Exception("Paused turbine kept rotating");
  }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
  SetPaused(false);yield return new WaitForSeconds(.5f);
  try{
   for(int i=0;i<turbineRotors.Count;i++)if(Quaternion.Angle(rotations[i],turbineRotors[i].rotation)<25)throw new Exception("Turbine did not resume");
  }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
  foreach(int i in new[]{0,6,9}){
   var turbine=windTurbines[i];var hub=turbineRotors[i].position;float scale=turbine.HubHeight/78;
   head.position=hub+turbineSites[i].TransformDirection(new Vector3(45,-25,-170))*scale;head.rotation=Quaternion.LookRotation(hub-Vector3.up*(turbine.HubHeight*.2f)-head.position);
   yield return new WaitForSeconds(.5f);CaptureScreenshot($"wind-site-{i:00}.png");yield return new WaitForSeconds(.25f);
  }
  cam.orthographic=true;cam.orthographicSize=980;head.position=new Vector3(50,1500,-125);head.rotation=Quaternion.Euler(90,0,0);
  yield return new WaitForSeconds(.5f);CaptureScreenshot("wind-registry-overview.png");yield return new WaitForSeconds(.25f);
  Debug.Log($"WIND_TEST passed: {turbineRotors.Count} registered sites / {blades.Length} blades, CSV base/hub/tip positions, model dimensions, west-facing rotors, clockwise {WindTurbineRpm:0.0} RPM, fixed hubs/towers, dynamic renderer bounds, static-batch exclusion, pause/resume");
  Application.Quit();
 }
}
