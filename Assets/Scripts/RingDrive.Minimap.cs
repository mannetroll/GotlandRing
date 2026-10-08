using UnityEngine;

public partial class RingDrive
{
 Texture2D map,bankLegend;
 Vector4 mapBounds;
 float mapBankLimit;
 string negativeBankLabel,positiveBankLabel;

 // Coolwarm colors sampled from gotland_ring_surface_validation.png's colorbar.
 // Its symmetric limits keep zero neutral even when the extrema are asymmetric.
 static readonly Color32[] BankColors={
  new Color32(59,76,192,255),new Color32(78,104,216,255),new Color32(98,130,234,255),new Color32(119,154,247,255),
  new Color32(141,176,254,255),new Color32(162,193,255,255),new Color32(185,208,249,255),new Color32(203,216,238,255),
  new Color32(220,221,221,255),new Color32(235,211,198,255),new Color32(244,197,173,255),new Color32(247,177,148,255),
  new Color32(244,154,123,255),new Color32(236,127,99,255),new Color32(222,97,77,255),new Color32(203,62,56,255),
  new Color32(180,4,38,255)
 };

 Color BankColor(float degrees){
  float position=Mathf.InverseLerp(-mapBankLimit,mapBankLimit,degrees)*(BankColors.Length-1);
  int index=Mathf.FloorToInt(position);
  // IMGUI applies the linear project's display transfer to procedural textures.
  return Color.Lerp(BankColors[index],BankColors[Mathf.Min(index+1,BankColors.Length-1)],position-index).linear;
 }

 void MakeMap(){
  float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
  foreach(var section in centerline.Sections){
   var p=section.Center;
   minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);
   minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);
   mapBankLimit=Mathf.Max(mapBankLimit,Mathf.Abs(section.BankingDegrees));
  }
  mapBounds=new Vector4(minX,maxX,minZ,maxZ);
  var pixels=new Color[256*256];
  for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(.035f,.065f,.08f,.9f);
  for(int i=0;i<centerline.Sections.Count;i++){
   var a=centerline.Sections[i];var b=centerline.Sections[(i+1)%centerline.Sections.Count];
   var start=MapPoint(a.Center);var end=MapPoint(b.Center);
   int steps=Mathf.CeilToInt(Vector2.Distance(start,end)*2);
   for(int step=0;step<=steps;step++){
    float t=(float)step/steps;var point=Vector2.Lerp(start,end,t);
    var color=BankColor(Mathf.Lerp(a.BankingDegrees,b.BankingDegrees,t));
    for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)
     pixels[Mathf.RoundToInt(point.y+y)*256+Mathf.RoundToInt(point.x+x)]=color;
   }
  }
  map=new Texture2D(256,256,TextureFormat.RGBA32,false){name="Banking mini-map",wrapMode=TextureWrapMode.Clamp};
  map.SetPixels(pixels);map.Apply();
  var legendPixels=new Color[256];
  for(int i=0;i<legendPixels.Length;i++)legendPixels[i]=BankColor(Mathf.Lerp(-mapBankLimit,mapBankLimit,i/255f));
  bankLegend=new Texture2D(256,1,TextureFormat.RGBA32,false){name="Banking scale",wrapMode=TextureWrapMode.Clamp};
  bankLegend.SetPixels(legendPixels);bankLegend.Apply();
  negativeBankLabel=$"−{mapBankLimit:0.0}°";positiveBankLabel=$"+{mapBankLimit:0.0}°";
 }

 Vector2 MapPoint(Vector3 p){
  float scale=232/Mathf.Max(mapBounds.y-mapBounds.x,mapBounds.w-mapBounds.z);
  return new Vector2(128+(p.x-(mapBounds.x+mapBounds.y)*.5f)*scale,128+(p.z-(mapBounds.z+mapBounds.w)*.5f)*scale);
 }

 void DrawMap(){
  GUI.DrawTexture(new Rect(1340,25,235,235),map);
  GUI.Label(new Rect(1352,30,215,22),"BANKING  /  + right edge higher",small);
  GUI.Label(new Rect(1352,227,60,20),negativeBankLabel,small);
  GUI.Label(new Rect(1450,227,30,20),"0°",small);
  GUI.Label(new Rect(1528,227,45,20),positiveBankLabel,small);
  GUI.DrawTexture(new Rect(1352,248,211,4),bankLegend);
  var q=MapPoint(car.position);float x=1340+q.x/256*235,y=25+(1-q.y/256)*235;
  GUI.color=new Color(.025f,.04f,.05f);
  GUI.DrawTexture(new Rect(x-5,y-5,10,10),Texture2D.whiteTexture);
  GUI.color=Color.white;
  GUI.DrawTexture(new Rect(x-3,y-3,6,6),Texture2D.whiteTexture);
 }
}
