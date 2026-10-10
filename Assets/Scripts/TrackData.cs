using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class TrackData
{
 public const int Across=16, Columns=21;
 public const float ApronWidth=3, TerrainExtent=42;
 public readonly List<Vector3> Points=new List<Vector3>();
 public readonly List<Section> Sections=new List<Section>();
 public float HorizontalLength,Length3D,MinY=float.MaxValue,MaxY=float.MinValue;
 public Mesh SurfaceMesh {get;private set;}
 Vector3[] vertices,normals;

 public struct Section
 {
  public Vector3 Center,Right;
  public float Distance,LeftWidth,RightWidth,Slope,Curve,GroundLeft,GroundRight;
  public bool LeftInferred,RightInferred;
  // Edge-to-edge angle, including asymmetric widths on crowned/hollow sections.
  public float BankingDegrees=>Mathf.Atan(Slope+Curve*(RightWidth-LeftWidth))*Mathf.Rad2Deg;
  // Positive crossfall raises the right edge. Banking is already in these heights.
  public Vector3 Road(float offset)=>Center+Right*offset+Vector3.up*(Slope*offset+Curve*offset*offset);
  public Vector3 Ground(float offset){
   float width=offset<0?LeftWidth:RightWidth,side=Mathf.Sign(offset),outside=Mathf.Abs(offset)-width;
   if(outside<=0)return Road(offset);
   var edge=Road(side*width);float dy=offset<0?GroundLeft:GroundRight;
   float height=outside<=ApronWidth?edge.y+dy*outside/ApronWidth
    :Mathf.Lerp(edge.y+dy,Center.y-3,Mathf.Clamp01((outside-ApronWidth)/(TerrainExtent-width-ApronWidth)));
   var point=Center+Right*offset;point.y=height;return point;
  }
  public float Offset(int column){
   if(column==0)return -TerrainExtent;
   if(column==1)return -LeftWidth-ApronWidth;
   if(column<=10)return Mathf.Lerp(-LeftWidth,0,(column-2)/8f);
   if(column<=18)return RightWidth*(column-10)/8f;
   return column==19?RightWidth+ApronWidth:TerrainExtent;
  }
 }

 public struct SurfaceSample
 {
  public int Segment;
  public float Height,Distance,Offset,Width;
  public Vector3 Normal;
  public bool OnRoad;
 }

 public static TrackData Load(string csv){
  var data=new TrackData();
  using(var reader=new StringReader(csv)){
   var first=reader.ReadLine();if(first==null)throw new FormatException("Empty surface CSV");
   var header=first.TrimStart('\uFEFF').Split(',');var columns=new Dictionary<string,int>();
   for(int i=0;i<header.Length;i++)columns.Add(header[i],i);
   foreach(var name in new[]{"x_m","y_m","z_m","distance_m","forward_x","forward_z","width_left_m","width_right_m","cross_slope_center_percent","cross_curve_per_m","ground_apron_left_dy_m","ground_apron_right_dy_m","width_left_inferred","width_right_inferred"})
    if(!columns.ContainsKey(name))throw new FormatException("Missing surface column: "+name);
   string line;while((line=reader.ReadLine())!=null){
    if(string.IsNullOrWhiteSpace(line))continue;var fields=line.Split(',');
    if(fields.Length!=header.Length)throw new FormatException("Incorrect surface column count");
    float Get(string name){float value=float.Parse(fields[columns[name]],CultureInfo.InvariantCulture);if(!float.IsFinite(value))throw new FormatException("Non-finite surface value: "+name);return value;}
    var section=new Section{Center=new Vector3(Get("x_m"),Get("y_m"),Get("z_m")),Right=new Vector3(Get("forward_z"),0,-Get("forward_x")),
     Distance=Get("distance_m"),LeftWidth=Get("width_left_m"),RightWidth=Get("width_right_m"),Slope=Get("cross_slope_center_percent")/100,Curve=Get("cross_curve_per_m"),
     GroundLeft=Get("ground_apron_left_dy_m"),GroundRight=Get("ground_apron_right_dy_m"),LeftInferred=Get("width_left_inferred")>.5f,RightInferred=Get("width_right_inferred")>.5f};
    if(section.LeftWidth<=0||section.RightWidth<=0||section.LeftWidth+ApronWidth>=TerrainExtent||section.RightWidth+ApronWidth>=TerrainExtent||section.Right.sqrMagnitude<.5f)throw new FormatException("Invalid road width or tangent");
    section.Right.Normalize();
    if(data.Sections.Count>0&&section.Distance<=data.Sections[data.Sections.Count-1].Distance)throw new FormatException("Surface distances must increase");
    data.Sections.Add(section);
   }
  }
  int last=data.Sections.Count-1;
  if(last<3)throw new FormatException("Surface must include a complete closed lap");
  var start=data.Sections[0];var end=data.Sections[last];
  if(start.Center!=end.Center||start.Right!=end.Right||start.LeftWidth!=end.LeftWidth||start.RightWidth!=end.RightWidth||start.Slope!=end.Slope||start.Curve!=end.Curve||start.GroundLeft!=end.GroundLeft||start.GroundRight!=end.GroundRight||start.LeftInferred!=end.LeftInferred||start.RightInferred!=end.RightInferred)throw new FormatException("Surface cross-section must close exactly");
  data.HorizontalLength=end.Distance;data.Sections.RemoveAt(last);
  foreach(var section in data.Sections)data.Points.Add(section.Center);
  for(int i=0;i<data.Points.Count;i++){
   var p=data.Points[i];var d=data.Points[(i+1)%data.Points.Count]-p;
   if(d.x*d.x+d.z*d.z<.000001f)throw new FormatException("Duplicate consecutive surface point");
   data.Length3D+=d.magnitude;data.MinY=Mathf.Min(data.MinY,p.y);data.MaxY=Mathf.Max(data.MaxY,p.y);
  }
  data.BuildMesh();
  Debug.Log($"CSV_SURFACE points={data.Points.Count} horizontal={data.HorizontalLength:F3} length3d={data.Length3D:F3} minY={data.MinY:F3} maxY={data.MaxY:F3}");return data;
 }

 void BuildMesh(){
  vertices=new Vector3[(Points.Count+1)*Columns];var uv=new Vector2[vertices.Length];
  var triangles=new List<int>[5];for(int i=0;i<triangles.Length;i++)triangles[i]=new List<int>();
  for(int i=0;i<=Points.Count;i++){
   var row=Sections[i%Points.Count];
   for(int j=0;j<Columns;j++){
    int a=i*Columns+j;float offset=row.Offset(j);vertices[a]=row.Ground(offset);uv[a]=new Vector2(offset/4,(i==Points.Count?HorizontalLength:row.Distance)/4);
    if(i==Points.Count||j==Columns-1)continue;
    int part=j==0?0:j==1?1:j<18?2:j==18?3:4,b=a+Columns;
    triangles[part].AddRange(new[]{a,b,a+1,a+1,b,b+1});
   }
  }
  SurfaceMesh=new Mesh{name="CSV banked road, aprons and shoulders",indexFormat=IndexFormat.UInt32,vertices=vertices,uv=uv,subMeshCount=5};
  for(int i=0;i<triangles.Length;i++)SurfaceMesh.SetTriangles(triangles[i],i);
  // Wide approximate shoulders can fold inside tight bends. Their faces must
  // not distort normals on the supplied road and its three-metre aprons.
  normals=new Vector3[vertices.Length];
  for(int part=1;part<=3;part++)for(int i=0;i<triangles[part].Count;i+=3){
   int a=triangles[part][i],b=triangles[part][i+1],c=triangles[part][i+2];
   var normal=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);normals[a]+=normal;normals[b]+=normal;normals[c]+=normal;
  }
  for(int i=0;i<normals.Length;i++)normals[i]=i%Columns==0||i%Columns==Columns-1?Vector3.up:normals[i].normalized;
  for(int j=0;j<Columns;j++){int end=Points.Count*Columns+j;normals[j]=normals[end]=(normals[j]+normals[end]).normalized;}
  SurfaceMesh.normals=normals;SurfaceMesh.RecalculateTangents();SurfaceMesh.RecalculateBounds();
 }

 public float Nearest(float x,float z,out int index,out float height){
  float best=float.MaxValue;height=0;index=0;
  for(int i=0;i<Points.Count;i++){
   var a=Points[i];var b=Points[(i+1)%Points.Count];float dx=b.x-a.x,dz=b.z-a.z;
   float t=Mathf.Clamp01(((x-a.x)*dx+(z-a.z)*dz)/(dx*dx+dz*dz));float ex=x-a.x-t*dx,ez=z-a.z-t*dz,d=ex*ex+ez*ez;
   if(d<best){best=d;index=i;height=Mathf.Lerp(a.y,b.y,t);}
  }
  return Mathf.Sqrt(best);
 }
 public float Height(float x,float z){Nearest(x,z,out _,out float y);return y;}

 // Query the very same triangles that are rendered, including the lap seam.
 public SurfaceSample Sample(float x,float z){
  float distance=Nearest(x,z,out int segment,out float y);
  var section=Sections[segment];float offset=Vector3.Dot(new Vector3(x,0,z)-section.Center,section.Right);
  var result=new SurfaceSample{Segment=segment,Distance=distance,Offset=offset,Width=offset<0?section.LeftWidth:section.RightWidth,Height=y-3,Normal=Vector3.up};
  if(distance>TerrainExtent+1)return result;
  foreach(int delta in SampleSegments){
   int row=(segment+delta+Points.Count)%Points.Count;
   for(int j=0;j<Columns-1;j++){
    int a=row*Columns+j,b=a+Columns;
    if(Triangle(x,z,a,b,a+1,out var weights))return Interpolate(result,row,j,a,b,a+1,weights);
    if(Triangle(x,z,a+1,b,b+1,out weights))return Interpolate(result,row,j,a+1,b,b+1,weights);
   }
  }
  return result;
 }
 static readonly int[] SampleSegments={0,-1,1,-2,2};
 bool Triangle(float x,float z,int ia,int ib,int ic,out Vector3 weights){
  var a=vertices[ia];var b=vertices[ib]-a;var c=vertices[ic]-a;float px=x-a.x,pz=z-a.z,det=b.x*c.z-c.x*b.z;
  float v=(px*c.z-c.x*pz)/det,w=(b.x*pz-px*b.z)/det;weights=new Vector3(1-v-w,v,w);
  return weights.x>=-.00001f&&v>=-.00001f&&w>=-.00001f;
 }
 SurfaceSample Interpolate(SurfaceSample result,int row,int column,int a,int b,int c,Vector3 weights){
  result.Height=vertices[a].y*weights.x+vertices[b].y*weights.y+vertices[c].y*weights.z;
  result.Normal=(normals[a]*weights.x+normals[b]*weights.y+normals[c]*weights.z).normalized;
  float t=(a/Columns-row)*weights.x+(b/Columns-row)*weights.y+(c/Columns-row)*weights.z;
  var first=Sections[row];var next=Sections[(row+1)%Points.Count];
  var center=Vector3.Lerp(first.Center,next.Center,t);var right=Vector3.Lerp(first.Right,next.Right,t).normalized;
  var point=vertices[a]*weights.x+vertices[b]*weights.y+vertices[c]*weights.z;
  result.Offset=Vector3.Dot(point-center,right);
  result.Width=result.Offset<0?Mathf.Lerp(first.LeftWidth,next.LeftWidth,t):Mathf.Lerp(first.RightWidth,next.RightWidth,t);
  result.OnRoad=column>=2&&column<18;return result;
 }

 public static float BankAcceleration(Vector3 normal,Vector3 right)=>9.81f*normal.y*Vector3.Dot(normal,right);
}
