using System;
using System.Collections;
using UnityEngine;

public partial class RingDrive
{
 IEnumerator SceneryTest(){
  Application.runInBackground=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
  yield return null;
  try{
   var bounds=buildingParts[0].bounds;float buildingClearance=float.MaxValue,barrierClearance=float.MaxValue;
   foreach(var part in buildingParts){
    bounds.Encapsulate(part.bounds);
    foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1}){
     var p=part.bounds.center+Vector3.Scale(part.bounds.extents,new Vector3(x,0,z));
     var sample=centerline.Sample(p.x,p.z);buildingClearance=Mathf.Min(buildingClearance,sample.Distance-sample.Width);
     if(sample.OnRoad)throw new Exception("Main building geometry overlaps the road: "+part.name);
    }
   }
   var delta=bounds.center-windTurbines[2].Position;
   if(delta.z<75||delta.z>120||delta.x<35||delta.x>75||bounds.size.x<38||bounds.size.x>55)
    throw new Exception("Rendered main building does not match the mapped site north of turbine 2: "+bounds);
   foreach(var foot in barrierFeet){
    var surface=centerline.Sample(foot.x,foot.z);float margin=surface.Distance-surface.Width;
    barrierClearance=Mathf.Min(barrierClearance,margin);
    if(surface.OnRoad||margin<2.5f||Mathf.Abs(foot.y-SceneryGroundHeight(foot.x,foot.z))>.01f)
     throw new Exception($"Barrier clearance/grounding failed at {foot}: margin={margin:F3}");
   }
   foreach(var renderer in barrierRenderers){
    if(!renderer.sharedMaterial.shader.isSupported||!float.IsFinite(renderer.bounds.size.magnitude)||renderer.bounds.size.magnitude<1)
     throw new Exception("Invalid barrier rendering: "+renderer.name);
   }
   Debug.Log($"SCENERY_TEST passed: mapped main building north of turbine 2, hall/annex/roof, buildingClearance={buildingClearance:F2}m, barrierRuns={BarrierRuns.Length}, barrierClearance={barrierClearance:F2}m, ground contact, supported materials");
  }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
  head.position=mainBuilding.position+new Vector3(53,31,-75);head.rotation=Quaternion.LookRotation(mainBuilding.position+Vector3.up*3-head.position);
  yield return new WaitForSeconds(.8f);CaptureScreenshot("main-building.png");yield return new WaitForSeconds(.3f);
  cam.orthographic=true;cam.orthographicSize=140;head.position=mainBuilding.position+new Vector3(-20,240,-35);head.rotation=Quaternion.Euler(90,0,0);
  yield return new WaitForSeconds(.6f);CaptureScreenshot("main-building-turbine-2.png");yield return new WaitForSeconds(.3f);cam.orthographic=false;
  foreach(var shot in new[]{(235,-30f,"pit-catch-fence"),(444,-15f,"armco-barriers"),(1835,0f,"concrete-barriers")}){
   RecoverCar(shot.Item1);view=1;lookYaw=shot.Item2;lookPitch=0;UpdateCameraPose();carModel.Animate(0,0,0,0,false);
   yield return new WaitForSeconds(.6f);CaptureScreenshot(shot.Item3+".png");yield return new WaitForSeconds(.3f);
  }
  Application.Quit();
 }
}
