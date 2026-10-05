using System;
using UnityEngine;
public static class TrackImportChecks {
 public static void Run(){var d=TrackData.Load(Resources.Load<TextAsset>("Track/Centerline").text);
  if(d.Points.Count!=2405 || Mathf.Abs(d.HorizontalLength-7214.397f)>.1f || Mathf.Abs(d.Length3D-7218.329f)>.1f || Mathf.Abs((d.MaxY-d.MinY)-23.685f)>.01f)throw new Exception("CSV geometry validation failed");
  float error=0;foreach(var p in d.Points){d.Nearest(p.x,p.z,out _,out float y);error=Mathf.Max(error,Mathf.Abs(y-p.y));}if(error>.001f)throw new Exception("Height projection failed");
  foreach(var bad in new[]{"x,y,z\n0,0,0", "x_m,y_m,z_m\n0,0,0\n1,0,0\n1,0,1\n0,0,2"}){bool rejected=false;try{TrackData.Load(bad);}catch(FormatException){rejected=true;}if(!rejected)throw new Exception("Invalid CSV accepted");}
  var original=d.Points.ToArray();float range=d.MaxY-d.MinY;d.SmoothLocalHeightNoise();float maxBend=0;
  for(int i=0;i<original.Length;i++){var p=d.Points[i];if(p.x!=original[i].x||p.z!=original[i].z||Mathf.Abs(p.y-original[i].y)>1.001f)throw new Exception("Smoothing moved horizontal coordinates or exceeded correction bound");maxBend=Mathf.Max(maxBend,Mathf.Abs(d.Points[(i+1)%original.Length].y-2*p.y+d.Points[(i+original.Length-1)%original.Length].y));}
  if(Mathf.Abs(d.MaxY-d.MinY-range)>.01f||maxBend>.25f)throw new Exception("Smoothing changed broad elevation range or retained sharp bumps");
  Debug.Log("HEIGHT_SMOOTH_TEST passed: unchanged X/Z, correction bound, preserved relief, reduced short-wave bumps");
  Debug.Log("TRACK_IMPORT_TEST passed: count, metric lengths, elevation, all-point height projection, invalid CSV rejection");
 }
}
