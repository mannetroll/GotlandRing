using System.Collections.Generic;
using UnityEngine;

public partial class RingDrive
{
 const float KerbWidth=TrackLandmarks.KerbWidth,KerbBlockLength=1.5f,KerbPaintHeight=.025f;
 readonly List<Mesh> kerbMeshes=new List<Mesh>();

 void MakeKerbs(){
  var light=Mat("Kerb weathered white",new Color(.82f,.82f,.78f),.08f);
  var dark=Mat("Kerb weathered charcoal",new Color(.13f,.14f,.14f),.08f);
  foreach(var material in new[]{light,dark}){
   material.SetTexture("_BumpMap",Resources.Load<Texture2D>("Visuals/AsphaltNormal"));
   material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.12f);
  }
  foreach(var run in TrackLandmarks.KerbRuns){
   float start=centerline.Sections[run.First].Distance,end=centerline.Sections[run.Last].Distance;
   int steps=Mathf.CeilToInt((end-start)/(KerbBlockLength/3));
   var vertices=new Vector3[(steps+1)*3];var uv=new Vector2[vertices.Length];
   var triangles=new[]{new List<int>(),new List<int>()};int section=run.First;
   for(int row=0;row<=steps;row++){
    float along=Mathf.Min(row*KerbBlockLength/3,end-start),distance=start+along;
    while(section<run.Last-1&&centerline.Sections[section+1].Distance<distance)section++;
    var first=centerline.Sections[section];var next=centerline.Sections[section+1];
    float t=(distance-first.Distance)/(next.Distance-first.Distance);
    float firstWidth=run.Side<0?first.LeftWidth:first.RightWidth,nextWidth=run.Side<0?next.LeftWidth:next.RightWidth;
    for(int across=0;across<3;across++){
     // Paint follows the apron triangles, including their crossfall. No raised
     // collision step is inferred from footage that does not measure a profile.
     float outside=.02f+across*KerbWidth/2;
     var point=Vector3.Lerp(first.Ground(run.Side*(firstWidth+outside)),next.Ground(run.Side*(nextWidth+outside)),t);
     point.y=centerline.Sample(point.x,point.z).Height+KerbPaintHeight;
     int index=row*3+across;vertices[index]=point;uv[index]=new Vector2(across*KerbWidth/2,along);
     if(row==steps||across==2)continue;
     int a=index,b=a+3,part=(row/3)%2;
     triangles[part].AddRange(run.Side>0?new[]{a,b,a+1,a+1,b,b+1}:new[]{a,a+1,b,a+1,b+1,b});
    }
   }
   var mesh=new Mesh{name="Black-white kerb - "+run.Name,vertices=vertices,uv=uv,subMeshCount=2};
   for(int part=0;part<2;part++)mesh.SetTriangles(triangles[part],part);
   mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();kerbMeshes.Add(mesh);
   var kerb=new GameObject(mesh.name);kerb.AddComponent<MeshFilter>().sharedMesh=mesh;
   kerb.AddComponent<MeshRenderer>().sharedMaterials=new[]{light,dark};
  }
  Debug.Log($"TRACK_KERBS runs={TrackLandmarks.KerbRuns.Length} width={KerbWidth:F2}m block={KerbBlockLength:F2}m source=onboard visual estimates");
 }

 void CheckKerbs(){
  int checkedVertices=0;float maximumGap=0;
  for(int run=0;run<kerbMeshes.Count;run++){
   var mesh=kerbMeshes[run];var vertices=mesh.vertices;
   for(int i=0;i<vertices.Length;i++){
    var point=vertices[i];var sample=drivingSurface.Sample(point.x,point.z);
    float gap=point.y-sample.Height;maximumGap=Mathf.Max(maximumGap,gap);
    if(!float.IsFinite(point.sqrMagnitude)||Mathf.Abs(gap-KerbPaintHeight)>.002f||sample.OnRoad||Mathf.Sign(sample.Offset)!=TrackLandmarks.KerbRuns[run].Side)
     throw new System.Exception($"Kerb is not attached to the correct road edge: {mesh.name}, vertex={i}, gap={gap:F4}");
    checkedVertices++;
   }
   for(int part=0;part<2;part++){
    var triangles=mesh.GetTriangles(part);
    for(int i=0;i<triangles.Length;i+=3){
     var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
     if(Vector3.Cross(b-a,c-a).y<=0)throw new System.Exception("Inverted or folded kerb: "+mesh.name);
    }
   }
  }
  Debug.Log($"KERB_TEST passed: {kerbMeshes.Count} runs, {checkedVertices} grounded vertices, correct road sides, upward faces; maximum paint offset={maximumGap:F3}m");
 }
}
