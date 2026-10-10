using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DriverSteeringImport
{
 public static DriverSteering Prepare(ImprezaModel car,MeshRenderer driver){
  var root=new GameObject("Driver steering rig").transform;root.SetParent(car.transform,false);
  var rig=root.gameObject.AddComponent<DriverSteering>();
  rig.wheelCentre=car.transform.InverseTransformPoint(car.steeringWheel.position);
  rig.wheelAxis=car.transform.InverseTransformDirection(car.steeringWheel.forward);
  rig.arms=new DriverSteering.Arm[2];var bones=new Transform[7];bones[0]=root;
  for(int side=0;side<2;side++){
   float X(float left){return side==0?left:-.794f-left;}
   var arm=new DriverSteering.Arm{shoulder=new Vector3(X(-.558f),.851f,-.222f),elbow=new Vector3(X(-.596f),.680f,-.046f),wrist=new Vector3(X(-.550f),.820f,.168f)};
   Transform Bone(string name,Vector3 position){var t=new GameObject((side==0?"Left ":"Right ")+name).transform;t.SetParent(root,false);t.localPosition=position;return t;}
   arm.upper=Bone("upper arm",arm.shoulder);arm.forearm=Bone("forearm",arm.elbow);arm.hand=Bone("hand",arm.wrist);
   rig.arms[side]=arm;bones[1+side*3]=arm.upper;bones[2+side*3]=arm.forearm;bones[3+side*3]=arm.hand;
  }
  var filter=driver.GetComponent<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name="DriverBody";
  var weights=new BoneWeight[mesh.vertexCount];
  // Bake the FBX scale into the skin so the mesh and bones share metre coordinates.
  var conversion=car.transform.worldToLocalMatrix*driver.transform.localToWorldMatrix;
  var vertices=mesh.vertices.Select(v=>conversion.MultiplyPoint3x4(v)).ToArray();
  mesh.vertices=vertices;mesh.normals=mesh.normals.Select(n=>conversion.MultiplyVector(n).normalized).ToArray();
  mesh.tangents=mesh.tangents.Select(t=>{var v=conversion.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;return new Vector4(v.x,v.y,v.z,t.w);}).ToArray();
  mesh.RecalculateBounds();
  float Blend(float a,float b,float value){return Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,value));}
  for(int i=0;i<vertices.Length;i++){
   var p=vertices[i];
   int side=p.x<-.397f?0:1;var arm=rig.arms[side];int bone=1+side*3;
   float sleeve=Mathf.Max(Blend(.115f,.190f,Mathf.Abs(p.x+.397f)),Blend(-.10f,0,p.z))*Blend(.60f,.655f,p.y);
   var upper=arm.elbow-arm.shoulder;var lower=arm.wrist-arm.elbow;
   float u=Mathf.Clamp01(Vector3.Dot(p-arm.shoulder,upper)/upper.sqrMagnitude);
   float f=Vector3.Dot(p-arm.elbow,lower)/lower.sqrMagnitude;
   float du=Vector3.Distance(p,arm.shoulder+upper*u),df=Vector3.Distance(p,arm.elbow+lower*Mathf.Clamp01(f));
   float along=du<df?u*upper.magnitude:upper.magnitude+f*lower.magnitude;
   float forearm=Blend(upper.magnitude-.055f,upper.magnitude+.055f,along),hand=Blend(.115f,.185f,p.z);
   weights[i]=new BoneWeight{boneIndex0=0,weight0=1-sleeve,boneIndex1=bone,weight1=sleeve*(1-forearm)*(1-hand),boneIndex2=bone+1,weight2=sleeve*forearm*(1-hand),boneIndex3=bone+2,weight3=sleeve*hand};
  }
  mesh.boneWeights=weights;mesh.bindposes=bones.Select(b=>b.worldToLocalMatrix*root.localToWorldMatrix).ToArray();
  var asset=filter.sharedMesh;EditorUtility.CopySerialized(mesh,asset);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(asset);
  var materials=driver.sharedMaterials;driver.name="Driver head anchor";
  var owner=new GameObject("driver");owner.transform.SetParent(root,false);
  UnityEngine.Object.DestroyImmediate(driver);UnityEngine.Object.DestroyImmediate(filter);
  var renderer=owner.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=asset;renderer.bones=bones;renderer.rootBone=root;renderer.sharedMaterials=materials;renderer.quality=SkinQuality.Bone4;
  var bounds=asset.bounds;bounds.Expand(.3f);renderer.localBounds=bounds;rig.body=renderer;
  rig.Pose(0);
  Debug.Log($"DRIVER_STEERING_IMPORT passed: {asset.vertexCount} vertices, two arms, seven bones");
  return rig;
 }
}
