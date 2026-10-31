using UnityEngine;

public sealed class CockpitInstruments : MonoBehaviour
{
 public Renderer screen;
 public int materialSlot;
 Material display,overlay;
 float tripKm;
 public Material Display=>display;

 void Awake(){
  display=new Material(Resources.Load<Shader>("Visuals/CockpitInstruments")){name="Plesse live instruments"};
  display.SetTexture("_Face",Resources.Load<Texture2D>("Visuals/PlesseCluster"));
  var materials=screen.sharedMaterials;materials[materialSlot]=display;screen.sharedMaterials=materials;
  overlay=new Material(display){name="Plesse HUD instruments"};
  Set(900,0,1);
 }
 public void Set(float rpm,float metresPerSecond,int gear,float fuel=1){
  display.SetFloat("_Rpm",rpm);display.SetFloat("_Speed",metresPerSecond*3.6f);display.SetFloat("_Gear",gear);
  display.SetFloat("_Fuel",fuel);
 }
 public void Travel(float metres){tripKm+=Mathf.Abs(metres)/1000;display.SetFloat("_TripKm",tripKm);}
 public void Draw(Rect rect){
  if(Event.current.type!=EventType.Repaint)return;
  overlay.CopyPropertiesFromMaterial(display);overlay.SetInt("_ZTest",(int)UnityEngine.Rendering.CompareFunction.Always);
  Graphics.DrawTexture(rect,Texture2D.whiteTexture,overlay);
 }
 void OnDestroy(){Destroy(display);Destroy(overlay);}
}
