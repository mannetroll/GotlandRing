using System.Collections.Generic;
using UnityEngine;

// Procedural, photo-inspired GC8 body. Dimensions are metres, forward is +Z.
public static class ImprezaModel
{
 static Material paint, rubber, alloy, lamp, tail, amber, dial;
 static GameObject MeshPart(string name,Transform parent,List<Vector3> v,List<int> t,Material material,List<Vector3> normals=null)
 {
  var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);
  if(normals!=null)mesh.SetNormals(normals);else mesh.RecalculateNormals();mesh.RecalculateBounds();
  var o=new GameObject(name);o.transform.SetParent(parent,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=material;return o;
 }
 static Material ColorMat(Material source,string name,Color color){var m=new Material(source){name=name,color=color};return m;}
 static void Quad(List<int> t,int a,int b,int c,int d){t.Add(a);t.Add(b);t.Add(c);t.Add(a);t.Add(c);t.Add(d);}
 public static GameObject Rounded(string name,Transform parent,Vector3 center,Vector3 size,float radius,Material material)
 {
  var v=new List<Vector3>();var normals=new List<Vector3>();var t=new List<int>();var half=size*.5f;radius=Mathf.Min(radius,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.98f);
  Vector3[] axes={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
  const int segments=10;
  foreach(var normal in axes){Vector3 u=Vector3.Cross(normal,Mathf.Abs(normal.y)>.9f?Vector3.forward:Vector3.up),w=Vector3.Cross(normal,u);int start=v.Count;
   for(int y=0;y<=segments;y++)for(int x=0;x<=segments;x++){Vector3 p=Vector3.Scale(normal+u*(x*2f/segments-1)+w*(y*2f/segments-1),half);var core=new Vector3(Mathf.Clamp(p.x,-half.x+radius,half.x-radius),Mathf.Clamp(p.y,-half.y+radius,half.y-radius),Mathf.Clamp(p.z,-half.z+radius,half.z-radius));var n=(p-core).normalized;v.Add(center+core+n*radius);normals.Add(n);}
   for(int y=0;y<segments;y++)for(int x=0;x<segments;x++){int a=start+y*(segments+1)+x;Quad(t,a,a+1,a+segments+2,a+segments+1);}
  }
  return MeshPart(name,parent,v,t,material,normals);
 }
 static GameObject Tube(string name,Transform parent,Vector3 center,float radius,float thickness,Material material,bool axleX=false,float start=0,float sweep=360)
 {
  var v=new List<Vector3>();var t=new List<int>();const int rings=96,sides=20;
  for(int i=0;i<=rings;i++){float a=(start+sweep*i/rings)*Mathf.Deg2Rad;for(int j=0;j<=sides;j++){float b=j*2*Mathf.PI/sides;float r=radius+Mathf.Cos(b)*thickness;Vector3 p=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(b)*thickness);if(axleX)p=new Vector3(p.z,p.y,p.x);v.Add(center+p);}}
  for(int i=0;i<rings;i++)for(int j=0;j<sides;j++){int a=i*(sides+1)+j;Quad(t,a,a+sides+1,a+sides+2,a+1);}
  if(axleX)for(int i=0;i<t.Count;i+=3){int swap=t[i+1];t[i+1]=t[i+2];t[i+2]=swap;}
  return MeshPart(name,parent,v,t,material);
 }
 static GameObject Cylinder(string name,Transform parent,Vector3 pos,float radius,float depth,Material material,bool axleX=false)
 {
  var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localRotation=Quaternion.Euler(axleX?0:90,0,axleX?90:0);o.transform.localScale=new Vector3(radius*2,depth*.5f,radius*2);o.GetComponent<Renderer>().sharedMaterial=material;Object.Destroy(o.GetComponent<Collider>());return o;
 }
 static void Beam(string name,Transform parent,Vector3 a,Vector3 b,float width,Material material)
 {var o=Rounded(name,parent,Vector3.zero,new Vector3(width,width,Vector3.Distance(a,b)),width*.45f,material);o.transform.localPosition=(a+b)*.5f;o.transform.localRotation=Quaternion.LookRotation(b-a);}
 static float Fender(float z)=>.035f*Mathf.Exp(-Mathf.Pow((z-1.3f)/.48f,2))+.035f*Mathf.Exp(-Mathf.Pow((z+1.32f)/.48f,2));
 static float Width(float z)=>Fender(z)+Mathf.Lerp(.73f,.855f,Mathf.Pow(Mathf.Clamp01(1-Mathf.Pow(z/2.18f,4)),.3f));
 static float Deck(float z)=>.91f-.16f*Mathf.Pow(Mathf.Abs(z)/2.18f,5);
 static float Bottom(float z){float y=.33f;foreach(float axle in new[]{-1.32f,1.3f}){float d=Mathf.Abs(z-axle);if(d<.405f)y=Mathf.Max(y,.35f+Mathf.Sqrt(.405f*.405f-d*d));}return y;}
 static void Body(Transform root)
 {
  // Continuous bonnet/shoulders with genuine open wheel-arch side boundaries.
  var v=new List<Vector3>();var t=new List<int>();const int longitudinal=240,cross=32;
  for(int i=0;i<=longitudinal;i++){float z=Mathf.Lerp(-2.16f,2.16f,i/(float)longitudinal);for(int j=0;j<=cross;j++){float x=j*2f/cross-1;v.Add(new Vector3(x*Width(z),Deck(z)+.055f*(1-x*x),z));}}
  for(int i=0;i<longitudinal;i++)for(int j=0;j<cross;j++){int a=i*(cross+1)+j;Quad(t,a,a+cross+1,a+cross+2,a+1);}
  MeshPart("Sculpted bonnet and shoulder shell",root,v,t,paint);
  foreach(int side in new[]{-1,1}){v=new List<Vector3>();t=new List<int>();const int rows=14;
   for(int i=0;i<=longitudinal;i++){float z=Mathf.Lerp(-2.16f,2.16f,i/(float)longitudinal);float arch=Bottom(z);for(int j=0;j<=rows;j++){float f=j/(float)rows;float bulge=.025f*Mathf.Sin(f*Mathf.PI);v.Add(new Vector3(side*(Width(z)+bulge-.025f*f),Mathf.Lerp(Deck(z),arch,f),z));}}
   for(int i=0;i<longitudinal;i++)for(int j=0;j<rows;j++){int a=i*(rows+1)+j;if(side>0)Quad(t,a,a+rows+1,a+rows+2,a+1);else Quad(t,a+1,a+rows+2,a+rows+1,a);}
   MeshPart("Contoured side with wheel openings",root,v,t,paint);
   foreach(float axle in new[]{-1.32f,1.3f})Tube("Rolled wheel arch lip",root,new Vector3(side*.86f,.35f,axle),.407f,.019f,paint,true,0,180);
   Rounded("Side sill",root,new Vector3(side*.83f,.34f,0),new Vector3(.07f,.10f,1.7f),.025f,paint);
   foreach(float z in new[]{-.83f,.13f})Rounded("Door handle",root,new Vector3(side*.87f,.82f,z),new Vector3(.035f,.035f,.16f),.014f,rubber);
   Beam("Door shut line",root,new Vector3(side*.873f,.43f,-.37f),new Vector3(side*.857f,.91f,-.37f),.006f,rubber);
   Rounded("Door rubbing strip",root,new Vector3(side*.87f,.59f,0),new Vector3(.017f,.025f,1.55f),.006f,rubber);
  }
 }
 public static Transform Create(Transform root,Material red,Material black,Material silver)
 {
  paint=red;rubber=black;alloy=silver;lamp=ColorMat(alloy,"Headlight lens",new Color(.78f,.87f,.91f));tail=ColorMat(paint,"Ruby taillight",new Color(.65f,.012f,.025f));amber=ColorMat(paint,"Amber indicator",new Color(.95f,.35f,.06f));dial=ColorMat(rubber,"White instrument faces",new Color(.83f,.84f,.79f));
  Body(root);
  Rounded("Rounded front bumper",root,new Vector3(0,.43f,2.05f),new Vector3(1.64f,.26f,.28f),.10f,paint);
  Rounded("Rounded rear bumper",root,new Vector3(0,.43f,-2.05f),new Vector3(1.64f,.26f,.28f),.10f,paint);
  Rounded("Lower grille",root,new Vector3(0,.45f,2.198f),new Vector3(.82f,.15f,.03f),.014f,rubber);
  Rounded("Upper grille",root,new Vector3(0,.76f,2.16f),new Vector3(.62f,.16f,.025f),.011f,rubber);
  for(int i=-4;i<=4;i++)Beam("Grille slat",root,new Vector3(i*.06f,.70f,2.177f),new Vector3(i*.06f,.82f,2.177f),.008f,alloy);
  Rounded("Hood scoop moulding",root,new Vector3(0,1.005f,1.16f),new Vector3(.71f,.16f,.44f),.055f,paint);
  Rounded("Scoop intake",root,new Vector3(0,1.012f,1.379f),new Vector3(.57f,.080f,.012f),.005f,rubber);
  foreach(int side in new[]{-1,1}){
   var vent=Rounded("Bonnet vent",root,new Vector3(side*.57f,.955f,1.31f),new Vector3(.16f,.015f,.32f),.007f,rubber);
   Rounded("Swept headlamp",root,new Vector3(side*.56f,.76f,2.128f),new Vector3(.46f,.19f,.09f),.043f,lamp);
   Rounded("Front indicator",root,new Vector3(side*.765f,.75f,2.10f),new Vector3(.10f,.15f,.075f),.025f,amber);
   Cylinder("Round rally fog lamp",root,new Vector3(side*.6f,.43f,2.20f),.11f,.025f,lamp);
   Tube("Fog lamp bezel",root,new Vector3(side*.6f,.43f,2.215f),.11f,.012f,alloy);
   Rounded("Rear lamp",root,new Vector3(side*.59f,.74f,-2.145f),new Vector3(.45f,.21f,.06f),.028f,tail);
   Rounded("Rear amber strip",root,new Vector3(side*.59f,.695f,-2.18f),new Vector3(.40f,.055f,.015f),.006f,amber);
   Beam("Slender A pillar",root,new Vector3(side*.79f,.92f,.68f),new Vector3(side*.68f,1.52f,.05f),.045f,paint);
   Beam("B pillar",root,new Vector3(side*.79f,.91f,-.48f),new Vector3(side*.69f,1.53f,-.48f),.045f,rubber);
   Beam("Swept C pillar",root,new Vector3(side*.80f,.92f,-1.48f),new Vector3(side*.67f,1.51f,-1.02f),.095f,paint);
   Beam("Window sill",root,new Vector3(side*.8f,.95f,-1.45f),new Vector3(side*.8f,.95f,.65f),.026f,rubber);
   Rounded("Aerodynamic mirror",root,new Vector3(side*.96f,1.01f,.53f),new Vector3(.22f,.14f,.28f),.065f,paint);
   Rounded("Mirror glass",root,new Vector3(side*.96f,1.012f,.385f),new Vector3(.16f,.09f,.012f),.005f,alloy);
   Beam("Wing support",root,new Vector3(side*.59f,.92f,-1.77f),new Vector3(side*.62f,1.20f,-1.88f),.075f,paint);
  }
  var glass=ColorMat(alloy,"Tinted automotive glass",new Color(.18f,.26f,.30f,.24f));
  glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);glass.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHABLEND_ON");glass.renderQueue=3000;
  foreach(int side in new[]{-1,1}){
   Glass(root,glass,new Vector3(side*.79f,.97f,.60f),new Vector3(side*.68f,1.49f,.04f),new Vector3(side*.69f,1.49f,-.46f),new Vector3(side*.79f,.97f,-.46f));
   Glass(root,glass,new Vector3(side*.79f,.97f,-.50f),new Vector3(side*.69f,1.49f,-.50f),new Vector3(side*.67f,1.49f,-1.02f),new Vector3(side*.79f,.97f,-1.4f));
   Rounded("Front seat back",root,new Vector3(side*.39f,1.08f,-.55f),new Vector3(.43f,.60f,.16f),.07f,rubber);
   Rounded("Headrest",root,new Vector3(side*.39f,1.43f,-.56f),new Vector3(.26f,.18f,.14f),.06f,rubber);
  }
  Glass(root,glass,new Vector3(-.78f,.96f,-1.43f),new Vector3(-.65f,1.49f,-1.04f),new Vector3(.65f,1.49f,-1.04f),new Vector3(.78f,.96f,-1.43f));
  Rounded("Crowned roof",root,new Vector3(0,1.535f,-.53f),new Vector3(1.39f,.11f,1.19f),.05f,paint);
  Rounded("Rear aerofoil",root,new Vector3(0,1.235f,-1.88f),new Vector3(1.67f,.075f,.30f),.035f,paint);
  Cylinder("Exhaust tip",root,new Vector3(-.62f,.27f,-2.21f),.065f,.18f,alloy);Cylinder("Exhaust opening",root,new Vector3(-.62f,.27f,-2.31f),.050f,.005f,rubber);
  foreach(int side in new[]{-1,1})foreach(float z in new[]{-1.32f,1.3f}){
   var center=new Vector3(side*.81f,.35f,z);Tube("96 segment rounded tire",root,center,.255f,.083f,rubber,true);
   Cylinder("Brake disc",root,center+Vector3.right*side*.063f,.215f,.025f,alloy,true);
   Tube("Polished wheel rim",root,center+Vector3.right*side*.105f,.222f,.018f,alloy,true);
   Cylinder("Wheel hub",root,center+Vector3.right*side*.117f,.055f,.025f,alloy,true);
   for(int k=0;k<10;k++){float a=k*Mathf.PI*2/10;var c=center+Vector3.right*side*.12f;Beam("Alloy spoke",root,c+new Vector3(0,Mathf.Cos(a),Mathf.Sin(a))*.05f,c+new Vector3(0,Mathf.Cos(a+.12f),Mathf.Sin(a+.12f))*.21f,.022f,alloy);}
  }
  Rounded("Soft dashboard",root,new Vector3(0,.99f,.46f),new Vector3(1.5f,.18f,.42f),.075f,rubber);
  Rounded("Curved instrument hood",root,new Vector3(-.4f,1.08f,.35f),new Vector3(.51f,.15f,.24f),.07f,rubber);
  for(int i=0;i<3;i++)Gauge(root,new Vector3(-.11f+i*.17f,1.11f,.39f),.067f);
  for(int i=0;i<2;i++)Gauge(root,new Vector3(-.50f+i*.19f,1.08f,.215f),.071f);
  var steering=new GameObject("Steering wheel").transform;steering.SetParent(root,false);steering.localPosition=new Vector3(-.4f,1.035f,.10f);
  Tube("Smooth leather steering rim",steering,Vector3.zero,.185f,.018f,rubber);
  for(int i=0;i<3;i++){float a=(30+i*120)*Mathf.Deg2Rad;Beam("Steering spoke",steering,Vector3.zero,new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.17f,.025f,alloy);}
  Rounded("Steering center",steering,Vector3.zero,new Vector3(.12f,.095f,.06f),.028f,rubber);
  int triangles=0;foreach(var filter in root.GetComponentsInChildren<MeshFilter>())triangles+=filter.sharedMesh.triangles.Length/3;Debug.Log("IMPREZA_MODEL triangles="+triangles);
  return steering;
 }
 static void Glass(Transform root,Material material,Vector3 a,Vector3 b,Vector3 c,Vector3 d){var v=new List<Vector3>{a,b,c,d};var t=new List<int>{0,1,2,0,2,3,2,1,0,3,2,0};var o=MeshPart("Window glazing",root,v,t,material);o.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
 static void Gauge(Transform root,Vector3 p,float r)
 {
  Cylinder("Round gauge housing",root,p+Vector3.forward*.035f,r+.009f,.085f,rubber);
  Cylinder("Instrument face",root,p-Vector3.forward*.013f,r,.006f,dial);
  Tube("Gauge chrome bezel",root,p-Vector3.forward*.018f,r,.005f,alloy);
  for(int j=0;j<12;j++){float a=(j*24-50)*Mathf.Deg2Rad;var direction=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);Beam("Dial graduation",root,p+direction*r*.75f-Vector3.forward*.019f,p+direction*r*.92f-Vector3.forward*.019f,.0025f,rubber);}
  Beam("Red gauge needle",root,p-Vector3.forward*.024f,p+new Vector3(-r*.45f,r*.45f,-.024f),.003f,paint);
 }
}
