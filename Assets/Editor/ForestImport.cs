using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ForestImport
{
 const string Output="Assets/Resources/Track/Forest.csv";
 const float OriginU=801.318349f,OriginV=786.115749f;
 const float GridRotation=3.244f*Mathf.Deg2Rad;
 const float Spacing=7;

 // Crop offsets in the 1 m/pixel SWEREF99 orthophoto (see TRACK.md).
 sealed class Canopy
 {
  public readonly int X,Y,Width,Height;
  readonly int[] dark;
  public Canopy(string filename,int x,int y){
   X=x;Y=y;var image=new Texture2D(2,2);
   image.LoadImage(File.ReadAllBytes("track/"+filename+".png"));
   Width=image.width;Height=image.height;var pixels=image.GetPixels32();
   dark=new int[(Width+1)*(Height+1)];
   for(int row=0;row<Height;row++){
    int sum=0;
    for(int col=0;col<Width;col++){
     var p=pixels[(Height-1-row)*Width+col];
     // Dark canopy and its shadows; bright limestone and asphalt remain open.
     int light=(p.r+p.g+p.b)/3;
     if(light>22 && light<108 && p.g>p.r && p.b-p.g<18)sum++;
     dark[(row+1)*(Width+1)+col+1]=dark[row*(Width+1)+col+1]+sum;
    }
   }
   UnityEngine.Object.DestroyImmediate(image);
  }
  public bool Covers(int u,int v)=>u>=X+7 && u<X+Width-7 && v>=Y+7 && v<Y+Height-7;
  public float Density(int u,int v){
   int x=u-X,y=v-Y,s=Width+1;
   int count=dark[(y+7)*s+x+7]-dark[(y-6)*s+x+7]-dark[(y+7)*s+x-6]+dark[(y-6)*s+x-6];
   return Mathf.SmoothStep(0,1,Mathf.InverseLerp(.24f,.76f,count/169f));
  }
 }

 [MenuItem("Gotland Ring/Prepare mapped forest")]
 public static void Prepare()
 {
  var track=TrackData.Load(Resources.Load<TextAsset>("Track/Surface").text);
  var images=new[]{new Canopy("check_north",570,45),new Canopy("check_southwest",10,540),
   new Canopy("check_southeast",680,480),new Canopy("south_grid",0,550)};
  var random=new System.Random(73029);var trees=new List<TrackForest.Tree>();
  var ground=new Dictionary<Vector2Int,float>();
  var signs=new List<Vector3>();
  foreach(var landmark in TrackLandmarks.All){
   int i=landmark.Point;var forward=track.Points[(i+4)%track.Points.Count]-track.Points[(i+track.Points.Count-4)%track.Points.Count];forward.y=0;
   signs.Add(track.Points[i]+Vector3.Cross(Vector3.up,forward.normalized)*Mathf.Max(17,track.Sections[i].RightWidth+9));
  }
  var pitForward=(track.Points[1]-track.Points[0]).normalized;pitForward.y=0;pitForward.Normalize();
  var pitRight=Vector3.Cross(Vector3.up,pitForward);
  float cos=Mathf.Cos(GridRotation),sin=Mathf.Sin(GridRotation),minimum=float.MaxValue;
  for(float v=52;v<1340;v+=Spacing)for(float u=7;u<1502;u+=Spacing){
   float pu=u+(float)(random.NextDouble()-.5)*Spacing*.8f,pv=v+(float)(random.NextDouble()-.5)*Spacing*.8f;
   // Quarry faces, water, buildings and their dark shadows are not tree canopy.
   if(QuarryOrPaddock(pu,pv))continue;
   float density=0;
   foreach(var image in images)if(image.Covers((int)pu,(int)pv)){density=image.Density((int)pu,(int)pv);break;}
   if(random.NextDouble()>=density*.93f)continue;
   float e=pu-OriginU,n=OriginV-pv;var p=new Vector3(e*cos+n*sin,0,-e*sin+n*cos);
   float distance=track.Nearest(p.x,p.z,out _,out _);
   float height=Mathf.Lerp(4.5f,13.5f,(float)random.NextDouble())*Mathf.Lerp(.7f,1,density);
   float width=height*Mathf.Lerp(.52f,.82f,(float)random.NextDouble());
   if(distance-width*.5f<19)continue;
   bool nearSign=false;
   foreach(var sign in signs){var delta=p-sign;delta.y=0;if(delta.magnitude<width*.5f+6){nearSign=true;break;}}
   if(nearSign)continue;
   var fromStart=p-track.Points[0];float along=Vector3.Dot(fromStart,pitForward),across=Vector3.Dot(fromStart,pitRight);
   if(Mathf.Abs(across-42)<width*.5f+10 && along>-45-width*.5f && along<50+width*.5f)continue;
   p.y=SurfaceHeight(track,ground,p.x,p.z);
   if(distance<42)p.y=Mathf.Max(p.y,track.Sample(p.x,p.z).Height);
   minimum=Mathf.Min(minimum,distance-width*.5f);
   trees.Add(new TrackForest.Tree{Position=p,Height=height,Width=width,Shade=Mathf.Lerp(.73f,1,(float)random.NextDouble()),Mirror=random.Next(2)==0});
  }
  var csv=new StringBuilder("x_m,y_m,z_m,height_m,width_m,shade,mirror\n");var culture=CultureInfo.InvariantCulture;
  foreach(var tree in trees)csv.AppendFormat(culture,"{0:F3},{1:F3},{2:F3},{3:F3},{4:F3},{5:F3},{6}\n",
   tree.Position.x,tree.Position.y,tree.Position.z,tree.Height,tree.Width,tree.Shade,tree.Mirror?1:0);
  File.WriteAllText(Output,csv.ToString());AssetDatabase.ImportAsset(Output,ImportAssetOptions.ForceUpdate);
  Validate(track,TrackForest.Read(File.ReadAllText(Output)));
  Debug.Log($"FOREST_IMPORT trees={trees.Count} minimumCanopyClearance={minimum:F2}m source=track aerial crops; video height/spacing reference");
 }

 static bool QuarryOrPaddock(float u,float v)
 {
  return (u>1185 && v>450 && v<660)
   || (u>625 && u<1100 && v>610 && v<760 && v<965-u*.22f)
   || (u>930 && v>930)
   || (u>875 && v>965)
   || (u>790 && v>1040 && v<1250);
 }

 static float SurfaceHeight(TrackData track,Dictionary<Vector2Int,float> samples,float x,float z)
 {
  int ix=Mathf.FloorToInt(x/18),iz=Mathf.FloorToInt(z/18);float fx=x/18-ix,fz=z/18-iz;
  float Sample(int dx,int dz){var key=new Vector2Int(ix+dx,iz+dz);if(!samples.TryGetValue(key,out float h)){h=track.Height(key.x*18,key.y*18)-3;samples.Add(key,h);}return h;}
  // Same two triangles per 18 m cell as RingDrive.MakeLandscape.
  return fx+fz<=1?Sample(0,0)*(1-fx-fz)+Sample(1,0)*fx+Sample(0,1)*fz
   :Sample(1,1)*(fx+fz-1)+Sample(0,1)*(1-fx)+Sample(1,0)*(1-fz);
 }

 static void Validate(TrackData track,List<TrackForest.Tree> trees)
 {
  int north=0,south=0;float minimum=float.MaxValue;
  foreach(var tree in trees){
   float clearance=track.Nearest(tree.Position.x,tree.Position.z,out _,out _)-tree.Width*.5f;
   minimum=Mathf.Min(minimum,clearance);
   if(clearance<18.99f || tree.Height<3 || tree.Height>14)throw new InvalidOperationException("Mapped forest clearance or scale is invalid");
   if(tree.Position.z>250)north++;if(tree.Position.z<0)south++;
  }
  if(north<2000 || south<1000)throw new InvalidOperationException("Mapped woodland is missing a circuit sector");
  Debug.Log($"FOREST_IMPORT_CHECK passed: {north} northern trees, {south} southern trees, minimum canopy clearance={minimum:F2}m");
 }
}
