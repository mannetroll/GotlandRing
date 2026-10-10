using System.Collections.Generic;
using UnityEngine;

public partial class RingDrive
{
 // Visual estimates from the owner's 2022 film, not registry measurements.
 // The rotor front faces W (-X): scene wind arrives from the west, toward +X.
 static readonly Vector3 WindTurbineFacing=Vector3.left;
 const float WindTurbineRpm=10;
 readonly List<Transform> turbineRotors=new List<Transform>();
 readonly List<Transform> turbineSites=new List<Transform>();
 List<WindTurbineData> windTurbines;

 void MakeWindTurbines(){
  windTurbines=WindTurbineData.Read(Resources.Load<TextAsset>("Track/WindTurbines").text);
  foreach(var turbine in windTurbines){
   var site=new GameObject($"Turbine {turbine.Index:00} / {turbine.Id} / {turbine.Model}").transform;
   site.position=turbine.Position;site.rotation=Quaternion.LookRotation(-WindTurbineFacing,Vector3.up);turbineSites.Add(site);
   float size=turbine.RotorDiameter/66;
   var tower=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tower.name="Wind turbine tower";
   tower.transform.SetParent(site,false);tower.transform.localPosition=Vector3.up*(turbine.HubHeight*.5f);
   tower.transform.localScale=new Vector3(3*size,turbine.HubHeight*.5f,3*size);
   tower.GetComponent<Renderer>().sharedMaterial=white;Destroy(tower.GetComponent<Collider>());
   Box("Wind turbine nacelle",Vector3.up*turbine.HubHeight,new Vector3(4,3,6)*size,white,site);
   var rotor=new GameObject("Wind turbine rotor / "+turbine.Id).transform;
   rotor.SetParent(site,false);rotor.localPosition=Vector3.up*turbine.HubHeight-Vector3.forward*(4*size);
   rotor.localRotation=Quaternion.Euler(0,0,-turbine.Index*37);
   turbineRotors.Add(rotor);
   var bladeMesh=MakeWindBlade(turbine.Radius);
   for(int j=0;j<3;j++){
    var blade=new GameObject("Wind turbine blade");blade.transform.SetParent(rotor,false);
    blade.transform.localRotation=Quaternion.Euler(0,0,-j*120);
    blade.AddComponent<MeshFilter>().sharedMesh=bladeMesh;blade.AddComponent<MeshRenderer>().sharedMaterial=white;
   }
   var hub=GameObject.CreatePrimitive(PrimitiveType.Sphere);hub.name="Wind turbine hub";
   hub.transform.SetParent(rotor,false);hub.transform.localScale=Vector3.one*(3*size);
   hub.GetComponent<Renderer>().sharedMaterial=white;Destroy(hub.GetComponent<Collider>());
   MakeTurbineGround(turbine);
  }
  Debug.Log($"WIND_TURBINES count={windTurbines.Count} source=windmills/gotland_ring_wind_turbines.csv datum=RH2000 hubHeights=55/78/105m diameters=47/66/90m facing=W rpm={WindTurbineRpm:0.0} windSource=2022-film-estimate");
 }

 static Mesh MakeWindBlade(float radius){
  // Approximate blade profile; the tip's swept radius is the registered dimension.
  var profile=new[]{new Vector2(0,0),new Vector2(.022f,.12f),new Vector2(.038f,.28f),new Vector2(.008f,.8f),
   new Vector2(0,1),new Vector2(-.011f,.8f),new Vector2(-.024f,.2f),new Vector2(-.012f,.06f)};
  int n=profile.Length;var vertices=new Vector3[n*2];var triangles=new List<int>();
  for(int i=0;i<n;i++){
   vertices[i]=new Vector3(profile[i].x,profile[i].y,-.006f)*radius;
   vertices[i+n]=new Vector3(profile[i].x,profile[i].y,.006f)*radius;
   int j=(i+1)%n;triangles.AddRange(new[]{i,j,i+n,j,j+n,i+n});
  }
  for(int i=1;i<n-1;i++)triangles.AddRange(new[]{0,i+1,i,n,n+i,n+i+1});
  var mesh=new Mesh{name=$"Turbine blade / radius {radius:0.0}m",vertices=vertices,triangles=triangles.ToArray()};
  mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }

 float SceneryGroundHeight(float x,float z)=>quarry.GroundHeight(x,z);

 void MakeTurbineGround(WindTurbineData turbine){
  // Local scenery transitions support the supplied base elevation without
  // moving the turbine or modifying any road/apron vertices.
  var p=turbine.Position;float pad=turbine.RotorDiameter*.07f;
  float clearance=centerline.Nearest(p.x,p.z,out _,out _),roadMargin=0;
  foreach(var section in centerline.Sections)roadMargin=Mathf.Max(roadMargin,section.LeftWidth,section.RightWidth);
  float radius=Mathf.Min(Mathf.Max(16,(p.y-SceneryGroundHeight(p.x,p.z))*4+pad),clearance-roadMargin-TrackData.ApronWidth-2);
  const int rings=8,sides=48;var vertices=new Vector3[1+rings*sides];var uv=new Vector2[vertices.Length];var triangles=new List<int>();
  vertices[0]=p;uv[0]=new Vector2(p.x,p.z)/12;
  for(int ring=0;ring<rings;ring++)for(int side=0;side<sides;side++){
   float r=Mathf.Lerp(pad,radius,ring/(float)(rings-1)),angle=side*Mathf.PI*2/sides;
   var point=p+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*r;
   float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(pad,radius,r));
   point.y=Mathf.Lerp(SceneryGroundHeight(point.x,point.z)-.05f,p.y,blend);
   int a=1+ring*sides+side,b=1+ring*sides+(side+1)%sides;vertices[a]=point;uv[a]=new Vector2(point.x,point.z)/12;
   if(ring==0)triangles.AddRange(new[]{0,a,b});
   else triangles.AddRange(new[]{a-sides,a,b-sides,b-sides,a,b});
  }
  var mesh=new Mesh{name="Turbine terrain transition / "+turbine.Id,vertices=vertices,uv=uv,triangles=triangles.ToArray()};mesh.RecalculateNormals();
  var ground=new GameObject(mesh.name);ground.AddComponent<MeshFilter>().sharedMesh=mesh;ground.AddComponent<MeshRenderer>().sharedMaterial=grass;
 }

 void AnimateWindTurbines(float dt){
  // Clockwise from the rotor's front, as seen around
  // 02:39–02:41 in Gotland Ring 2022 - 1 of 1.mp4. Each rotor has its own phase.
  foreach(var rotor in turbineRotors)rotor.Rotate(Vector3.forward,-WindTurbineRpm*6*dt,Space.Self);
 }
}
