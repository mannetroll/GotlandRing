using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class TrackImportChecks
{
 public static void Run(){
  var csv=Resources.Load<TextAsset>("Track/Surface").text;
  if(csv!=File.ReadAllText("track/gotland_ring_full_surface_3m.csv"))throw new Exception("Bundled surface differs from the supplied CSV");
  var source=File.ReadAllLines("track/gotland_ring_full_centerline_3m_lowpass.csv");
  var rows=File.ReadAllLines("track/gotland_ring_full_surface_3m.csv");
  if(source.Length!=rows.Length)throw new Exception("Surface and centerline row counts differ");
  for(int i=0;i<rows.Length;i++){
   var fields=rows[i].Split(',');if(fields.Length!=39)throw new Exception("Expected all 39 supplied surface columns");
   if(string.Join(",",fields,0,12)!=source[i])throw new Exception("Surface changed a source centerline column");
  }
  var data=TrackData.Load(csv);
  try{
   if(data.Points.Count!=2405||Mathf.Abs(data.HorizontalLength-7214.397f)>.01f||Mathf.Abs(data.Length3D-7216.638f)>.1f||Mathf.Abs(data.MinY+19.445f)>.001f||Mathf.Abs(data.MaxY-3.929f)>.001f)throw new Exception("Surface centerline geometry failed");
   var headers=rows[0].Split(',');float maxError=0,minBank=100,maxBank=-100;
   for(int i=0;i<data.Points.Count;i++){
    var row=data.Sections[i];var fields=rows[i+1].Split(',');
    float Get(string key)=>float.Parse(fields[Array.IndexOf(headers,key)],CultureInfo.InvariantCulture);
    foreach(int side in new[]{-1,1}){
     float width=side<0?row.LeftWidth:row.RightWidth;var edge=row.Road(side*width);string name=side<0?"left":"right";
     var expected=new Vector3(Get(name+"_edge_x_m"),Get(name+"_edge_y_m"),Get(name+"_edge_z_m"));
     if(Vector3.Distance(edge,expected)>.002f)throw new Exception("Road edge reconstruction failed at "+i);
     var inside=row.Road(side*(width-.05f));var outside=row.Ground(side*(width+.05f));
     if(!data.Sample(inside.x,inside.z).OnRoad||data.Sample(outside.x,outside.z).OnRoad)throw new Exception($"Asymmetric asphalt boundary failed at {i}, side {side}");
     foreach(float offset in new[]{0,side*width*.63f,side*(width+1.5f)}){
      var p=row.Ground(offset);var sample=data.Sample(p.x,p.z);maxError=Mathf.Max(maxError,Mathf.Abs(p.y-sample.Height));
      if(sample.Normal.y<.7f||Mathf.Abs(sample.Normal.magnitude-1)>.001f)throw new Exception($"Invalid surface normal row={i} offset={offset} normal={sample.Normal} segment={sample.Segment} heightError={p.y-sample.Height}");
     }
    }
    float bank=Mathf.Atan((row.Road(row.RightWidth).y-row.Road(-row.LeftWidth).y)/(row.RightWidth+row.LeftWidth))*Mathf.Rad2Deg;
    if(Mathf.Abs(bank-Get("banking_deg"))>.002f)throw new Exception("Banking was lost or applied twice");
    minBank=Mathf.Min(minBank,bank);maxBank=Mathf.Max(maxBank,bank);
   }
   if(maxError>.01f)throw new Exception($"Rendered/sampled surface mismatch {maxError:F5} m");
   var mesh=data.SurfaceMesh;var vertices=mesh.vertices;var normals=mesh.normals;
   var road=mesh.GetTriangles(2);if(road.Length/3!=76960)throw new Exception("Expected 16 subdivisions across the road");
   for(int part=1;part<=3;part++){
    var triangles=mesh.GetTriangles(part);
    for(int i=0;i<triangles.Length;i+=3){
     var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
     if(Vector3.Cross(b-a,c-a).y<=.00001f)throw new Exception("Inverted or degenerate road/apron triangle");
     if(i%291==0){var p=(a+b+c)/3;var sample=data.Sample(p.x,p.z);if(Mathf.Abs(p.y-sample.Height)>.002f||(part==2&&!sample.OnRoad))throw new Exception("Surface sampling disagrees with triangle interior");}
    }
   }
   for(int j=0;j<TrackData.Columns;j++){
    int last=data.Points.Count*TrackData.Columns+j;
    if(vertices[j]!=vertices[last]||normals[j]!=normals[last])throw new Exception("Surface seam is not closed and smooth");
   }
   Debug.Log($"SURFACE_IMPORT_TEST passed: original columns, asymmetric widths, edge reconstruction, bank={minBank:F3}..{maxBank:F3}deg, 76960 road triangles, winding, seam, maxHeightError={maxError:F5}m");
   Debug.Log("TRACK_IMPORT_TEST passed: source match, count, metric lengths and retained low-pass elevation");
  }finally{UnityEngine.Object.DestroyImmediate(data.SurfaceMesh);}
  // Reject corrupt current-format input at the CSV boundary.
  void Reject(string text){try{var bad=TrackData.Load(text);UnityEngine.Object.DestroyImmediate(bad.SurfaceMesh);}catch(FormatException){return;}throw new Exception("Malformed surface accepted");}
  var first=rows[1].Split(',');first[12]="-1";var changed=(string[])rows.Clone();changed[1]=string.Join(",",first);Reject(string.Join("\n",changed));
  first=rows[1].Split(',');first[17]="NaN";changed[1]=string.Join(",",first);Reject(string.Join("\n",changed));
  changed=(string[])rows.Clone();first=changed[changed.Length-1].Split(',');first[17]="99";changed[changed.Length-1]=string.Join(",",first);Reject(string.Join("\n",changed));
 }
}
