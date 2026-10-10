using System.Collections.Generic;
using UnityEngine;

public partial class RingDrive
{
 void MakeTrack(){
  centerline=TrackData.Load(Resources.Load<TextAsset>("Track/Surface").text);
  quarry=new QuarryTerrain(centerline);
  drivingSurface=new DrivingSurface(centerline,quarry);
  track.AddRange(centerline.Points);length=centerline.HorizontalLength;
  var gravel=Mat("Limestone ground apron",Color.white);VisualUpgrade.Surface(gravel,"Gravel",.06f);
  var road=new GameObject("Estimated banked track surface");
  road.AddComponent<MeshFilter>().sharedMesh=centerline.SurfaceMesh;
  road.AddComponent<MeshCollider>().sharedMesh=centerline.SurfaceMesh;
  road.AddComponent<MeshRenderer>().sharedMaterials=new[]{grass,gravel,asphalt,gravel,grass};
  MakeEdgePaint(-1);MakeEdgePaint(1);MakeKerbs();MakeStartLine();
  MakeTrackSigns();
 }

 void MakeEdgePaint(int side){
  var vertices=new Vector3[(track.Count+1)*2];var uv=new Vector2[vertices.Length];var triangles=new List<int>();
  for(int i=0;i<=track.Count;i++){
   var row=centerline.Sections[i%track.Count];float offset=side<0?-row.LeftWidth+.05f:row.RightWidth-.17f;
   for(int j=0;j<2;j++){int v=i*2+j;vertices[v]=row.Road(offset+j*.12f)+Vector3.up*.012f;uv[v]=new Vector2(j,row.Distance);}
   if(i==track.Count)continue;var next=centerline.Sections[(i+1)%track.Count];
   if(side<0?row.LeftInferred||next.LeftInferred:row.RightInferred||next.RightInferred)continue;
   int a=i*2,b=a+2;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});
  }
  var mesh=new Mesh{name=side<0?"Estimated left edge paint":"Estimated right edge paint",vertices=vertices,uv=uv};mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
  var paint=new GameObject(mesh.name);paint.AddComponent<MeshFilter>().sharedMesh=mesh;paint.AddComponent<MeshRenderer>().sharedMaterial=white;
 }

 void MakeStartLine(){
  int start=TrackLandmarks.StartFinishPoint;
  var first=centerline.Sections[start];var next=centerline.Sections[start+1];
  var vertices=new Vector3[(TrackData.Across+1)*2];var triangles=new List<int>();
  for(int x=0;x<=TrackData.Across;x++)for(int z=0;z<2;z++){
   float t=z*.45f/(next.Distance-first.Distance);
   var p=Vector3.Lerp(first.Road(first.Offset(x+2)),next.Road(next.Offset(x+2)),t);
   p.y=Ground(p.x,p.z)+.025f;vertices[x*2+z]=p;
   if(x<TrackData.Across&&z==0){int a=x*2;triangles.AddRange(new[]{a,a+1,a+2,a+2,a+1,a+3});}
  }
  var mesh=new Mesh{name="Gutemålrakan start / finish paint",vertices=vertices,triangles=triangles.ToArray()};mesh.RecalculateNormals();
  var line=new GameObject(mesh.name);line.AddComponent<MeshFilter>().sharedMesh=mesh;line.AddComponent<MeshRenderer>().sharedMaterial=white;
 }

 void PlaceCarOnSurface(Vector3 position,float bodyRoll){
  var surface=drivingSurface.Sample(position.x,position.z);position.y=surface.Height+.04f;car.position=position;
  var heading=new Vector3(Mathf.Sin(yaw*Mathf.Deg2Rad),0,Mathf.Cos(yaw*Mathf.Deg2Rad));
  car.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(heading,surface.Normal),surface.Normal)*Quaternion.Euler(0,0,bodyRoll);
 }
}
