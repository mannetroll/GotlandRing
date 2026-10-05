using System.Collections.Generic;
using UnityEngine;

// Procedural, photo-inspired GC8 body. Dimensions are metres, forward is +Z.
public static class ImprezaModel
{
 static Material paint, rubber, alloy, lamp, tail, amber, dial;
 static GameObject MeshPart(string name,Transform parent,List<Vector3> v,List<int> t,Material material,List<Vector3> normals=null)
 {
  var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);var uv=new Vector2[v.Count];for(int i=0;i<v.Count;i++)uv[i]=new Vector2(v[i].x*3,v[i].z*3+v[i].y);mesh.uv=uv;
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
 static float Deck(float z)=>.94f-.13f*Mathf.Pow(Mathf.Abs(z)/2.18f,4);
 static float Bottom(float z){float y=.33f;foreach(float axle in new[]{-1.32f,1.3f}){float d=Mathf.Abs(z-axle);if(d<.405f)y=Mathf.Max(y,.35f+Mathf.Sqrt(.405f*.405f-d*d));}return y;}
 static void Body(Transform root)
 {
  // Continuous bonnet/shoulders with genuine open wheel-arch side boundaries.
  var v=new List<Vector3>();var t=new List<int>();const int longitudinal=240,cross=32;
  for(int i=0;i<=longitudinal;i++){float z=Mathf.Lerp(-2.16f,2.16f,i/(float)longitudinal);for(int j=0;j<=cross;j++){float x=j*2f/cross-1;v.Add(new Vector3(x*Width(z),Deck(z)+.055f*(1-x*x),z));}}
  for(int i=0;i<longitudinal;i++)for(int j=0;j<cross;j++){float z=Mathf.Lerp(-2.16f,2.16f,i/(float)longitudinal);if(z> -1.43f && z<.63f && j>1 && j<cross-2)continue;int a=i*(cross+1)+j;Quad(t,a,a+cross+1,a+cross+2,a+1);}
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
  paint=red;rubber=black;alloy=silver;lamp=ColorMat(alloy,"Headlight lens",new Color(.78f,.87f,.91f));tail=ColorMat(paint,"Ruby taillight",new Color(.9f,.009f,.018f));amber=ColorMat(paint,"Amber indicator",new Color(.95f,.35f,.06f));dial=ColorMat(rubber,"White instrument faces",new Color(.83f,.84f,.79f));
  Body(root);Details(root);
  Rounded("Rounded front bumper",root,new Vector3(0,.43f,2.05f),new Vector3(1.64f,.26f,.28f),.10f,paint);
  Rounded("Rounded rear bumper",root,new Vector3(0,.43f,-2.045f),new Vector3(1.66f,.30f,.32f),.13f,paint);
  Rounded("Lower grille",root,new Vector3(0,.45f,2.198f),new Vector3(.82f,.15f,.03f),.014f,rubber);
  Rounded("Upper grille",root,new Vector3(0,.76f,2.16f),new Vector3(.62f,.16f,.025f),.011f,rubber);
  for(int i=-4;i<=4;i++)Beam("Grille slat",root,new Vector3(i*.06f,.70f,2.177f),new Vector3(i*.06f,.82f,2.177f),.008f,alloy);
  Rounded("Hood scoop moulding",root,new Vector3(0,1.005f,1.16f),new Vector3(.71f,.13f,.44f),.045f,paint);
  Rounded("Scoop intake",root,new Vector3(0,1.012f,1.379f),new Vector3(.57f,.080f,.012f),.005f,rubber);
  foreach(int side in new[]{-1,1}){
   var vent=Rounded("Bonnet vent",root,new Vector3(side*.57f,.955f,1.31f),new Vector3(.16f,.015f,.32f),.007f,rubber);
   Rounded("Swept headlamp",root,new Vector3(side*.56f,.76f,2.128f),new Vector3(.46f,.19f,.06f),.025f,lamp);
   Rounded("Front indicator",root,new Vector3(side*.765f,.75f,2.10f),new Vector3(.10f,.15f,.075f),.025f,amber);
   Cylinder("Round rally fog lamp",root,new Vector3(side*.6f,.43f,2.20f),.11f,.025f,lamp);
   Tube("Fog lamp bezel",root,new Vector3(side*.6f,.43f,2.215f),.11f,.012f,alloy);
   Rounded("Rear lamp",root,new Vector3(side*.59f,.74f,-2.145f),new Vector3(.43f,.21f,.10f),.040f,tail);
   Rounded("Rear amber strip",root,new Vector3(side*.59f,.695f,-2.205f),new Vector3(.39f,.047f,.018f),.006f,amber);
   Beam("Slender A pillar",root,new Vector3(side*.79f,.92f,.68f),new Vector3(side*.68f,1.52f,.05f),.045f,paint);
   Beam("B pillar",root,new Vector3(side*.79f,.91f,-.48f),new Vector3(side*.69f,1.53f,-.48f),.045f,rubber);
   Beam("Swept C pillar",root,new Vector3(side*.80f,.92f,-1.48f),new Vector3(side*.67f,1.51f,-1.02f),.095f,paint);
   Beam("Window sill",root,new Vector3(side*.8f,.95f,-1.45f),new Vector3(side*.8f,.95f,.65f),.026f,rubber);
   Rounded("Aerodynamic mirror",root,new Vector3(side*.96f,1.01f,.53f),new Vector3(.22f,.14f,.28f),.065f,paint);
   Rounded("Mirror glass",root,new Vector3(side*.96f,1.012f,.385f),new Vector3(.16f,.09f,.012f),.005f,alloy);

  }
  var glass=Resources.Load<Material>("Visuals/Glass");
  foreach(int side in new[]{-1,1}){
   Glass(root,glass,new Vector3(side*.79f,.97f,.60f),new Vector3(side*.68f,1.49f,.04f),new Vector3(side*.69f,1.49f,-.46f),new Vector3(side*.79f,.97f,-.46f));
   Glass(root,glass,new Vector3(side*.79f,.97f,-.50f),new Vector3(side*.69f,1.49f,-.50f),new Vector3(side*.67f,1.49f,-1.02f),new Vector3(side*.79f,.97f,-1.4f));
   Rounded("Front seat back",root,new Vector3(side*.39f,1.08f,-.55f),new Vector3(.43f,.60f,.16f),.07f,rubber);
   Rounded("Headrest",root,new Vector3(side*.39f,1.43f,-.56f),new Vector3(.26f,.18f,.14f),.06f,rubber);
  }
  Glass(root,glass,new Vector3(-.78f,.96f,-1.43f),new Vector3(-.65f,1.49f,-1.04f),new Vector3(.65f,1.49f,-1.04f),new Vector3(.78f,.96f,-1.43f));
  Glass(root,glass,new Vector3(-.78f,.96f,.65f),new Vector3(.78f,.96f,.65f),new Vector3(.665f,1.50f,.04f),new Vector3(-.665f,1.50f,.04f));
  Roof(root);
  CurvedWing(root);RearDetails(root);
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
 static float WingHeight(float x)=>.94f+.30f*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(Mathf.Abs(x)/.79f,8)));
 static void CurvedWing(Transform root){
  var v=new List<Vector3>();var t=new List<int>();const int spans=128,sides=24;
  for(int i=0;i<=spans;i++){
   float x=Mathf.Lerp(-.785f,.785f,i/(float)spans),y=WingHeight(x);
   float derivative=(WingHeight(Mathf.Clamp(x+.001f,-.789f,.789f))-WingHeight(Mathf.Clamp(x-.001f,-.789f,.789f)))/.002f;
   Vector3 normal=new Vector3(-derivative,1,0).normalized;float thick=Mathf.Lerp(.026f,.058f,Mathf.Pow(Mathf.Abs(x)/.79f,6));
   for(int j=0;j<=sides;j++){float a=j*2*Mathf.PI/sides;v.Add(new Vector3(x,y,-1.82f+.055f*x*x)+normal*(Mathf.Cos(a)*thick)+Vector3.forward*(Mathf.Sin(a)*.135f));}
  }
  for(int i=0;i<spans;i++)for(int j=0;j<sides;j++){int a=i*(sides+1)+j;Quad(t,a,a+1,a+sides+2,a+sides+1);}
  MeshPart("Curved factory rear wing with integrated shoulders",root,v,t,paint);
  foreach(int side in new[]{-1,1})Rounded("Wing mounting foot",root,new Vector3(side*.775f,.961f,-1.785f),new Vector3(.105f,.085f,.27f),.040f,paint);
  Rounded("Wing high brake light",root,new Vector3(0,1.236f,-1.957f),new Vector3(.40f,.022f,.014f),.006f,tail);
 }
 static void RearDetails(Transform root){
  Rounded("Boot lid central inset",root,new Vector3(0,.727f,-2.174f),new Vector3(.90f,.245f,.026f),.012f,paint);
  foreach(int side in new[]{-1,1}){
   Rounded("Rear reversing lens",root,new Vector3(side*.465f,.695f,-2.22f),new Vector3(.13f,.04f,.017f),.006f,lamp);
   Rounded("Rear bumper dark lower corner",root,new Vector3(side*.66f,.282f,-2.08f),new Vector3(.32f,.06f,.25f),.025f,rubber);
   Beam("Boot shut line",root,new Vector3(side*.45f,.61f,-2.197f),new Vector3(side*.47f,.837f,-2.19f),.004f,rubber);
   Rounded("Wraparound tail lamp",root,new Vector3(side*.78f,.755f,-2.04f),new Vector3(.075f,.15f,.24f),.035f,tail);
  }
  var plateMaterial=new Material(dial);plateMaterial.mainTexture=Resources.Load<Texture2D>("Visuals/NumberPlate");plateMaterial.color=Color.white;
  Rounded("Rear number plate recess",root,new Vector3(0,.412f,-2.208f),new Vector3(.49f,.13f,.02f),.008f,rubber);
  RearDecal(root,"Rear registration plate",plateMaterial,new Vector3(0,.415f,-2.222f),new Vector2(.44f,.10f));
  var badgeMaterial=new Material(dial);badgeMaterial.mainTexture=Resources.Load<Texture2D>("Visuals/RearBadge");badgeMaterial.color=Color.white;
  RearDecal(root,"SUBARU boot lettering",badgeMaterial,new Vector3(0,.768f,-2.194f),new Vector2(.45f,.045f));
 }
 static void RearDecal(Transform root,string name,Material material,Vector3 p,Vector2 size){var q=GameObject.CreatePrimitive(PrimitiveType.Quad);q.name=name;q.transform.SetParent(root,false);q.transform.localPosition=p;q.transform.localScale=new Vector3(size.x,size.y,1);q.GetComponent<Renderer>().sharedMaterial=material;Object.Destroy(q.GetComponent<Collider>());}
 static void Roof(Transform root){
  var v=new List<Vector3>();var t=new List<int>();const int nx=32,nz=40;
  for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){float u=x*2f/nx-1,w=z/(float)nz;v.Add(new Vector3(u*(.665f+.025f*Mathf.Sin(w*Mathf.PI)),1.515f+.075f*(1-u*u)*Mathf.Sin(w*Mathf.PI),Mathf.Lerp(-1.08f,.08f,w)));}
  for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int a=z*(nx+1)+x;Quad(t,a,a+nx+1,a+nx+2,a+1);}MeshPart("Compound curved roof panel",root,v,t,paint);
 }
 static void Details(Transform root){
  // Connect the nose and tail to the continuous fenders with curved vertical skins.
  foreach(int side in new[]{-1,1}){
   for(int i=0;i<24;i++){float z0=Mathf.Lerp(.7f,2.02f,i/24f),z1=Mathf.Lerp(.7f,2.02f,(i+1)/24f);Beam("Bonnet panel seam",root,new Vector3(side*.65f,Deck(z0)+.023f,z0),new Vector3(side*.65f,Deck(z1)+.023f,z1),.004f,rubber);}
   for(int i=0;i<6;i++)Beam("Headlamp optical rib",root,new Vector3(side*.56f+(i-2.5f)*.06f,.695f,2.167f),new Vector3(side*.56f+(i-2.5f)*.06f,.815f,2.167f),.004f,alloy);
   for(int k=0;k<2;k++)Cylinder("Headlight reflector",root,new Vector3(side*.56f+(k-.5f)*.18f,.76f,2.165f),.065f,.012f,alloy);
   Rounded("Front bumper shoulder",root,new Vector3(side*.76f,.56f,1.96f),new Vector3(.25f,.22f,.36f),.09f,paint);
   Rounded("Fog lamp dark recess",root,new Vector3(side*.60f,.43f,2.185f),new Vector3(.31f,.27f,.025f),.11f,rubber);
   Beam("Windscreen wiper",root,new Vector3(side*.22f,.988f,.65f),new Vector3(side*.65f,1.016f,.59f),.012f,rubber);
   Rounded("Window upper weather seal",root,new Vector3(side*.686f,1.502f,-.49f),new Vector3(.018f,.023f,1.08f),.008f,rubber);
  }
  Rounded("Front nose painted bridge",root,new Vector3(0,.635f,2.12f),new Vector3(1.59f,.065f,.13f),.029f,paint);
  Rounded("Front lip",root,new Vector3(0,.282f,2.10f),new Vector3(1.61f,.065f,.23f),.028f,paint);
  Rounded("Rear trunk closing panel",root,new Vector3(0,.68f,-2.12f),new Vector3(1.56f,.27f,.08f),.035f,paint);
  Rounded("Front number plate",root,new Vector3(0,.51f,2.23f),new Vector3(.40f,.09f,.015f),.006f,dial);
  var plateMaterial=new Material(dial);plateMaterial.mainTexture=Resources.Load<Texture2D>("Visuals/NumberPlate");plateMaterial.color=Color.white;
  var plate=GameObject.CreatePrimitive(PrimitiveType.Quad);plate.name="RNY 994";plate.transform.SetParent(root,false);plate.transform.localPosition=new Vector3(0,.51f,2.243f);plate.transform.localRotation=Quaternion.Euler(0,180,0);plate.transform.localScale=new Vector3(.40f,.09f,1);plate.GetComponent<Renderer>().sharedMaterial=plateMaterial;Object.Destroy(plate.GetComponent<Collider>());
 }
 static void Glass(Transform root,Material material,Vector3 a,Vector3 b,Vector3 c,Vector3 d){var v=new List<Vector3>{a,b,c,d};var t=new List<int>{0,1,2,0,2,3};if(Vector3.Dot(Vector3.Cross(b-a,c-a),(a+b+c+d)*.25f-new Vector3(0,1.2f,-.4f))<0){t=new List<int>{0,2,1,0,3,2};}var o=MeshPart("Window glazing",root,v,t,material);o.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
 static void Gauge(Transform root,Vector3 p,float r)
 {
  Cylinder("Round gauge housing",root,p+Vector3.forward*.035f,r+.009f,.085f,rubber);
  Cylinder("Instrument face",root,p-Vector3.forward*.013f,r,.006f,dial);
  Tube("Gauge chrome bezel",root,p-Vector3.forward*.018f,r,.005f,alloy);
  for(int j=0;j<12;j++){float a=(j*24-50)*Mathf.Deg2Rad;var direction=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);Beam("Dial graduation",root,p+direction*r*.75f-Vector3.forward*.019f,p+direction*r*.92f-Vector3.forward*.019f,.0025f,rubber);}
  Beam("Red gauge needle",root,p-Vector3.forward*.024f,p+new Vector3(-r*.45f,r*.45f,-.024f),.003f,paint);
 }
}
