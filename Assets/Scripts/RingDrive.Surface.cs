using System.Collections.Generic;
using UnityEngine;

public partial class RingDrive
{
 void MakeTrack(){
  centerline=TrackData.Load(Resources.Load<TextAsset>("Track/Surface").text);
  track.AddRange(centerline.Points);length=centerline.HorizontalLength;
  var gravel=Mat("Limestone ground apron",Color.white);VisualUpgrade.Surface(gravel,"Gravel",.06f);
  var road=new GameObject("Estimated banked track surface");
  road.AddComponent<MeshFilter>().sharedMesh=centerline.SurfaceMesh;
  road.AddComponent<MeshRenderer>().sharedMaterials=new[]{grass,gravel,asphalt,gravel,grass};
  MakeEdgePaint(-1);MakeEdgePaint(1);MakeStartLine();
  var row=centerline.Sections[0];var forward=Vector3.Cross(row.Right,Vector3.up);
  foreach(int side in new[]{-1,1}){
   var foot=row.Ground(side*(side<0?row.LeftWidth+2:row.RightWidth+2));
   Box("Gantry support",foot+Vector3.up*4,new Vector3(.4f,8,.4f),silver);
  }
  var middle=row.Center+row.Right*(row.RightWidth-row.LeftWidth)*.5f;
  var banner=Box("Start gantry",middle+Vector3.up*8,new Vector3(row.LeftWidth+row.RightWidth+4,1.7f,.4f),black);banner.transform.rotation=Quaternion.LookRotation(forward);
  Sign("GOTLAND RING",middle+Vector3.up*8-forward*.3f,-forward,1.2f);
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
  var first=centerline.Sections[0];var next=centerline.Sections[1];int across=Mathf.CeilToInt(first.LeftWidth+first.RightWidth);
  for(int x=0;x<across;x++)for(int z=0;z<2;z++){
   var vertices=new Vector3[4];
   for(int j=0;j<4;j++){
    float u=(x+j%2)/(float)across,t=(z+j/2)/next.Distance;
    var p=Vector3.Lerp(first.Road(Mathf.Lerp(-first.LeftWidth,first.RightWidth,u)),next.Road(Mathf.Lerp(-next.LeftWidth,next.RightWidth,u)),t);
    p.y=Ground(p.x,p.z)+.018f;vertices[j]=p;
   }
   var mesh=new Mesh{name="Start line tile",vertices=vertices,triangles=new[]{0,2,1,1,2,3}};mesh.RecalculateNormals();
   var tile=new GameObject(mesh.name);tile.AddComponent<MeshFilter>().sharedMesh=mesh;tile.AddComponent<MeshRenderer>().sharedMaterial=(x+z)%2==0?white:black;
  }
 }

 void PlaceCarOnSurface(Vector3 position,float bodyRoll){
  var surface=centerline.Sample(position.x,position.z);position.y=surface.Height+.04f;car.position=position;
  var heading=new Vector3(Mathf.Sin(yaw*Mathf.Deg2Rad),0,Mathf.Cos(yaw*Mathf.Deg2Rad));
  car.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(heading,surface.Normal),surface.Normal)*Quaternion.Euler(0,0,bodyRoll);
 }
}
