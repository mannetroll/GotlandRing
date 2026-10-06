using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RallyCarImport
{
 const string Source="Assets/Models/RallyCar/Source/Rally Car.fbx";
 const string Prefab="Assets/Resources/RallyCar.prefab";

 [MenuItem("Gotland Ring/Prepare rally car prefab")]
 public static void Prepare()
 {
  var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source));
  model.name="Rally car mesh";
  var meshes=model.GetComponentsInChildren<MeshRenderer>(true);
  foreach(var r in meshes)
   r.sharedMaterials=r.sharedMaterials.Select(m=>{
    var path="Assets/Models/RallyCar/Materials/"+m.name.Replace(' ','_')+".mat";
    var material=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(!material)throw new InvalidOperationException("Missing rally car material: "+path);
    return material;
   }).ToArray();
  var bounds=meshes[0].bounds;foreach(var r in meshes)bounds.Encapsulate(r.bounds);
  var root=new GameObject("Impreza rally car");
  var visual=new GameObject("Model scale").transform;visual.SetParent(root.transform,false);
  model.transform.SetParent(visual,true);
  float scale=4.32f/bounds.size.z;
  visual.localScale=Vector3.one*scale;
  visual.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
  var controller=root.AddComponent<ImprezaModel>();
  controller.bodyPaint=AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/RallyCar/Materials/livery.mat");
  var transforms=model.GetComponentsInChildren<Transform>(true);
  controller.steeringWheel=transforms.Single(t=>t.name=="Wheel");
  controller.cockpitHidden=meshes.Where(r=>r.name=="driver"||r.name=="seatbelts").Cast<Renderer>().ToArray();
  controller.frontSteering=new Transform[2];controller.wheelSpin=new Transform[4];
  string[] wheels={"Wheel.001","Wheel.005","Wheel.002","Wheel.004"};
  string[] suffix={"",".003",".001",".002"};
  string[] corners={"Front left","Front right","Rear left","Rear right"};
  for(int i=0;i<wheels.Length;i++)
  {
   var wheel=meshes.Single(r=>r.name==wheels[i]);
   var hub=new GameObject(corners[i]+" steering").transform;hub.SetParent(root.transform,false);hub.position=wheel.bounds.center;
   if(i<2)controller.frontSteering[i]=hub;
   var spin=new GameObject(corners[i]+" spin").transform;spin.SetParent(hub,false);controller.wheelSpin[i]=spin;
   wheel.transform.SetParent(spin,true);
   meshes.Single(r=>r.name=="Brake Disc"+suffix[i]).transform.SetParent(spin,true);
   meshes.Single(r=>r.name=="Brake Caliper"+suffix[i]).transform.SetParent(hub,true);
   if(i==0)controller.wheelRadius=wheel.bounds.extents.y;
  }
  controller.cockpitView=new GameObject("Cockpit camera").transform;
  controller.cockpitView.SetParent(root.transform,false);
  controller.cockpitView.position=visual.TransformPoint(new Vector3(-.46f,1.43f,.08f));
  controller.cockpitView.localRotation=Quaternion.Euler(8.45f,0,0);
  controller.bonnetView=new GameObject("Bonnet camera").transform;
  controller.bonnetView.SetParent(root.transform,false);
  controller.bonnetView.localPosition=new Vector3(0,1.02f,1.02f);
  controller.bonnetView.localRotation=Quaternion.Euler(2,0,0);
  if(meshes.Length!=91 || controller.cockpitHidden.Length!=2 || controller.wheelRadius<.3f || controller.wheelRadius>.36f)
   throw new InvalidOperationException("Unexpected rally car geometry or hierarchy");
  var final=meshes[0].bounds;foreach(var r in meshes)final.Encapsulate(r.bounds);
  if(Mathf.Abs(final.size.z-4.32f)>.01f || Mathf.Abs(final.min.y)>.01f)
   throw new InvalidOperationException("Rally car scale or ground contact is invalid");
  float radius=controller.wheelRadius;
  PrefabUtility.SaveAsPrefabAsset(root,Prefab);
  UnityEngine.Object.DestroyImmediate(root);
  AssetDatabase.SaveAssets();
  Debug.Log($"RALLY_CAR_IMPORT passed: 91 meshes, length={final.size.z:F3}m, radius={radius:F3}m, 19 mapped materials");
 }
}
