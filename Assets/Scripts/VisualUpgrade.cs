using UnityEngine;
using UnityEngine.Rendering;

public static class VisualUpgrade
{
 public static void Surface(Material m,string texture,float gloss,float normal=.5f){
  m.color=Color.white;m.mainTexture=Resources.Load<Texture2D>("Visuals/"+texture);
  m.SetTexture("_BumpMap",Resources.Load<Texture2D>("Visuals/"+texture+"Normal"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",normal);m.SetFloat("_Glossiness",gloss);
 }
 public static void Lighting(Camera cam,Transform car){
  QualitySettings.antiAliasing=4;QualitySettings.shadowResolution=ShadowResolution.VeryHigh;QualitySettings.shadowCascades=4;QualitySettings.shadowDistance=180;
  cam.clearFlags=CameraClearFlags.Skybox;cam.allowHDR=true;cam.allowMSAA=true;
  var sky=Resources.Load<Material>("Visuals/PhotographicSky");RenderSettings.skybox=sky;
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.48f,.55f,.65f);RenderSettings.ambientEquatorColor=new Color(.32f,.35f,.36f);RenderSettings.ambientGroundColor=new Color(.16f,.15f,.12f);
  RenderSettings.fogColor=new Color(.64f,.70f,.73f);RenderSettings.fogDensity=.00016f;
  var sun=Object.FindFirstObjectByType<Light>();sun.intensity=1.05f;sun.color=new Color(1,.95f,.86f);sun.transform.rotation=Quaternion.Euler(32,-48,0);sun.shadowBias=.02f;sun.shadowNormalBias=.15f;
  var probe=new GameObject("Local sky and circuit reflections").AddComponent<ReflectionProbe>();probe.transform.position=car.position+Vector3.up*2;
  probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;probe.resolution=128;probe.size=Vector3.one*10000;probe.nearClipPlane=3;probe.farClipPlane=1800;probe.clearFlags=ReflectionProbeClearFlags.Skybox;probe.RenderProbe();
 }
 // Irregular layered conifer crowns replace the spherical placeholder trees.
 public static void Pine(Vector3 p,float height,Material foliage,Material bark){
  var tree=GameObject.CreatePrimitive(PrimitiveType.Quad);tree.name="Coastal pine billboard";tree.transform.position=p+Vector3.up*height*.46f;tree.transform.localScale=new Vector3(height*.9f,height,1);tree.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Visuals/Pine");Object.Destroy(tree.GetComponent<Collider>());
 }
}
