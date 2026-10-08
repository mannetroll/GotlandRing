using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class RingDrive
{
 Transform mainBuilding;
 readonly List<Renderer> buildingParts=new List<Renderer>();
 readonly List<Vector3> barrierFeet=new List<Vector3>();
 readonly List<MeshRenderer> barrierRenderers=new List<MeshRenderer>();
 enum BarrierKind { Armco,Concrete,CatchFence }
 // Approximate runs matched to the 2022 film: the northern pit straight and
 // older loop use mesh/steel; the southern extension uses pale concrete.
 static readonly (int Start,int End,int Side,float Setback,BarrierKind Kind)[] BarrierRuns={
  (176,294,-1,3.8f,BarrierKind.CatchFence),
  (294,365,-1,4.5f,BarrierKind.Armco),(286,358,1,4.5f,BarrierKind.Armco),
  (423,516,-1,6,BarrierKind.Armco),(540,674,1,5,BarrierKind.Armco),
  (748,874,-1,4.5f,BarrierKind.Armco),(970,1135,1,5,BarrierKind.Armco),
  (1240,1485,-1,4.5f,BarrierKind.Concrete),(1290,1635,1,3.5f,BarrierKind.Concrete),
  (1550,1760,-1,3.5f,BarrierKind.Concrete),(1690,1930,1,3.5f,BarrierKind.Concrete),
  (1810,2150,-1,4.5f,BarrierKind.Concrete),(1960,2175,1,4,BarrierKind.Concrete),
  (2200,2300,-1,4.5f,BarrierKind.Concrete)
 };

 void MakeMainBuilding(){
  // Roof centre and orientation digitized from the supplied turbine map,
  // transformed from its SWEREF grid into the track's east/north frame.
  mainBuilding=new GameObject("GotlandRing main building / north of turbine 2").transform;
  mainBuilding.position=new Vector3(-12.1f,windTurbines[2].Position.y+.1f,125.7f);
  mainBuilding.rotation=Quaternion.Euler(0,-7.25f,0);
  var walls=Mat("Main building pale render",new Color(.77f,.78f,.73f),.12f);
  var roof=Mat("Main building slate metal roof",new Color(.16f,.22f,.24f),.3f);
  var glazing=Mat("Main building shaded glazing",new Color(.08f,.15f,.19f),.72f);
  GameObject Part(string name,Vector3 p,Vector3 size,Material material){
   var part=Box("Main building / "+name,p,size,material,mainBuilding);buildingParts.Add(part.GetComponent<Renderer>());return part;
  }
  Part("hall",new Vector3(0,2.7f,0),new Vector3(38,5.4f,16),walls);
  Part("foundation",new Vector3(0,.15f,0),new Vector3(38.5f,.3f,16.5f),white);
  for(int side=-1;side<=1;side+=2){
   var panel=Part("pitched roof",new Vector3(0,6.45f,side*4.3f),new Vector3(39,.22f,8.87f),roof);
   panel.transform.localRotation=Quaternion.Euler(side*14.4f,0,0);
   for(int i=0;i<40;i++){
    var rib=Part("roof seam",new Vector3(i-19.5f,6.58f,side*4.3f),new Vector3(.035f,.035f,8.87f),roof);
    rib.transform.localRotation=panel.transform.localRotation;
   }
   Part("eaves",new Vector3(0,5.3f,side*8.5f),new Vector3(39,.22f,.18f),roof);
  }
  Part("ridge",new Vector3(0,7.57f,0),new Vector3(39,.15f,.28f),roof);
  // Close the two gable ends rather than leaving open triangles under the roof.
  foreach(int side in new[]{-1,1}){
   var vertices=new[]{new Vector3(side*19,5.4f,-8),new Vector3(side*19,7.5f,0),new Vector3(side*19,5.4f,8)};
   var mesh=new Mesh{name="Main building gable",vertices=vertices,triangles=side>0?new[]{0,1,2}:new[]{0,2,1}};mesh.RecalculateNormals();
   var gable=new GameObject("Main building / gable");gable.transform.SetParent(mainBuilding,false);
   gable.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=gable.AddComponent<MeshRenderer>();renderer.sharedMaterial=walls;buildingParts.Add(renderer);
  }
  for(int i=0;i<6;i++){
   float x=i*6-15;
   Part("front glazed bay",new Vector3(x,2.2f,-8.04f),new Vector3(5.2f,3.3f,.08f),glazing);
   Part("bay mullion",new Vector3(x,2.2f,-8.10f),new Vector3(.08f,3.35f,.07f),roof);
   Part("bay transom",new Vector3(x,2.9f,-8.10f),new Vector3(5.2f,.08f,.07f),roof);
  }
  Part("north connector",new Vector3(-7,2.2f,11),new Vector3(9,4.4f,8),walls);
  Part("north annex",new Vector3(-9.1f,2,20.4f),new Vector3(24,4,16),walls);
  Part("annex roof",new Vector3(-9.1f,4.15f,20.4f),new Vector3(24.8f,.3f,16.8f),roof);
  Part("entrance canopy",new Vector3(-13,3.9f,-10),new Vector3(8,.22f,4),roof);
  foreach(float x in new[]{-16.6f,-9.4f})Part("canopy post",new Vector3(x,1.95f,-11.6f),new Vector3(.12f,3.9f,.12f),silver);
  Part("name board",new Vector3(1,4.5f,-8.12f),new Vector3(19,.9f,.1f),roof);
  var label=new GameObject("Main building / GOTLANDRING").AddComponent<TextMesh>();label.transform.SetParent(mainBuilding,false);
  label.transform.localPosition=new Vector3(1,4.5f,-8.19f);label.font=trackSignFont;label.fontSize=96;label.characterSize=1;
  label.anchor=TextAnchor.MiddleCenter;label.text="GOTLANDRING";label.color=Color.white;label.GetComponent<MeshRenderer>().sharedMaterial=trackSignTextMaterial;
  var textBounds=label.GetComponent<MeshRenderer>().localBounds;
  label.transform.localScale=Vector3.one*Mathf.Min(17/textBounds.size.x,.65f/textBounds.size.y);
  MakeBuildingForecourt();
  Debug.Log($"MAIN_BUILDING map=windmills/gotland_ring_wind_turbines_map.png position={mainBuilding.position} turbine2Offset={mainBuilding.position-windTurbines[2].Position} hall=38x16m heading=82.75deg footprint=visual-estimate");
 }

 void MakeBuildingForecourt(){
  // A level paved pad joins the approximate terrain with a sloped gravel skirt.
  var inner=new[]{new Vector3(-29,0,-22),new Vector3(-29,0,32),new Vector3(27,0,32),new Vector3(27,0,-22)};
  var vertices=new Vector3[8];var uv=new Vector2[8];
  for(int i=0;i<4;i++){
   vertices[i]=mainBuilding.TransformPoint(inner[i]);
   var outer=mainBuilding.TransformPoint(inner[i]+new Vector3(inner[i].x<0?-12:12,0,inner[i].z<0?-12:12));
   outer.y=SceneryGroundHeight(outer.x,outer.z)-.05f;vertices[i+4]=outer;
  }
  for(int i=0;i<8;i++)uv[i]=new Vector2(vertices[i].x,vertices[i].z)/12;
  var mesh=new Mesh{name="Main building forecourt and terrain skirt",vertices=vertices,uv=uv,subMeshCount=2};
  mesh.SetTriangles(new[]{0,1,2,0,2,3},0);var skirt=new List<int>();
  for(int i=0;i<4;i++){int next=(i+1)%4;skirt.AddRange(new[]{i,i+4,next,i+4,next+4,next});}
  mesh.SetTriangles(skirt,1);mesh.RecalculateNormals();
  var ground=new GameObject(mesh.name);ground.AddComponent<MeshFilter>().sharedMesh=mesh;
  var gravel=Mat("Paddock limestone gravel",new Color(.58f,.57f,.49f));VisualUpgrade.Surface(gravel,"Gravel",.06f);
  ground.AddComponent<MeshRenderer>().sharedMaterials=new[]{asphalt,gravel};
 }

 void MakeTrackBarriers(){
  var concrete=Mat("Pale concrete safety barriers",new Color(.86f,.87f,.83f),.1f);
  concrete.SetColor("_EmissionColor",new Color(.075f,.075f,.07f));concrete.EnableKeyword("_EMISSION");
  var steel=Mat("Galvanized Armco",new Color(.49f,.54f,.56f),.5f);steel.SetFloat("_Metallic",.65f);
  var wire=new Material(Resources.Load<Shader>("Visuals/CatchFence"));wire.color=new Color(.38f,.43f,.44f);
  foreach(var run in BarrierRuns){
   var feet=new List<Vector3>();var outward=new List<Vector3>();
   for(int i=run.Start;i<=run.End;i++){
    var row=centerline.Sections[i];float width=run.Side<0?row.LeftWidth:row.RightWidth;
    var foot=row.Center+row.Right*(run.Side*(width+run.Setback));foot.y=SceneryGroundHeight(foot.x,foot.z);
    feet.Add(foot);outward.Add(row.Right*run.Side);barrierFeet.Add(foot);
   }
   var vertices=new List<Vector3>();var triangles=new List<int>();
   if(run.Kind==BarrierKind.Armco){
    for(int rail=0;rail<3;rail++)Extrude(new[]{new Vector2(0,.2f+rail*.31f),new Vector2(-.07f,.27f+rail*.31f),
     new Vector2(-.015f,.35f+rail*.31f),new Vector2(-.07f,.43f+rail*.31f),new Vector2(0,.50f+rail*.31f)});
   }else if(run.Kind==BarrierKind.Concrete){
    Extrude(new[]{new Vector2(-.38f,0),new Vector2(-.32f,.22f),new Vector2(-.15f,.6f),new Vector2(-.15f,1.05f),
     new Vector2(.15f,1.05f),new Vector2(.15f,.6f),new Vector2(.32f,.22f),new Vector2(.38f,0)});
   }else{
    Extrude(new[]{new Vector2(-.2f,0),new Vector2(-.2f,1.05f),new Vector2(.2f,1.05f),new Vector2(.2f,0)});
   }
   AddBarrierMesh($"{run.Kind} / {run.Start}-{run.End} / side {run.Side}",vertices,triangles,run.Kind==BarrierKind.Armco?steel:concrete);
   if(run.Kind!=BarrierKind.Concrete)for(int i=0;i<feet.Count;i++){
    float height=run.Kind==BarrierKind.CatchFence?3.15f:1.05f;
    Box(run.Kind+" post",feet[i]+Vector3.up*(height*.5f)+outward[i]*.12f,new Vector3(.075f,height,.075f),steel);
    if(run.Kind==BarrierKind.CatchFence){
     var a=feet[i]+Vector3.up*3.15f+outward[i]*.12f;var b=a+Vector3.up*.35f-outward[i]*.25f;
     var post=Box("Catch fence inward top",(a+b)*.5f,new Vector3(.07f,(b-a).magnitude,.07f),steel);post.transform.up=(b-a).normalized;
    }
   }
   if(run.Kind==BarrierKind.CatchFence){
    vertices=new List<Vector3>();triangles=new List<int>();
    for(int i=0;i<feet.Count-1;i++){
     var start=feet[i]+Vector3.up*1.08f;var along=feet[i+1]-feet[i];float width=along.magnitude;along/=width;
     const float height=2.05f,spacing=.18f,thickness=.005f;
     foreach(int slope in new[]{-1,1})for(float intercept=-width;intercept<height+width;intercept+=spacing){
      float x0=slope>0?Mathf.Max(0,-intercept):Mathf.Max(0,intercept-height);
      float x1=slope>0?Mathf.Min(width,height-intercept):Mathf.Min(width,intercept);
      if(x1<=x0)continue;
      var a=start+along*x0+Vector3.up*(slope*x0+intercept);var b=start+along*x1+Vector3.up*(slope*x1+intercept);
      var across=(Vector3.up-along*slope).normalized*thickness*.5f;int v=vertices.Count;
      vertices.AddRange(new[]{a-across,a+across,b-across,b+across});triangles.AddRange(new[]{v,v+2,v+1,v+1,v+2,v+3});
     }
    }
    AddBarrierMesh("Pit straight diamond catch mesh",vertices,triangles,wire);
   }
   void Extrude(Vector2[] profile){
    int first=vertices.Count,n=profile.Length;
    for(int i=0;i<feet.Count;i++)foreach(var p in profile)vertices.Add(feet[i]+outward[i]*p.x+Vector3.up*p.y);
    for(int i=0;i<feet.Count-1;i++)for(int j=0;j<n-1;j++){
     int a=first+i*n+j,b=a+n;
     triangles.AddRange(run.Side>0?new[]{a,b,a+1,a+1,b,b+1}:new[]{a,a+1,b,a+1,b+1,b});
    }
   }
  }
  Debug.Log($"TRACK_BARRIERS runs={BarrierRuns.Length} sampledFeet={barrierFeet.Count} source=2022-film styles=catch-mesh/Armco/concrete placement=visual-estimate");
 }

 void AddBarrierMesh(string name,List<Vector3> vertices,List<int> triangles,Material material){
  var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
  var obj=new GameObject(name);obj.AddComponent<MeshFilter>().sharedMesh=mesh;
  var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;barrierRenderers.Add(renderer);
 }
}
