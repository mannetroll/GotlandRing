using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SubaruCarImport
{
 const string Folder="Assets/Models/SubaruImpreza/";
 [Serializable] class Materials { public MaterialData[] materials; }
 [Serializable] class MaterialData {
  public string name,albedo,normal,emission,metallicSmoothness,alphaMode;
  public float[] color;public float metallic,roughness;
 }

 [MenuItem("Gotland Ring/Prepare Subaru Impreza prefab")]
 public static void Prepare(){
  var descriptions=JsonUtility.FromJson<Materials>(File.ReadAllText(Folder+"Source/materials.json"));
  Directory.CreateDirectory(Folder+"Materials");AssetDatabase.Refresh();
  var materials=descriptions.materials.ToDictionary(d=>d.name,MakeMaterial);
  var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Source/SubaruImpreza.fbx"));
  model.name="Subaru GLB exterior";
  var meshes=model.GetComponentsInChildren<MeshRenderer>(true);
  foreach(var r in meshes)r.sharedMaterials=r.sharedMaterials.Select(m=>materials[m.name]).ToArray();
  var root=new GameObject("Subaru Impreza");
  var visual=new GameObject("Exterior scale").transform;visual.SetParent(root.transform,false);model.transform.SetParent(visual,false);
  model.transform.localRotation=Quaternion.Euler(-90,0,0)*model.transform.localRotation;
  float nose=meshes.Single(r=>r.name=="headlight_headlights_0").bounds.center.z;
  float tail=meshes.Single(r=>r.name=="tail light_taillight_0").bounds.center.z;
  if(nose<tail)model.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;
  var bounds=meshes[0].bounds;foreach(var r in meshes)bounds.Encapsulate(r.bounds);
  float scale=4.32f/bounds.size.z;
  visual.localScale=Vector3.one*scale;
  visual.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);

  var controller=root.AddComponent<ImprezaModel>();
  controller.displayName="Subaru Impreza";controller.credit="Subaru Impreza: Mateusz Woliński / CC BY-NC 4.0";
  controller.bodyPaint=materials["livery"];
  controller.paintColors=new[]{Color.white};controller.paintNames=new[]{"Blue & gold"};controller.initialPaint=0;
  controller.frontSteering=new Transform[2];controller.wheelSpin=new Transform[4];
  string[] wheels={"Wheel.001","Wheel.005","Wheel.002","Wheel.004"};
  string[] suffix={"",".003",".001",".002"};
  for(int i=0;i<4;i++){
   var wheel=meshes.Single(r=>r.name==wheels[i]+"_wheel_0");
   var hub=new GameObject(wheels[i]+" steering").transform;hub.SetParent(root.transform,false);hub.position=wheel.bounds.center;
   if(i<2)controller.frontSteering[i]=hub;
   var spin=new GameObject(wheels[i]+" spin").transform;spin.SetParent(hub,false);controller.wheelSpin[i]=spin;
   wheel.transform.SetParent(spin,true);
   meshes.Single(r=>r.name=="Brake Disc"+suffix[i]+"_wheel_0").transform.SetParent(spin,true);
   meshes.Single(r=>r.name=="Brake Caliper"+suffix[i]+"_wheel_0").transform.SetParent(hub,true);
   if(i==0)controller.wheelRadius=wheel.bounds.extents.y;
  }
  // The supplied GLB has an exterior and cabin shell. Reuse this project's
  // detailed Impreza cockpit and driver at the same vehicle scale.
  var cabin=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RallyCar.prefab"),root.transform);
  cabin.name="Shared Impreza cockpit";var donor=cabin.GetComponent<ImprezaModel>();
  controller.steeringWheel=donor.steeringWheel;controller.cockpitHead=donor.cockpitHead;
  controller.cockpitView=donor.cockpitView;controller.bonnetView=donor.bonnetView;
  foreach(var spin in donor.wheelSpin)UnityEngine.Object.DestroyImmediate(spin.parent.gameObject);
  UnityEngine.Object.DestroyImmediate(donor);
  var interior=new HashSet<string>{"interior","buttons","lcd_screen","seat","driver","roll_cage"};
  foreach(var r in cabin.GetComponentsInChildren<MeshRenderer>(true))
   if(!r.sharedMaterials.All(m=>interior.Contains(m.name)))UnityEngine.Object.DestroyImmediate(r.gameObject);

  var final=meshes[0].bounds;foreach(var r in meshes)final.Encapsulate(r.bounds);
  if(meshes.Length!=67||Mathf.Abs(final.size.z-4.32f)>.01f||Mathf.Abs(final.min.y)>.01f||controller.wheelRadius<.3f||controller.wheelRadius>.36f)
   throw new InvalidOperationException($"Subaru exterior geometry: meshes={meshes.Length}, bounds={final}, radius={controller.wheelRadius}, importedBounds={bounds}, scale={scale}");
  if(controller.frontSteering.Any(t=>t.localPosition.z<0)||!controller.steeringWheel||!controller.cockpitHead)
   throw new InvalidOperationException("Subaru orientation or cockpit references are invalid");
  if(controller.wheelSpin.Any(t=>Mathf.Abs(t.position.y-controller.wheelRadius)>.02f))
   throw new InvalidOperationException("Subaru must be upright with all four tires on the ground");
  float radius=controller.wheelRadius;
  PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/SubaruImpreza.prefab");
  UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
  Debug.Log($"SUBARU_CAR_IMPORT passed: 67 exterior meshes, length={final.size.z:F3}m, radius={radius:F3}m, 12 materials, shared animated cockpit");
 }

 static Material MakeMaterial(MaterialData data){
  var path=Folder+"Materials/"+data.name+".mat";
  var material=new Material(Shader.Find("Standard")){name=data.name};
  material.color=new Color(data.color[0],data.color[1],data.color[2],data.color[3]);
  material.SetFloat("_Metallic",data.metallic);material.SetFloat("_Glossiness",1-data.roughness);
  if(!string.IsNullOrEmpty(data.albedo))material.mainTexture=Texture(data.albedo,false,false);
  if(!string.IsNullOrEmpty(data.normal)){material.SetTexture("_BumpMap",Texture(data.normal,true,true));material.EnableKeyword("_NORMALMAP");}
  if(!string.IsNullOrEmpty(data.metallicSmoothness)){material.SetTexture("_MetallicGlossMap",Texture(data.metallicSmoothness,false,true));material.SetFloat("_GlossMapScale",1);material.EnableKeyword("_METALLICGLOSSMAP");}
  if(!string.IsNullOrEmpty(data.emission)){material.SetTexture("_EmissionMap",Texture(data.emission,false,false));material.SetColor("_EmissionColor",Color.white*.15f);material.EnableKeyword("_EMISSION");}
  if(data.alphaMode=="BLEND"){
   material.SetFloat("_Mode",1);material.SetOverrideTag("RenderType","TransparentCutout");material.SetFloat("_Cutoff",.4f);
   material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;
  }
  if(data.name=="glass"){
   material.mainTexture=null;material.color=new Color(.8f,.9f,1,.16f);material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",.65f);
   material.SetTexture("_MetallicGlossMap",null);material.DisableKeyword("_METALLICGLOSSMAP");
   material.SetFloat("_Mode",3);material.SetOverrideTag("RenderType","Transparent");
   material.SetInt("_SrcBlend",(int)BlendMode.One);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);material.SetInt("_ZWrite",0);
   material.EnableKeyword("_ALPHAPREMULTIPLY_ON");material.renderQueue=3000;
  }
  var asset=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(asset){EditorUtility.CopySerialized(material,asset);UnityEngine.Object.DestroyImmediate(material);EditorUtility.SetDirty(asset);return asset;}
  AssetDatabase.CreateAsset(material,path);return material;
 }

 static Texture2D Texture(string relative,bool normal,bool linear){
  string path=Folder+relative;var importer=(TextureImporter)AssetImporter.GetAtPath(path);
  importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
  importer.sRGBTexture=!linear;importer.maxTextureSize=2048;importer.anisoLevel=4;importer.alphaSource=TextureImporterAlphaSource.FromInput;
  importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 }
}
