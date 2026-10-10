using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
public static class BuildGame {
 [MenuItem("Gotland Ring/Build Windows x64")]
 public static void Build() {
  PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
  PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{UnityEngine.Rendering.GraphicsDeviceType.Direct3D11});
  BuildPlayer(BuildTarget.StandaloneWindows64,"Build/Windows/GotlandRing.exe");
 }
 [MenuItem("Gotland Ring/Build macOS Apple Silicon")]
 public static void BuildMacOS() {
  EditorUserBuildSettings.SetPlatformSettings("OSXUniversal","Architecture","ARM64");
  PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX,false);
  PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX,new[]{UnityEngine.Rendering.GraphicsDeviceType.Metal});
  BuildPlayer(BuildTarget.StandaloneOSX,"Build/macOS/GotlandRing.app");
 }
 static void BuildPlayer(BuildTarget target,string outputPath) {
  TrackImportChecks.Run();
  WindTurbineImport.Prepare();
  EngineAudioChecks.Run();
  ForestImport.Prepare();
  RallyCarImport.Prepare();
  SubaruCarImport.Prepare();
  PlayerSettings.colorSpace=ColorSpace.Linear;
  foreach(var path in System.IO.Directory.GetFiles("Assets/Resources/Visuals","*.jpg")){
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)continue;importer.maxTextureSize=2048;importer.anisoLevel=8;
   if(path.Contains("Normal"))importer.textureType=TextureImporterType.NormalMap;
   importer.SaveAndReimport();
  }
  var pinePath="Assets/Resources/Visuals/Pine.mat";var pine=AssetDatabase.LoadAssetAtPath<Material>(pinePath);if(!pine){pine=new Material(Shader.Find("Gotland/Pine"));AssetDatabase.CreateAsset(pine,pinePath);}pine.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Visuals/Pine.png");EditorUtility.SetDirty(pine);
  var skyPath="Assets/Resources/Visuals/PhotographicSky.mat";
  var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
  if(!sky){sky=new Material(Shader.Find("Skybox/Panoramic"));AssetDatabase.CreateAsset(sky,skyPath);}
  sky.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Visuals/Sky.hdr"));sky.SetFloat("_Exposure",.8f);sky.SetFloat("_Rotation",145);
  EditorUtility.SetDirty(sky);
  PlayerSettings.bundleVersion="v0.3.0";
  PlayerSettings.companyName="Mannetroll Solutions AB"; PlayerSettings.productName="Gotland Ring - Impreza";
  PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Artwork/ImprezaIcon.png")});
  PlayerSettings.SplashScreen.show=true;
  PlayerSettings.SplashScreen.showUnityLogo=false;
  PlayerSettings.SplashScreen.animationMode=PlayerSettings.SplashScreen.AnimationMode.Static;
  PlayerSettings.SplashScreen.backgroundColor=Color.black;
  PlayerSettings.SplashScreen.overlayOpacity=0;
  PlayerSettings.SplashScreen.logos=new[]{PlayerSettings.SplashScreenLogo.Create(2.5f,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Artwork/ImprezaIcon.png"))};
  PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=900;
  PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.resizableWindow=true; PlayerSettings.runInBackground=false;
  PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
  if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/DrivingMaterial.mat")) AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")),"Assets/Resources/DrivingMaterial.mat");
  AssetDatabase.SaveAssets();
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  new GameObject("Gotland Ring").AddComponent<RingDrive>();
  System.IO.Directory.CreateDirectory("Assets/Scenes");
  EditorSceneManager.SaveScene(scene,"Assets/Scenes/Gotland.unity");
  var outputDirectory=System.IO.Path.GetDirectoryName(outputPath);
  System.IO.Directory.CreateDirectory(outputDirectory);
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Gotland.unity"},locationPathName=outputPath,target=target,options=BuildOptions.None});
  if(report.summary.result!=BuildResult.Succeeded) throw new System.Exception("Build failed: "+report.summary.result);
  foreach(var file in new[]{"README.md","LICENSE","DATA_LICENSES.md","THIRD_PARTY_NOTICES.md","TRACK.md","docs/ASSET-CREDITS.md","docs/ENGINE-AUDIO.md","docs/SCENERY.md","docs/AWD-PHYSICS.md","track/gotland_ring_full_centerline_3m_lowpass.csv","track/gotland_ring_full_surface_3m.csv","track/SURFACE_README.md","track/gotland_ring_surface_validation.png","track/gotland_ring_validation.png","track/gotland_ring_whole_lap_lowpass.png","windmills/gotland_ring_wind_turbines.csv","windmills/WIND_TURBINES_README.txt","windmills/gotland_ring_wind_turbines_map.png"}){
   var destination=System.IO.Path.Combine(outputDirectory,file);
   System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
   System.IO.File.Copy(file,destination,true);
  }
  Debug.Log("GOTLAND_BUILD_SUCCESS "+target+" "+outputPath+" "+report.summary.totalSize);
 }
}
