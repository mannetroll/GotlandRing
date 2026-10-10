using System.Collections.Generic;
using UnityEngine;

// Quarry stockpiles registered to the aerial crops (1 m/pixel, TRACK.md).
// Crest elevations and talus slopes are visual estimates from the reference lap.
public sealed class QuarryTerrain
{
 public sealed class Bank
 {
  public readonly string Name;
  public readonly float Crest,Skirt;
  public readonly Vector2[] Polygon;
  public Bank(string name,float crest,float skirt,params Vector2[] map){
   Name=name;Crest=crest;Skirt=skirt;Polygon=new Vector2[map.Length];
   float angle=3.244f*Mathf.Deg2Rad,c=Mathf.Cos(angle),s=Mathf.Sin(angle);
   for(int i=0;i<map.Length;i++){float e=map[i].x-801.318349f,n=786.115749f-map[i].y;Polygon[i]=new Vector2(e*c+n*s,-e*s+n*c);}
  }
  public float Profile(float x,float z){
   var p=new Vector2(x,z);bool inside=false;float distance=float.MaxValue;
   for(int i=0,j=Polygon.Length-1;i<Polygon.Length;j=i++){
    var a=Polygon[j];var b=Polygon[i];var edge=b-a;
    distance=Mathf.Min(distance,Vector2.Distance(p,a+edge*Mathf.Clamp01(Vector2.Dot(p-a,edge)/edge.sqrMagnitude)));
    if((a.y>z)!=(b.y>z)&&x<(b.x-a.x)*(z-a.y)/(b.y-a.y)+a.x)inside=!inside;
   }
   float skirt=Skirt*Mathf.Lerp(.72f,1.2f,Mathf.PerlinNoise(x*.055f+70,z*.055f+70));
   return inside?1:Mathf.Clamp01(1-distance/skirt);
  }
 }
 public static readonly Bank[] Banks={
  new Bank("Eastern quarry ridge / Månen approach",14,32,new Vector2(1330,492),new Vector2(1425,500),new Vector2(1440,578),new Vector2(1370,641),new Vector2(1320,604)),
  new Bank("Havsörnen limestone face",7,30,new Vector2(1325,755),new Vector2(1395,775),new Vector2(1320,915),new Vector2(1240,1030),new Vector2(1230,1105),new Vector2(1187,1088),new Vector2(1170,1015),new Vector2(1200,932),new Vector2(1260,870)),
  new Bank("Southern quarry stockpile",8,40,new Vector2(1030,1120),new Vector2(1105,1080),new Vector2(1190,1115),new Vector2(1190,1214),new Vector2(1090,1240),new Vector2(960,1205))
 };
 public sealed class Patch
 {
  public Mesh Mesh;
  public Vector3[] Vertices;
  public float X,Z;
  public int Columns,Rows;
  public bool Sample(float x,float z,out float height,out Vector3 normal){
   float fx=(x-X)/Cell,fz=(z-Z)/Cell;int ix=Mathf.FloorToInt(fx),iz=Mathf.FloorToInt(fz);
   height=0;normal=Vector3.up;if(ix<0||iz<0||ix>=Columns-1||iz>=Rows-1)return false;
   fx-=ix;fz-=iz;int i=iz*Columns+ix;
   var a=Vertices[i];var b=Vertices[i+1];var c=Vertices[i+Columns];var d=Vertices[i+Columns+1];
   if(fx+fz<=1){height=a.y*(1-fx-fz)+b.y*fx+c.y*fz;normal=Vector3.Cross(c-a,b-a).normalized;}
   else{height=d.y*(fx+fz-1)+c.y*(1-fx)+b.y*(1-fz);normal=Vector3.Cross(c-b,d-b).normalized;}
   return true;
  }
 }
 const float Cell=4;
 public readonly List<Patch> Patches=new List<Patch>();
 readonly TrackData track;
 readonly Dictionary<Vector2Int,float> baseHeights=new Dictionary<Vector2Int,float>();
 public QuarryTerrain(TrackData track){this.track=track;foreach(var bank in Banks)Patches.Add(Build(bank));}

 public static bool BareGround(float x,float z){foreach(var bank in Banks)if(bank.Profile(x,z)>0)return true;return false;}
 public void Sample(float x,float z,ref TrackData.SurfaceSample surface){
  if(surface.OnRoad)return;
  foreach(var patch in Patches)if(patch.Sample(x,z,out float height,out var normal)&&height>surface.Height){surface.Height=height;surface.Normal=normal;}
 }
 public float GroundHeight(float x,float z){
  var sample=track.Sample(x,z);sample.Height=sample.Distance<=TrackData.TerrainExtent?Mathf.Max(sample.Height,BaseHeight(x,z)):BaseHeight(x,z);Sample(x,z,ref sample);return sample.Height;
 }
 public float BaseHeight(float x,float z){
  int ix=Mathf.FloorToInt(x/18),iz=Mathf.FloorToInt(z/18);float fx=x/18-ix,fz=z/18-iz;
  float Height(int dx,int dz){var key=new Vector2Int(ix+dx,iz+dz);if(!baseHeights.TryGetValue(key,out float h)){h=track.Height(key.x*18,key.y*18)-3;baseHeights.Add(key,h);}return h;}
  return fx+fz<=1?Height(0,0)*(1-fx-fz)+Height(1,0)*fx+Height(0,1)*fz
   :Height(1,1)*(fx+fz-1)+Height(0,1)*(1-fx)+Height(1,0)*(1-fz);
 }
 Patch Build(Bank bank){
  var min=bank.Polygon[0];var max=min;
  foreach(var p in bank.Polygon){min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
  min-=Vector2.one*(bank.Skirt*1.2f+Cell);max+=Vector2.one*(bank.Skirt*1.2f+Cell);
  var patch=new Patch{X=Mathf.Floor(min.x/Cell)*Cell,Z=Mathf.Floor(min.y/Cell)*Cell};
  patch.Columns=Mathf.CeilToInt((max.x-patch.X)/Cell)+1;patch.Rows=Mathf.CeilToInt((max.y-patch.Z)/Cell)+1;
  patch.Vertices=new Vector3[patch.Columns*patch.Rows];var uv=new Vector2[patch.Vertices.Length];var triangles=new List<int>();
  for(int z=0;z<patch.Rows;z++)for(int x=0;x<patch.Columns;x++){
   float px=patch.X+x*Cell,pz=patch.Z+z*Cell,ground=BaseHeight(px,pz),profile=bank.Profile(px,pz);
   // Keep the entire rendered road, apron and shoulder envelope clear.
   float distance=track.Nearest(px,pz,out _,out _);
   float clearance=Mathf.SmoothStep(0,1,Mathf.InverseLerp(TrackData.TerrainExtent+Cell,TrackData.TerrainExtent+24,distance));
   float crest=bank.Crest+(Mathf.PerlinNoise(px*.025f+30,pz*.025f+30)-.5f)*3.5f;
   float ribs=(Mathf.PerlinNoise(px*.15f+60,pz*.15f+60)-.5f)*2.2f*4*profile*(1-profile);
   int i=z*patch.Columns+x;patch.Vertices[i]=new Vector3(px,ground-.12f+Mathf.Max(0,crest-ground+ribs)*Mathf.Pow(profile,1.4f)*clearance,pz);
   uv[i]=new Vector2(px/9,pz/9);
   if(x==patch.Columns-1||z==patch.Rows-1)continue;
   triangles.AddRange(new[]{i,i+patch.Columns,i+1,i+1,i+patch.Columns,i+patch.Columns+1});
  }
  patch.Mesh=new Mesh{name=bank.Name,vertices=patch.Vertices,uv=uv,triangles=triangles.ToArray()};patch.Mesh.RecalculateNormals();patch.Mesh.RecalculateTangents();patch.Mesh.RecalculateBounds();return patch;
 }
}
