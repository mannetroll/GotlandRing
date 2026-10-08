using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public sealed class WindTurbineData
{
 public int Index;
 public string Id,Model;
 public Vector3 Position;
 public float Elevation,HubHeight,RotorDiameter,HubY,RotorTopY;
 public float Radius=>RotorDiameter*.5f;

 public static List<WindTurbineData> Read(string csv){
  var turbines=new List<WindTurbineData>();var ids=new HashSet<string>();
  using(var reader=new StringReader(csv)){
   var columns=new Dictionary<string,int>();var header=reader.ReadLine().TrimStart('\uFEFF').Split(',');
   for(int i=0;i<header.Length;i++)columns.Add(header[i],i);
   foreach(var name in new[]{"point_index","turbine_id","model","x_m","y_m","z_m","elevation_m","hub_height_m","rotor_diameter_m","hub_y_m","rotor_top_y_m"})
    if(!columns.ContainsKey(name))throw new FormatException("Missing turbine column: "+name);
   string line;while((line=reader.ReadLine())!=null){
    if(string.IsNullOrWhiteSpace(line))continue;var fields=line.Split(',');
    if(fields.Length!=header.Length)throw new FormatException("Incorrect turbine column count");
    string Text(string name)=>fields[columns[name]];
    float Number(string name){float value=float.Parse(Text(name),CultureInfo.InvariantCulture);if(!float.IsFinite(value))throw new FormatException("Non-finite turbine value: "+name);return value;}
    var turbine=new WindTurbineData{
     Index=int.Parse(Text("point_index"),CultureInfo.InvariantCulture),Id=Text("turbine_id"),Model=Text("model"),
     Position=new Vector3(Number("x_m"),Number("y_m"),Number("z_m")),Elevation=Number("elevation_m"),
     HubHeight=Number("hub_height_m"),RotorDiameter=Number("rotor_diameter_m"),HubY=Number("hub_y_m"),RotorTopY=Number("rotor_top_y_m")
    };
    if(turbine.Index!=turbines.Count||string.IsNullOrWhiteSpace(turbine.Id)||!ids.Add(turbine.Id)||string.IsNullOrWhiteSpace(turbine.Model))throw new FormatException("Invalid turbine index, ID or model");
    if(turbine.RotorDiameter<=0||turbine.HubHeight<=turbine.Radius||Mathf.Abs(turbine.Position.y+turbine.HubHeight-turbine.HubY)>.002f||Mathf.Abs(turbine.HubY+turbine.Radius-turbine.RotorTopY)>.002f||Mathf.Abs(turbine.Elevation-38.507350922f-turbine.Position.y)>.002f)
     throw new FormatException("Turbine heights do not match the track datum: "+turbine.Id);
    turbines.Add(turbine);
   }
  }
  return turbines;
 }
}
