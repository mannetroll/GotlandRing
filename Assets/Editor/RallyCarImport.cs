using System;
using System.Collections.Generic;
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
  foreach(var name in new[]{"notes","co driver","seatbelts.001"})
   UnityEngine.Object.DestroyImmediate(model.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name==name).gameObject);
  var meshes=model.GetComponentsInChildren<MeshRenderer>(true);
  foreach(var r in meshes)
   r.sharedMaterials=r.sharedMaterials.Select(m=>{
    var path="Assets/Models/RallyCar/Materials/"+m.name.Replace(' ','_')+".mat";
    var material=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(!material)throw new InvalidOperationException("Missing rally car material: "+path);
    return material;
   }).ToArray();
  var driverHead=SplitDriver(meshes.Single(r=>r.name=="driver"));
  meshes=model.GetComponentsInChildren<MeshRenderer>(true);
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
  controller.cockpitHead=driverHead;
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
  if(meshes.Length!=89 || controller.wheelRadius<.3f || controller.wheelRadius>.36f)
   throw new InvalidOperationException("Unexpected rally car geometry or hierarchy");
  var final=meshes[0].bounds;foreach(var r in meshes)final.Encapsulate(r.bounds);
  if(Mathf.Abs(final.size.z-4.32f)>.01f || Mathf.Abs(final.min.y)>.01f)
   throw new InvalidOperationException("Rally car scale or ground contact is invalid");
  float radius=controller.wheelRadius;
  PrefabUtility.SaveAsPrefabAsset(root,Prefab);
  UnityEngine.Object.DestroyImmediate(root);
  AssetDatabase.SaveAssets();
  Debug.Log($"RALLY_CAR_IMPORT passed: 89 meshes, length={final.size.z:F3}m, radius={radius:F3}m, 18 mapped materials");
 }

 static MeshRenderer SplitDriver(MeshRenderer driver)
 {
  var filter=driver.GetComponent<MeshFilter>();var source=filter.sharedMesh;
  var vertices=source.vertices;var triangles=source.triangles;
  var groups=Enumerable.Range(0,vertices.Length).ToArray();
  int Find(int index){while(groups[index]!=index){groups[index]=groups[groups[index]];index=groups[index];}return index;}
  for(int i=0;i<triangles.Length;i+=3){groups[Find(triangles[i])]=Find(triangles[i+1]);groups[Find(triangles[i])]=Find(triangles[i+2]);}
  // Use whole vertex-index islands: the helmet's chin sits lower than the hands.
  // In the imported car's metre coordinates, the suit ends below 1.14 m.
  var headGroups=new HashSet<int>();
  for(int i=0;i<vertices.Length;i++)if(driver.transform.TransformPoint(vertices[i]).y>1.14f)headGroups.Add(Find(i));
  var headTriangles=new List<int>();var bodyTriangles=new List<int>();
  for(int i=0;i<triangles.Length;i+=3){
   var part=headGroups.Contains(Find(triangles[i]))?headTriangles:bodyTriangles;
   part.Add(triangles[i]);part.Add(triangles[i+1]);part.Add(triangles[i+2]);
  }
  if(headTriangles.Count==0 || bodyTriangles.Count==0)throw new InvalidOperationException("Driver head/body separation failed");
  filter.sharedMesh=SaveDriverPart(source,bodyTriangles,"DriverBody");
  var head=new GameObject("Driver head and helmet");head.transform.SetParent(driver.transform,false);
  head.AddComponent<MeshFilter>().sharedMesh=SaveDriverPart(source,headTriangles,"DriverHead");
  var renderer=head.AddComponent<MeshRenderer>();renderer.sharedMaterials=driver.sharedMaterials;
  Debug.Log($"DRIVER_PARTS body={bodyTriangles.Count/3} triangles, head={headTriangles.Count/3} triangles");
  return renderer;
 }

 static Mesh SaveDriverPart(Mesh source,List<int> triangles,string name)
 {
  var mesh=UnityEngine.Object.Instantiate(source);mesh.name=name;mesh.SetTriangles(triangles,0);
  var vertices=mesh.vertices;var bounds=new Bounds(vertices[triangles[0]],Vector3.zero);
  foreach(int index in triangles)bounds.Encapsulate(vertices[index]);mesh.bounds=bounds;
  string path="Assets/Models/RallyCar/"+name+".asset";
  var asset=AssetDatabase.LoadAssetAtPath<Mesh>(path);
  if(asset){EditorUtility.CopySerialized(mesh,asset);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(asset);return asset;}
  AssetDatabase.CreateAsset(mesh,path);return mesh;
 }
}
