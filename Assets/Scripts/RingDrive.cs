using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingDrive : MonoBehaviour
{
 public readonly List<Vector3> track=new List<Vector3>();
 Transform car, head, wheel; Camera cam; BoxerAudio motor;
 Material red, black, silver, asphalt, grass, white, kerbBlue;
 Vector3 velocity; float yaw, steer, lookYaw, lookPitch, throttle, rpm=900, boost, lapStart, best;
 int nearest, lastIndex, checkpoints, lap=1, gear=1, view; bool paused, muted, automatic, smokeBrake, modelPreview;
 DrivingSettings dynamics, draft; bool settingsOpen, wasPaused; float pauseStarted;
 float length, smokeTime; GUIStyle label, big, small; Texture2D map;
 readonly float[] ratios={3.45f,1.95f,1.37f,1.03f,.78f};
 public static float Ground(float x,float z)=>4f*Mathf.Sin(x*.0018f)+6f*Mathf.Sin(z*.002f)+3f*Mathf.Sin((x+z)*.003f);
 Material Mat(string name,Color c,float shine=0){var m=new Material(Resources.Load<Material>("DrivingMaterial"));m.name=name;m.color=c;m.SetFloat("_Glossiness",shine);return m;}
 GameObject Box(string n,Vector3 p,Vector3 s,Material m,Transform parent=null){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=m;Destroy(o.GetComponent<Collider>());return o;}
 void Awake(){
  dynamics=DrivingSettings.Load();
  QualitySettings.vSyncCount=1;Application.targetFrameRate=60;Time.fixedDeltaTime=.01f;
  red=Mat("Cayenne red pearl",new Color(.48f,.035f,.045f),.65f);black=Mat("Charcoal",new Color(.025f,.032f,.039f));silver=Mat("Alloy",new Color(.6f,.65f,.68f),.7f);
  asphalt=Mat("Asphalt",new Color(.19f,.21f,.22f));grass=Mat("Gotland dry meadow",new Color(.42f,.46f,.27f));white=Mat("Paint",new Color(.88f,.88f,.8f));
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.53f,.58f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.70f,.80f,.85f);RenderSettings.fogDensity=.0005f;
  var sun=new GameObject("Baltic afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.15f;sun.transform.rotation=Quaternion.Euler(38,-32,0);sun.shadows=LightShadows.Soft;QualitySettings.shadowDistance=130;
  kerbBlue=Mat("Blue kerbs",new Color(.2f,.55f,.78f));MakeTrack();MakeLandscape();MakeCar();MakeMap();ResetCar(0);
  lapStart=Time.time;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
  automatic=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--smoke-test");
  if(automatic) StartCoroutine(SmokeTest());
  if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--settings-test"))StartCoroutine(SettingsTest());
  modelPreview=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--model-preview");if(modelPreview){muted=true;StartCoroutine(ModelPreview());}
 }
 void MakeTrack(){
  // Hand-traced from the supplied IMG_0284 circuit sign. Full loop, not a survey.
  Vector2[] p={new(675,740),new(820,688),new(998,624),new(980,548),new(947,530),new(810,559),new(686,584),new(665,562),new(665,523),new(691,483),new(680,446),new(658,407),new(662,347),new(677,280),new(733,326),new(753,411),new(782,468),new(892,496),new(920,456),new(941,392),new(930,354),new(881,315),new(880,264),new(926,223),new(1004,187),new(1043,194),new(1053,235),new(1027,282),new(1024,327),new(1062,372),new(1116,349),new(1161,349),new(1227,336),new(1239,356),new(1211,411),new(1145,437),new(1080,449),new(1048,495),new(1038,542),new(1045,580),new(1094,610),new(1091,651),new(1050,697),new(958,753),new(831,779),new(710,810),new(578,901),new(468,966),new(449,995),new(399,999),new(347,977),new(315,941),new(317,899),new(333,803),new(319,689),new(316,585),new(334,560),new(378,560),new(402,595),new(443,653),new(466,704),new(453,756),new(416,852),new(384,890),new(402,918),new(427,889),new(468,815),new(510,729),new(549,709),new(605,725)};
  var raw=new List<Vector3>();for(int i=0;i<p.Length;i++)for(int k=0;k<12;k++){float t=k/12f;Vector2 a=p[(i+p.Length-1)%p.Length],b=p[i],c=p[(i+1)%p.Length],d=p[(i+2)%p.Length];Vector2 v=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);raw.Add(new Vector3(v.x,0,-v.y));}
  float total=0;for(int i=0;i<raw.Count;i++)total+=Vector3.Distance(raw[i],raw[(i+1)%raw.Count]);float scale=7300/total;
  foreach(var v in raw){var q=(v-new Vector3(780,0,-600))*scale;q.y=Ground(q.x,q.z)+.10f;track.Add(q);}
  length=7300;Ribbon("Gravel runoff",11.5f,Mat("Limestone gravel",new Color(.67f,.65f,.53f)),0);Ribbon("Racing surface",7,asphalt,.025f);
  for(int i=0;i<track.Count;i++){
   var a=track[i];var b=track[(i+1)%track.Count];var dir=(b-a).normalized;var right=Vector3.Cross(Vector3.up,dir);
   foreach(int side in new[]{-1,1}){var line=Box("Edge line",(a+b)*.5f+right*6.65f*side+Vector3.up*.05f,new Vector3(.13f,.02f,Vector3.Distance(a,b)+.1f),white);line.transform.rotation=Quaternion.LookRotation(dir);}
   if(i%2==0){var next=(track[(i+4)%track.Count]-b).normalized;if(Vector3.Angle(dir,next)>4){foreach(int side in new[]{-1,1}){var curb=Box("Kerb",a+right*7.2f*side+Vector3.up*.08f,new Vector3(.65f,.12f,Vector3.Distance(a,b)*1.8f),i%4==0?kerbBlue:white);curb.transform.rotation=Quaternion.LookRotation(dir);}}}
  }
  var start=track[0];var forward=(track[1]-start).normalized;var rt=Vector3.Cross(Vector3.up,forward);
  for(int x=0;x<14;x++)for(int z=0;z<2;z++){var tile=Box("Start finish",start+rt*(x-6.5f)+forward*(z-.5f)+Vector3.up*.06f,new Vector3(1,.025f,1),(x+z)%2==0?white:black);tile.transform.rotation=Quaternion.LookRotation(forward);}
  for(int side=-1;side<=1;side+=2)Box("Gantry support",start+rt*side*9+Vector3.up*4,new Vector3(.4f,8,.4f),silver);
  var banner=Box("Start gantry",start+Vector3.up*8,new Vector3(19,1.7f,.4f),black);banner.transform.rotation=Quaternion.LookRotation(forward);
  Sign("GOTLAND RING",start+Vector3.up*8-forward*.3f,-forward,1.2f);
  for(int i=0;i<track.Count;i+=70){var d=(track[(i+1)%track.Count]-track[i]).normalized;Sign("BRAKE / "+(i/70+1),track[i]+Vector3.Cross(Vector3.up,d)*10+Vector3.up*1.7f,-d,.45f);}
 }
 void Ribbon(string n,float width,Material mat,float lift){var verts=new Vector3[track.Count*2];var uv=new Vector2[verts.Length];var tri=new int[track.Count*6];for(int i=0;i<track.Count;i++){var d=(track[(i+1)%track.Count]-track[(i+track.Count-1)%track.Count]).normalized;var r=Vector3.Cross(Vector3.up,d);for(int j=0;j<2;j++){verts[2*i+j]=track[i]+r*width*(j==0?-1:1)+Vector3.up*lift;uv[2*i+j]=new Vector2(j,i*.5f);}int a=2*i,b=2*((i+1)%track.Count);int t=i*6;tri[t]=a;tri[t+1]=b;tri[t+2]=a+1;tri[t+3]=a+1;tri[t+4]=b;tri[t+5]=b+1;}var mesh=new Mesh();mesh.vertices=verts;mesh.triangles=tri;mesh.uv=uv;mesh.RecalculateNormals();var o=new GameObject(n);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=mat;}
 void Sign(string text,Vector3 p,Vector3 direction,float size){var o=new GameObject(text);o.transform.position=p;o.transform.rotation=Quaternion.LookRotation(-direction);var t=o.AddComponent<TextMesh>();t.text=text;t.fontSize=64;t.characterSize=size*.1f;t.anchor=TextAnchor.MiddleCenter;t.color=Color.white;}
 void MakeLandscape(){
  const int n=110;var vs=new Vector3[(n+1)*(n+1)];var ts=new int[n*n*6];for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){float px=(x-n/2)*45,pz=(z-n/2)*45;vs[z*(n+1)+x]=new Vector3(px,Ground(px,pz),pz);}int ti=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=z*(n+1)+x;ts[ti++]=a;ts[ti++]=a+n+1;ts[ti++]=a+1;ts[ti++]=a+1;ts[ti++]=a+n+1;ts[ti++]=a+n+2;}var mesh=new Mesh();mesh.vertices=vs;mesh.triangles=ts;mesh.RecalculateNormals();var land=new GameObject("Rolling limestone meadow");land.AddComponent<MeshFilter>().sharedMesh=mesh;land.AddComponent<MeshRenderer>().sharedMaterial=grass;
  var foliage=Mat("Pine foliage",new Color(.17f,.27f,.18f));var bark=Mat("Pine trunks",new Color(.28f,.24f,.19f));var stone=Mat("Limestone",new Color(.61f,.60f,.52f));UnityEngine.Random.InitState(2000);
  for(int i=0;i<700;i++){var p=new Vector3(UnityEngine.Random.Range(-1600,1600),0,UnityEngine.Random.Range(-1600,1600));float dist=DistanceToTrack(p,out _);if(dist<24)continue;p.y=Ground(p.x,p.z);float h=UnityEngine.Random.Range(5,12);Box("Pine trunk",p+Vector3.up*h*.4f,new Vector3(.5f,h*.8f,.5f),bark);var crown=GameObject.CreatePrimitive(PrimitiveType.Sphere);crown.name="Windswept pine";crown.transform.position=p+Vector3.up*h;crown.transform.localScale=new Vector3(h*.85f,h*.65f,h*.7f);crown.GetComponent<Renderer>().sharedMaterial=foliage;Destroy(crown.GetComponent<Collider>());}
  for(int i=0;i<160;i++){var p=new Vector3(UnityEngine.Random.Range(-1400,1400),0,UnityEngine.Random.Range(-1400,1400));if(DistanceToTrack(p,out _)<25)continue;p.y=Ground(p.x,p.z);var b=Box("Quarry stone",p,new Vector3(4,2,3)*UnityEngine.Random.Range(.6f,2),stone);b.transform.rotation=Quaternion.Euler(0,UnityEngine.Random.Range(0,180),0);}
  for(int i=0;i<7;i++){var p=new Vector3(-1150+i*360,0,1200);p.y=Ground(p.x,p.z);Box("Wind turbine tower",p+Vector3.up*37,new Vector3(2.5f,74,2.5f),white);var hub=p+Vector3.up*75;for(int j=0;j<3;j++){float a=j*120*Mathf.Deg2Rad;var blade=Box("Wind turbine blade",hub+new Vector3(Mathf.Sin(a),Mathf.Cos(a),0)*17,new Vector3(2,34,.7f),white);blade.transform.rotation=Quaternion.Euler(0,0,-j*120);}}

  for(int i=0;i<45;i++){var p=track[i];var d=(track[i+1]-p).normalized;var r=Vector3.Cross(Vector3.up,d);var wall=Box("Pit wall",p-r*12+Vector3.up*.6f,new Vector3(.35f,1.2f,Vector3.Distance(p,track[i+1])+.2f),white);wall.transform.rotation=Quaternion.LookRotation(d);Box("Fence post",p-r*12+Vector3.up*2,new Vector3(.07f,2.8f,.07f),silver);}
  var st=track[0];var tangent=(track[1]-st).normalized;var right=Vector3.Cross(Vector3.up,tangent);for(int i=0;i<6;i++){var p=st+right*42+tangent*(i*15-35);p.y=Ground(p.x,p.z)+3;var garage=Box("Pit garage",p,new Vector3(15,6,12),stone);garage.transform.rotation=Quaternion.LookRotation(tangent);var roof=Box("Pit roof",p+Vector3.up*3.3f,new Vector3(16,.6f,13),black);roof.transform.rotation=garage.transform.rotation;}
 }
 void MakeCar(){
  car=new GameObject("2000 Impreza GT - photo-inspired").transform;
  wheel=ImprezaModel.Create(car,red,black,silver);
  head=new GameObject("Driver head").transform;head.SetParent(car,false);head.localPosition=new Vector3(-.4f,1.34f,-.18f);
  cam=new GameObject("Driver camera").AddComponent<Camera>();cam.transform.SetParent(head,false);cam.nearClipPlane=.035f;cam.farClipPlane=3000;cam.fieldOfView=76;cam.backgroundColor=new Color(.66f,.79f,.89f);cam.clearFlags=CameraClearFlags.SolidColor;cam.gameObject.AddComponent<AudioListener>();
  motor=car.gameObject.AddComponent<BoxerAudio>();
 }
 public float DistanceToTrack(Vector3 p,out int index){float best=1e20f;index=0;for(int i=0;i<track.Count;i++){var a=track[i];var b=track[(i+1)%track.Count];a.y=b.y=p.y;var d=b-a;var q=a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);float s=(p-q).sqrMagnitude;if(s<best){best=s;index=i;}}return Mathf.Sqrt(best);}
 void ResetCar(int i){nearest=i;car.position=track[i]+Vector3.up*.15f;yaw=Quaternion.LookRotation(track[(i+1)%track.Count]-track[i]).eulerAngles.y;car.rotation=Quaternion.Euler(0,yaw,0);velocity=Vector3.zero;reversing=false;steer=0;lookYaw=lookPitch=0;checkpoints=0;lapStart=Time.time;lastIndex=i;}
 void Update(){
  if(Input.GetKeyDown(KeyCode.F3)){if(settingsOpen)CloseSettings(false);else OpenSettings();}
  if(Input.GetKeyDown(KeyCode.Escape)){if(settingsOpen)CloseSettings(false);else SetPaused(!paused);}
  if(!settingsOpen && Input.GetKeyDown(KeyCode.M))muted=!muted;if(!settingsOpen && Input.GetKeyDown(KeyCode.C))view=(view+1)%3;if(!settingsOpen && Input.GetKeyDown(KeyCode.R))ResetCar(nearest);if(!settingsOpen && Input.GetKeyDown(KeyCode.Home))ResetCar(0);if(Input.GetKeyDown(KeyCode.F2))ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../GotlandRing-screenshot.png"));
  if(!paused){lookYaw=Mathf.Clamp(lookYaw+Input.GetAxis("Mouse X")*2,-115,115);lookPitch=Mathf.Clamp(lookPitch-Input.GetAxis("Mouse Y")*1.5f,-45,40);if(Input.GetMouseButtonDown(1))lookYaw=lookPitch=0;}
  if(view==0){head.localPosition=new Vector3(-.4f,1.34f,-.18f);head.localRotation=Quaternion.Euler(lookPitch,lookYaw,-steer*velocity.magnitude*.015f);}
  else{head.localPosition=view==1?new Vector3(0,1.05f,1.75f):new Vector3(0,3.1f,-6.4f);head.localRotation=Quaternion.Euler(view==1?lookPitch:12+lookPitch,lookYaw,0);}
  if(modelPreview && Time.timeSinceLevelLoad>5){head.localPosition=new Vector3(4,2.4f,5.5f);head.localRotation=Quaternion.LookRotation(new Vector3(0,.8f,0)-head.localPosition);}
  cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,76+velocity.magnitude*.12f,Time.deltaTime*3);wheel.localRotation=Quaternion.Euler(0,0,-steer*110);
  motor.Rpm=rpm;motor.Load=throttle;motor.Speed=velocity.magnitude;motor.Slip=slip;motor.Muted=muted||paused;
 }
 bool reversing;
 float slip; void FixedUpdate(){if(paused)return;float dt=Time.fixedDeltaTime;float speed=velocity.magnitude;float dist=DistanceToTrack(car.position,out nearest);bool road=dist<7.5f;float input=((Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.D))?1:0)-((Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.A))?1:0);throttle=(Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.W))?1:0;float brake=(Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.S))?1:0;
  if(automatic){var aim=track[(nearest+7)%track.Count]-car.position;float angle=Vector3.SignedAngle(car.forward,aim,Vector3.up);input=Mathf.Clamp(angle/18,-1,1);throttle=speed<24?1:0;brake=speed>27?1:0;if(smokeBrake){throttle=0;brake=1;}}
  steer=Mathf.MoveTowards(steer,input,dt*dynamics.response);Vector3 f=new Vector3(Mathf.Sin(yaw*Mathf.Deg2Rad),0,Mathf.Cos(yaw*Mathf.Deg2Rad));Vector3 right=Vector3.Cross(Vector3.up,f);float longitudinal=Vector3.Dot(velocity,f),lateral=Vector3.Dot(velocity,right);
  float wheelRpm=Mathf.Abs(longitudinal)/(.32f*2*Mathf.PI)*60;float target=Mathf.Max(900,wheelRpm*ratios[gear-1]*4.11f);if(target>6400&&gear<5){gear++;target*=.72f;}else if(target<2200&&gear>1){gear--;target*=1.3f;}rpm=Mathf.Lerp(rpm,target+throttle*350,dt*8);boost=Mathf.MoveTowards(boost,throttle*Mathf.InverseLerp(2100,4000,rpm),dt*.7f);
  bool reverseRequested=!automatic && Input.GetKey(KeyCode.X);
  if(reverseRequested){throttle=1;if(longitudinal>.3f){brake=1;throttle=0;}else reversing=true;}
  else if(throttle>0 && longitudinal<-.3f){brake=1;throttle=0;}
  else if(longitudinal>=-.3f)reversing=false;
  float drive=throttle*dynamics.acceleration*(reversing?-3f:2.6f+boost*3.3f)*Mathf.Clamp01(((reversing?8:72)-speed)/(reversing?2:15));float drag=.10f+speed*speed*.0012f+(road?0:2.5f);longitudinal=Mathf.MoveTowards(longitudinal,0,(drag+brake*dynamics.braking)*dt);longitudinal+=drive*dt;
  float steeringAngle=steer*Mathf.Lerp(dynamics.steering,dynamics.highSpeedSteering,Mathf.Clamp01(speed/65))*Mathf.Deg2Rad;float yawRate=longitudinal/2.52f*Mathf.Tan(steeringAngle);float maxRate=(road?dynamics.grip:dynamics.offRoadGrip)/Mathf.Max(speed,3);yawRate=Mathf.Clamp(yawRate,-maxRate,maxRate);yaw+=yawRate*Mathf.Rad2Deg*dt;
  lateral=Mathf.MoveTowards(lateral,0,(road?dynamics.lateralGrip:dynamics.offRoadGrip)*dt);slip=Mathf.Clamp01(Mathf.Abs(yawRate*speed)/(road?dynamics.grip:dynamics.offRoadGrip))*.6f+(road?0:.3f);f=new Vector3(Mathf.Sin(yaw*Mathf.Deg2Rad),0,Mathf.Cos(yaw*Mathf.Deg2Rad));right=Vector3.Cross(Vector3.up,f);velocity=f*longitudinal+right*lateral;var pos=car.position+velocity*dt;pos.y=Ground(pos.x,pos.z)+.25f;car.position=pos;float slope=(Ground(pos.x+f.x*2,pos.z+f.z*2)-Ground(pos.x-f.x*2,pos.z-f.z*2))/4;car.rotation=Quaternion.Euler(-Mathf.Atan(slope)*Mathf.Rad2Deg,yaw,-steer*speed*.035f);
  int quarter=track.Count/4;if(nearest>=(checkpoints+1)*quarter&&checkpoints<3)checkpoints++;if(lastIndex>track.Count*.9f&&nearest<track.Count*.1f&&checkpoints==3){float lapTime=Time.time-lapStart;if(best==0||lapTime<best)best=lapTime;lap++;lapStart=Time.time;checkpoints=0;}lastIndex=nearest;
 }
 void MakeMap(){map=new Texture2D(256,256);var pix=new Color[65536];for(int i=0;i<pix.Length;i++)pix[i]=new Color(.035f,.065f,.08f,.9f);float minX=1e9f,maxX=-1e9f,minZ=1e9f,maxZ=-1e9f;foreach(var p in track){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}mapBounds=new Vector4(minX,maxX,minZ,maxZ);foreach(var p in track){var q=MapPoint(p);for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)pix[Mathf.Clamp((int)q.y+y,0,255)*256+Mathf.Clamp((int)q.x+x,0,255)]=new Color(.55f,.68f,.69f);}map.SetPixels(pix);map.Apply();}
 Vector4 mapBounds;Vector2 MapPoint(Vector3 p)=>new Vector2(12+232*Mathf.InverseLerp(mapBounds.x,mapBounds.y,p.x),12+232*Mathf.InverseLerp(mapBounds.z,mapBounds.w,p.z));
 void OnGUI(){if(label==null){label=new GUIStyle(GUI.skin.label){fontSize=20};label.normal.textColor=Color.white;big=new GUIStyle(label){fontSize=54,fontStyle=FontStyle.Bold};small=new GUIStyle(label){fontSize=13};}float sx=Screen.width/1600f,sy=Screen.height/900f;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(sx,sy,1));GUI.color=new Color(.035f,.055f,.07f,.9f);GUI.DrawTexture(new Rect(25,25,450,88),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(25,735,370,135),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(45,35,420,32),"GOTLAND RING / OPEN PRACTICE",label);GUI.Label(new Rect(45,73,430,28),"IMPREZA 2000 GT 2.0 S  /  PHOTO-INSPIRED",small);
  GUI.Label(new Rect(45,742,180,75),(velocity.magnitude*3.6f).ToString("000"),big);GUI.Label(new Rect(180,787,90,25),"km/h",label);GUI.Label(new Rect(285,746,100,65),(reversing?"R":gear.ToString()),big);GUI.Label(new Rect(45,825,320,25),$"{rpm:0} RPM     BOOST {boost*.9f:0.00} bar",small);
  GUI.color=new Color(.15f,.2f,.24f);GUI.DrawTexture(new Rect(45,815,315,5),Texture2D.whiteTexture);GUI.color=new Color(.96f,.3f,.2f);GUI.DrawTexture(new Rect(45,815,315*Mathf.Clamp01(rpm/7000),5),Texture2D.whiteTexture);GUI.color=Color.white;
  GUI.DrawTexture(new Rect(1340,25,235,235),map);var q=MapPoint(car.position);GUI.color=new Color(1,.3f,.2f);GUI.DrawTexture(new Rect(1340+q.x/256*235-4,25+(1-q.y/256)*235-4,8,8),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(1340,268,255,30),$"LAP {lap}   {Format(Time.time-lapStart)}",label);GUI.Label(new Rect(1340,300,255,30),best>0?"BEST "+Format(best):"7.3 km / traced layout",small);
  GUI.Label(new Rect(440,850,880,30),"WASD / ARROWS  Drive     MOUSE  Look     C  Camera     R  Recover     X  Reverse    F3  Dynamics",small);GUI.Label(new Rect(1250,855,350,25),"Mannetroll Solutions AB / Prototype",small);
  if(GUI.Button(new Rect(45,120,170,32),"Dynamics [F3]"))OpenSettings();
  if(paused && !settingsOpen){GUI.color=new Color(0,0,0,.75f);GUI.DrawTexture(new Rect(450,270,700,300),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(510,310,600,65),"PRACTICE PAUSED",big);GUI.Label(new Rect(510,395,600,100),"Escape to resume  |  Right mouse to center head\nHome: restart lap  |  M: mute  |  F2: screenshot",label);if(GUI.Button(new Rect(510,510,180,40),"Quit"))Application.Quit();if(GUI.Button(new Rect(710,510,220,40),"Driving dynamics"))OpenSettings();}
  if(settingsOpen)DrawSettings();
 }
 void SetPaused(bool value){if(value==paused)return;if(value)pauseStarted=Time.time;else lapStart+=Time.time-pauseStarted;paused=value;Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;}
 void OpenSettings(){if(settingsOpen)return;wasPaused=paused;draft=dynamics.Copy();settingsOpen=true;SetPaused(true);}
 void CloseSettings(bool apply){if(apply){dynamics=draft;dynamics.Save();}settingsOpen=false;SetPaused(wasPaused);}
 float Setting(string title,float value,float min,float max,float y,string format="0.0"){
  GUI.Label(new Rect(470,y,350,28),title,label);GUI.Label(new Rect(1060,y,100,28),value.ToString(format),label);
  return GUI.HorizontalSlider(new Rect(810,y+10,225,24),value,min,max);
 }
 void DrawSettings(){
  GUI.color=new Color(.035f,.055f,.07f,.98f);GUI.DrawTexture(new Rect(425,145,750,610),Texture2D.whiteTexture);GUI.color=Color.white;
  GUI.Label(new Rect(470,170,650,40),"DRIVING DYNAMICS",label);
  GUI.Label(new Rect(470,211,650,30),"Driving is paused. Apply saves your setup for the next launch.",small);
  draft.grip=Setting("Cornering grip (m/s²)",draft.grip,10,40,260);
  draft.lateralGrip=Setting("Side-slip recovery (m/s²)",draft.lateralGrip,10,60,306);
  draft.steering=Setting("Low-speed steering (degrees)",draft.steering,25,55,352);
  draft.highSpeedSteering=Setting("High-speed steering (degrees)",draft.highSpeedSteering,8,25,398);
  draft.response=Setting("Steering response",draft.response,2,10,444);
  draft.acceleration=Setting("Acceleration multiplier",draft.acceleration,.5f,1.8f,490,"0.00");
  draft.braking=Setting("Braking (m/s²)",draft.braking,6,20,536);
  draft.offRoadGrip=Setting("Off-road grip (m/s²)",draft.offRoadGrip,3,12,582);
  GUI.Label(new Rect(470,625,650,35),"Higher grip keeps tighter turns; these are arcade handling settings.",small);
  if(GUI.Button(new Rect(470,685,180,40),"Restore defaults"))draft=new DrivingSettings();
  if(GUI.Button(new Rect(735,685,180,40),"Cancel"))CloseSettings(false);
  if(GUI.Button(new Rect(935,685,190,40),"Apply & close"))CloseSettings(true);
 }
 IEnumerator SettingsTest(){
  yield return new WaitForSeconds(2);var original=dynamics.Copy();OpenSettings();draft.grip=39;CloseSettings(false);
  Debug.Assert(dynamics.grip==original.grip && !paused,"Cancel must preserve dynamics and resume");
  OpenSettings();draft.grip=31;CloseSettings(true);Debug.Assert(DrivingSettings.Load().grip==31,"Apply must persist dynamics");
  dynamics=original;dynamics.Save();SetPaused(true);OpenSettings();CloseSettings(false);Debug.Assert(paused,"Dialog must preserve existing pause");
  SetPaused(false);OpenSettings();yield return new WaitForSeconds(2);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../dynamics-dialog.png"));
  Debug.Log("SETTINGS_TEST passed: cancel, apply, persistence, pause restoration");yield return new WaitForSeconds(2);Application.Quit();
 }
 string Format(float t)=>$"{(int)t/60:00}:{t%60:00.00}";
 IEnumerator ModelPreview(){yield return new WaitForSeconds(3);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../cockpit-detail.png"));yield return new WaitForSeconds(5);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../model-detail.png"));yield return new WaitForSeconds(2);Application.Quit();}
 IEnumerator SmokeTest(){yield return new WaitForSeconds(30);Debug.Log($"SMOKE_TEST speed={velocity.magnitude:F1} distanceFromStart={Vector3.Distance(car.position,track[0]):F1} rpm={rpm:F0} track={track.Count} length={length}");ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../smoke-test.png"));smokeBrake=true;yield return new WaitForSeconds(4);Debug.Log("BRAKE_TEST speed="+velocity.magnitude.ToString("F2")+" pass="+(velocity.magnitude<1));Application.Quit();}
}
