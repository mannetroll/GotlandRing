using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
public static class BuildGame {
 public static void Build() {
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
  Debug.Log("GOTLAND_BUILD_SUCCESS "+report.summary.totalSize);
 }
}
