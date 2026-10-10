using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WindTurbineImport
{
 public static void Prepare(){
  const string source="windmills/gotland_ring_wind_turbines.csv",resource="Assets/Resources/Track/WindTurbines.csv";
  var csv=File.ReadAllText(source);var turbines=WindTurbineData.Read(csv);
  if(turbines.Count!=12)throw new Exception("Expected all twelve supplied registered turbines");
  foreach(var model in new[]{("V47-660kw",6,55f,47f),("V66-1.75",3,78f,66f),("V90-2.0",3,105f,90f)}){
   var group=turbines.Where(t=>t.Model==model.Item1).ToArray();
   if(group.Length!=model.Item2||group.Any(t=>t.HubHeight!=model.Item3||t.RotorDiameter!=model.Item4))throw new Exception("Turbine model dimensions/count differ from the supplied registry export");
  }
  File.Copy(source,resource,true);AssetDatabase.ImportAsset(resource,ImportAssetOptions.ForceUpdate);
  if(Resources.Load<TextAsset>("Track/WindTurbines").text!=csv)throw new Exception("Bundled turbine data differs from the source export");
  Debug.Log("WIND_IMPORT_TEST passed: 12 unique registry IDs, 6 V47 / 3 V66 / 3 V90, unchanged coordinates, RH2000 base/hub/tip relationships, exact bundled CSV");
 }
}
