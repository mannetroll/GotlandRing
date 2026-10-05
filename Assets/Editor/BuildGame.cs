using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
public static class BuildGame {
 public static void Build() {
  TrackImportChecks.Run();
  PlayerSettings.colorSpace=ColorSpace.Linear;
  foreach(var path in System.IO.Directory.GetFiles("Assets/Resources/Visuals","*.jpg")){
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)continue;importer.maxTextureSize=2048;importer.anisoLevel=8;
   if(path.Contains("Normal"))importer.textureType=TextureImporterType.NormalMap;
   importer.SaveAndReimport();
  }
  if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Visuals/Glass.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Gotland/AutomotiveGlass")),"Assets/Resources/Visuals/Glass.mat");
  var pinePath="Assets/Resources/Visuals/Pine.mat";var pine=AssetDatabase.LoadAssetAtPath<Material>(pinePath);if(!pine){pine=new Material(Shader.Find("Gotland/Pine"));AssetDatabase.CreateAsset(pine,pinePath);}pine.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Visuals/Pine.png");EditorUtility.SetDirty(pine);
  var skyPath="Assets/Resources/Visuals/PhotographicSky.mat";
  var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
  if(!sky){sky=new Material(Shader.Find("Skybox/Panoramic"));AssetDatabase.CreateAsset(sky,skyPath);}
  sky.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Visuals/Sky.hdr"));sky.SetFloat("_Exposure",.8f);sky.SetFloat("_Rotation",145);
  EditorUtility.SetDirty(sky);
  PlayerSettings.bundleVersion="0.1.2";
  PlayerSettings.companyName="Mannetroll Solutions AB"; PlayerSettings.productName="Gotland Ring - Impreza";
  PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Artwork/ImprezaIcon.png")});
  PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=900;
  PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.runInBackground=false;
  PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
  PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
  PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{UnityEngine.Rendering.GraphicsDeviceType.Direct3D11});
  if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/DrivingMaterial.mat")) AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")),"Assets/Resources/DrivingMaterial.mat");
  AssetDatabase.SaveAssets();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  new GameObject("Gotland Ring").AddComponent<RingDrive>();
  System.IO.Directory.CreateDirectory("Assets/Scenes");
  EditorSceneManager.SaveScene(scene,"Assets/Scenes/Gotland.unity");
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Gotland.unity"},locationPathName="Build/GotlandRing.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
  if(report.summary.result!=BuildResult.Succeeded) throw new System.Exception("Build failed: "+report.summary.result);
  foreach(var file in new[]{"README_CSV.md","gotland_ring_full_centerline_3m.csv","gotland_ring_validation.png"})System.IO.File.Copy(file,System.IO.Path.Combine("Build",file),true);
  Debug.Log("GOTLAND_BUILD_SUCCESS "+report.summary.totalSize);
 }
}
