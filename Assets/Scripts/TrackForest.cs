using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class TrackForest : MonoBehaviour
{
 public struct Tree
 {
  public Vector3 Position;
  public float Height, Width, Shade;
  public bool Mirror;
 }

 public static List<Tree> Read(string csv)
 {
  var trees=new List<Tree>();var culture=CultureInfo.InvariantCulture;
  using(var reader=new StringReader(csv)){
   reader.ReadLine();string line;
   while((line=reader.ReadLine())!=null){
    var f=line.Split(',');
    trees.Add(new Tree{Position=new Vector3(float.Parse(f[0],culture),float.Parse(f[1],culture),float.Parse(f[2],culture)),
     Height=float.Parse(f[3],culture),Width=float.Parse(f[4],culture),Shade=float.Parse(f[5],culture),Mirror=f[6]=="1"});
   }
  }
  return trees;
 }

 public static void Create()
 {
  var root=new GameObject("Mapped Gotland woodland").AddComponent<TrackForest>();
  var tiles=new Dictionary<Vector2Int,List<Tree>>();
  var trees=Read(Resources.Load<TextAsset>("Track/Forest").text);
  foreach(var tree in trees){
   var tile=new Vector2Int(Mathf.FloorToInt(tree.Position.x/96),Mathf.FloorToInt(tree.Position.z/96));
   if(!tiles.TryGetValue(tile,out var group)){group=new List<Tree>();tiles.Add(tile,group);}group.Add(tree);
  }
  var material=Resources.Load<Material>("Visuals/Pine");
  foreach(var tile in tiles)root.MakeTile(tile.Key,tile.Value,material);
  Debug.Log($"TRACK_FOREST trees={trees.Count} tiles={tiles.Count} source=registered aerial canopy");
 }

 void MakeTile(Vector2Int key,List<Tree> trees,Material material)
 {
  int count=trees.Count;var vertices=new Vector3[count*4];var uv=new Vector2[count*4];
  var offsets=new Vector2[count*4];var colors=new Color32[count*4];var triangles=new int[count*6];
  var bounds=new Bounds(trees[0].Position,Vector3.zero);
  for(int i=0;i<count;i++){
   var tree=trees[i];int v=i*4,t=i*6;float half=tree.Width*.5f;
   for(int j=0;j<4;j++){
    bool right=j==1||j==2,top=j>=2;
    vertices[v+j]=tree.Position;
    uv[v+j]=new Vector2(right!=tree.Mirror?1:0,top?1:0);
    offsets[v+j]=new Vector2(right?half:-half,(top?.96f:-.04f)*tree.Height);
    colors[v+j]=new Color(tree.Shade*.97f,tree.Shade,tree.Shade*.94f,1);
   }
   triangles[t]=v;triangles[t+1]=v+2;triangles[t+2]=v+1;
   triangles[t+3]=v;triangles[t+4]=v+3;triangles[t+5]=v+2;
   // Billboards turn in the shader; their bounds must cover every horizontal angle.
   bounds.Encapsulate(tree.Position+new Vector3(-half,-tree.Height*.04f,-half));
   bounds.Encapsulate(tree.Position+new Vector3(half,tree.Height*.96f,half));
  }
  var mesh=new Mesh{name=$"Woodland {key.x},{key.y}"};
  mesh.vertices=vertices;mesh.uv=uv;mesh.uv2=offsets;mesh.colors32=colors;mesh.triangles=triangles;mesh.bounds=bounds;
  mesh.UploadMeshData(true);
  var tile=new GameObject(mesh.name);tile.transform.SetParent(transform,false);
  tile.AddComponent<MeshFilter>().sharedMesh=mesh;
  var renderer=tile.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
  renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
 }
}
