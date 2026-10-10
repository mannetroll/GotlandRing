using UnityEngine;

public sealed class CockpitInstruments : MonoBehaviour
{
 public Renderer screen;
 public int materialSlot;
 public Vector4 uvRect;
 Material display;
 static readonly int Rpm=Shader.PropertyToID("_Rpm"),Speed=Shader.PropertyToID("_Speed"),Gear=Shader.PropertyToID("_Gear");

 void Awake(){
  display=new Material(Resources.Load<Shader>("Visuals/CockpitInstruments")){name="Live cockpit LCD"};
  display.SetVector("_UvRect",uvRect);
  var materials=screen.sharedMaterials;materials[materialSlot]=display;screen.sharedMaterials=materials;
  Set(900,0,1);
 }
 public void Set(float rpm,float metresPerSecond,int gear){
  display.SetFloat(Rpm,rpm);display.SetFloat(Speed,metresPerSecond*3.6f);display.SetFloat(Gear,gear);
 }
 void OnDestroy(){Destroy(display);}
}
