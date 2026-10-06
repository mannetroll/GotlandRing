using System;
using UnityEngine;
public static class TrackImportChecks {
 public static void Run(){var csv=Resources.Load<TextAsset>("Track/Centerline").text;
  if(csv!=System.IO.File.ReadAllText("track/gotland_ring_full_centerline_3m_lowpass.csv"))throw new Exception("Bundled centerline differs from the low-pass source CSV");
  var d=TrackData.Load(csv);
  if(d.Points.Count!=2405 || Mathf.Abs(d.HorizontalLength-7214.398f)>.1f || Mathf.Abs(d.Length3D-7216.638f)>.1f || Mathf.Abs(d.MinY+19.445f)>.001f || Mathf.Abs(d.MaxY-3.929f)>.001f)throw new Exception("CSV geometry validation failed");
  float error=0;foreach(var p in d.Points){d.Nearest(p.x,p.z,out _,out float y);error=Mathf.Max(error,Mathf.Abs(y-p.y));}if(error>.001f)throw new Exception("Height projection failed");
  foreach(var bad in new[]{"x,y,z\n0,0,0", "x_m,y_m,z_m\n0,0,0\n1,0,0\n1,0,1\n0,0,2"}){bool rejected=false;try{TrackData.Load(bad);}catch(FormatException){rejected=true;}if(!rejected)throw new Exception("Invalid CSV accepted");}
  Debug.Log("TRACK_IMPORT_TEST passed: low-pass source match, count, metric lengths, elevation, all-point height projection, invalid CSV rejection");
 }
}
