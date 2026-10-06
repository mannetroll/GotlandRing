using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public sealed class TrackData {
 public readonly List<Vector3> Points=new List<Vector3>();
 public float HorizontalLength,Length3D,MinY=float.MaxValue,MaxY=float.MinValue;
 public static TrackData Load(string csv){
  var data=new TrackData();var ci=CultureInfo.InvariantCulture;
  using(var reader=new StringReader(csv)){
   var header=reader.ReadLine().TrimStart('\uFEFF').Split(',');
   int ix=Array.IndexOf(header,"x_m"),iy=Array.IndexOf(header,"y_m"),iz=Array.IndexOf(header,"z_m");
   if(ix<0||iy<0||iz<0)throw new FormatException("Centerline requires x_m, y_m, z_m headers");
   string line;while((line=reader.ReadLine())!=null){if(string.IsNullOrWhiteSpace(line))continue;var f=line.Split(',');
    var p=new Vector3(float.Parse(f[ix],ci),float.Parse(f[iy],ci),float.Parse(f[iz],ci));
    if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z))throw new FormatException("Non-finite track position");data.Points.Add(p);
   }
  }
  if(data.Points.Count<4 || Vector3.Distance(data.Points[0],data.Points[data.Points.Count-1])>.01f)throw new FormatException("Centerline must include its closing row");
  data.Points.RemoveAt(data.Points.Count-1);
  for(int i=0;i<data.Points.Count;i++){var p=data.Points[i];var d=data.Points[(i+1)%data.Points.Count]-p;float h=Mathf.Sqrt(d.x*d.x+d.z*d.z);if(h<.001f)throw new FormatException("Duplicate consecutive centerline point");data.HorizontalLength+=h;data.Length3D+=d.magnitude;data.MinY=Mathf.Min(data.MinY,p.y);data.MaxY=Mathf.Max(data.MaxY,p.y);}
  Debug.Log($"CSV_TRACK points={data.Points.Count} horizontal={data.HorizontalLength:F3} length3d={data.Length3D:F3} minY={data.MinY:F3} maxY={data.MaxY:F3}");return data;
 }
 public float Nearest(float x,float z,out int index,out float height){float best=float.MaxValue;height=0;index=0;
  for(int i=0;i<Points.Count;i++){var a=Points[i];var b=Points[(i+1)%Points.Count];float dx=b.x-a.x,dz=b.z-a.z;float t=Mathf.Clamp01(((x-a.x)*dx+(z-a.z)*dz)/(dx*dx+dz*dz));float ex=x-a.x-t*dx,ez=z-a.z-t*dz;float d=ex*ex+ez*ez;if(d<best){best=d;index=i;height=Mathf.Lerp(a.y,b.y,t);}}
  return Mathf.Sqrt(best);
 }
 public float Height(float x,float z){Nearest(x,z,out _,out float y);return y;}
}
