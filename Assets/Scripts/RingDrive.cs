using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class RingDrive : MonoBehaviour
{
 public readonly List<Vector3> track=new List<Vector3>();
 Transform car, head, wheel; Camera cam; BoxerAudio motor;
 Material red, black, silver, asphalt, grass, white, kerbBlue;
 Vector3 velocity; float yaw, steer, lookYaw, lookPitch, throttle, rpm=900, boost, lapStart, best;
 int nearest, lastIndex, checkpoints, lap=1, gear=1, view; bool paused, muted, automatic, smokeBrake, modelPreview, signTest;
 DrivingSettings dynamics, draft; bool settingsOpen, wasPaused; float pauseStarted;
 float fpsElapsed; int fpsFrames; string fpsText="FPS --";
 float length, smokeTime; GUIStyle label, big, small, keyLabel; Texture2D map;
 readonly float[] ratios={3.45f,1.95f,1.37f,1.03f,.78f};
 TrackData centerline;
 AutopilotController autopilot; AutopilotController.Controls pilotControls;
 bool autopilotEnabled, autopilotTest;
 readonly List<Transform> trackSignBoards=new List<Transform>();
 Font trackSignFont; Material trackSignTextMaterial;
 float Ground(float x,float z)=>centerline.Height(x,z);
 Material Mat(string name,Color c,float shine=0){var m=new Material(Resources.Load<Material>("DrivingMaterial"));m.name=name;m.color=c;m.SetFloat("_Glossiness",shine);return m;}
 GameObject Box(string n,Vector3 p,Vector3 s,Material m,Transform parent=null){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=m;Destroy(o.GetComponent<Collider>());return o;}
 void Awake(){
  dynamics=DrivingSettings.Load();
  QualitySettings.vSyncCount=1;Application.targetFrameRate=60;Time.fixedDeltaTime=.01f;
  red=Mat("Cayenne red pearl",new Color(.64f,.024f,.032f),.65f);black=Mat("Charcoal",new Color(.025f,.032f,.039f));silver=Mat("Alloy",new Color(.6f,.65f,.68f),.7f);
  asphalt=Mat("Asphalt",new Color(.19f,.21f,.22f));grass=Mat("Gotland dry meadow",new Color(.42f,.46f,.27f));white=Mat("Paint",new Color(.88f,.88f,.8f));
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.53f,.58f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.70f,.80f,.85f);RenderSettings.fogDensity=.0005f;
  var sun=new GameObject("Baltic afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.15f;sun.transform.rotation=Quaternion.Euler(38,-32,0);sun.shadows=LightShadows.Soft;QualitySettings.shadowDistance=130;
  VisualUpgrade.Surface(asphalt,"Asphalt",.22f,.45f);VisualUpgrade.Surface(grass,"Grass",.05f,.7f);red.SetFloat("_Metallic",.30f);red.SetFloat("_Glossiness",.88f);silver.SetFloat("_Metallic",.85f);
  kerbBlue=Mat("Blue kerbs",new Color(.2f,.55f,.78f));MakeTrack();MakeLandscape();ValidateTreeClearance();BatchScenery();MakeCar();MakeMap();ResetCar(0);
  VisualUpgrade.Lighting(cam,car);
  lapStart=Time.time;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
  automatic=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--smoke-test");
  if(automatic) StartCoroutine(SmokeTest());
  autopilot=new AutopilotController(track,dynamics);
  if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--autopilot"))SetAutopilot(true);
  autopilotTest=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--autopilot-test");
  if(autopilotTest)StartCoroutine(AutopilotTest());
  StartCoroutine(RenderStats());
  if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--settings-test"))StartCoroutine(SettingsTest());
  modelPreview=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--model-preview");if(modelPreview){muted=true;StartCoroutine(ModelPreview());}
  signTest=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--sign-test");if(signTest){muted=true;StartCoroutine(SignTest());}
 }
 void MakeTrack(){
  var asset=Resources.Load<TextAsset>("Track/Centerline");if(!asset)throw new InvalidOperationException("Missing bundled track CSV");
  centerline=TrackData.Load(asset.text);track.AddRange(centerline.Points);length=centerline.HorizontalLength;
  var gravel=Mat("Limestone gravel",Color.white);VisualUpgrade.Surface(gravel,"Gravel",.06f);Ribbon("Gravel runoff",11.5f,gravel,0);Ribbon("Racing surface",7,asphalt,.025f);MakeShoulders();
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
  MakeTrackSigns();
 }
 void MakeTrackSigns(){
  var boardMaterial=Mat("Track sign navy",new Color(.025f,.075f,.11f),.12f);
  trackSignFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
  trackSignTextMaterial=new Material(Resources.Load<Shader>("Visuals/TrackSignText"));Font.textureRebuilt+=RefreshTrackSignFont;
  for(int number=0;number<TrackLandmarks.All.Length;number++){
   var landmark=TrackLandmarks.All[number];int i=landmark.Point;
   var forward=track[(i+4)%track.Count]-track[(i+track.Count-4)%track.Count];forward.y=0;forward.Normalize();
   var position=track[i]+Vector3.Cross(Vector3.up,forward)*17;
   position.y=Ground(position.x,position.z)-3*Mathf.Clamp01((DistanceToTrack(position,out _)-11.4f)/30.6f)+2.9f;
   var sign=new GameObject($"Track sign {number+1:00} - {landmark.Name.Replace("\n"," / ")}").transform;
   sign.position=position;sign.rotation=Quaternion.LookRotation(forward);trackSignBoards.Add(sign);
   Box("Sign border",Vector3.zero,new Vector3(8.8f,2.6f,.18f),white,sign);
   Box("Sign face",new Vector3(0,0,-.12f),new Vector3(8.6f,2.4f,.1f),boardMaterial,sign);
   Box("Sign number panel",new Vector3(-3.45f,0,-.181f),new Vector3(1.4f,2.2f,.015f),kerbBlue,sign);
   TrackSignText("Number",(number+1).ToString("00"),sign,new Vector3(-3.45f,0,-.2f),1.18f,1.2f);
   TrackSignText("Name",landmark.Name,sign,new Vector3(.75f,0,-.2f),6.3f,landmark.Name.Contains("\n")?1.7f:.85f);
   foreach(int side in new[]{-1,1}){
    var foot=sign.TransformPoint(new Vector3(side*3.2f,0,0));float ground=Ground(foot.x,foot.z)-3*Mathf.Clamp01((DistanceToTrack(foot,out _)-11.4f)/30.6f);
    float height=position.y+.9f-ground;Box("Sign post",new Vector3(side*3.2f,ground+height*.5f-position.y,.04f),new Vector3(.14f,height,.14f),silver,sign);
   }
  }
  RefreshTrackSignFont(trackSignFont);
  Debug.Log($"TRACK_SIGNS count={trackSignBoards.Count} side=right source=track_points.jpeg");
 }
 void RefreshTrackSignFont(Font font){if(font==trackSignFont)trackSignTextMaterial.mainTexture=font.material.mainTexture;}
 void OnDestroy(){Font.textureRebuilt-=RefreshTrackSignFont;}
 void TrackSignText(string objectName,string text,Transform sign,Vector3 position,float width,float height){
  var label=new GameObject(objectName).AddComponent<TextMesh>();label.transform.SetParent(sign,false);label.transform.localPosition=position;
  label.font=trackSignFont;label.text=text;label.fontSize=96;label.characterSize=1;label.fontStyle=FontStyle.Bold;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=Color.white;label.GetComponent<MeshRenderer>().sharedMaterial=trackSignTextMaterial;
  var bounds=label.GetComponent<MeshRenderer>().localBounds;label.transform.localScale=Vector3.one*Mathf.Min(width/bounds.size.x,height/bounds.size.y);
 }
 void MakeShoulders(){
  foreach(int side in new[]{-1,1}){var v=new Vector3[track.Count*2];var uv=new Vector2[v.Length];var t=new int[track.Count*6];for(int i=0;i<track.Count;i++){var d=track[(i+1)%track.Count]-track[(i+track.Count-1)%track.Count];d.y=0;var r=Vector3.Cross(Vector3.up,d.normalized)*side;v[i*2]=track[i]+r*11.4f-Vector3.up*.02f;v[i*2+1]=track[i]+r*42-Vector3.up*3;for(int j=0;j<2;j++)uv[i*2+j]=new Vector2(v[i*2+j].x/12,v[i*2+j].z/12);int a=i*2,b=((i+1)%track.Count)*2,k=i*6;t[k]=a;t[k+1]=b;t[k+2]=a+1;t[k+3]=a+1;t[k+4]=b;t[k+5]=b+1;if(side<0){int swap=t[k+1];t[k+1]=t[k+2];t[k+2]=swap;swap=t[k+4];t[k+4]=t[k+5];t[k+5]=swap;}}
  var mesh=new Mesh();mesh.vertices=v;mesh.uv=uv;mesh.triangles=t;mesh.RecalculateNormals();var o=new GameObject("Approximate terrain shoulder");o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=grass;}
 }
 void Ribbon(string n,float width,Material mat,float lift){var verts=new Vector3[track.Count*2];var uv=new Vector2[verts.Length];var tri=new int[track.Count*6];for(int i=0;i<track.Count;i++){var d=(track[(i+1)%track.Count]-track[(i+track.Count-1)%track.Count]).normalized;var r=Vector3.Cross(Vector3.up,d);for(int j=0;j<2;j++){verts[2*i+j]=track[i]+r*width*(j==0?-1:1)+Vector3.up*lift;uv[2*i+j]=new Vector2(j*width*.5f,i*1.1f);}int a=2*i,b=2*((i+1)%track.Count);int t=i*6;tri[t]=a;tri[t+1]=b;tri[t+2]=a+1;tri[t+3]=a+1;tri[t+4]=b;tri[t+5]=b+1;}var mesh=new Mesh();mesh.vertices=verts;mesh.triangles=tri;mesh.uv=uv;mesh.RecalculateNormals();var o=new GameObject(n);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=mat;}
 void Sign(string text,Vector3 p,Vector3 direction,float size){var o=new GameObject(text);o.transform.position=p;o.transform.rotation=Quaternion.LookRotation(-direction);var t=o.AddComponent<TextMesh>();t.text=text;t.fontSize=64;t.characterSize=size*.1f;t.anchor=TextAnchor.MiddleCenter;t.color=Color.white;}
 void MakeLandscape(){
  const int n=220;var vs=new Vector3[(n+1)*(n+1)];var terrainUv=new Vector2[vs.Length];var ts=new int[n*n*6];for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){float px=(x-n/2)*18,pz=(z-n/2)*18;vs[z*(n+1)+x]=new Vector3(px,Ground(px,pz)-3,pz);terrainUv[z*(n+1)+x]=new Vector2(px/12,pz/12);}int ti=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=z*(n+1)+x;ts[ti++]=a;ts[ti++]=a+n+1;ts[ti++]=a+1;ts[ti++]=a+1;ts[ti++]=a+n+1;ts[ti++]=a+n+2;}var mesh=new Mesh();mesh.vertices=vs;mesh.uv=terrainUv;mesh.triangles=ts;mesh.RecalculateNormals();var land=new GameObject("Rolling limestone meadow");land.AddComponent<MeshFilter>().sharedMesh=mesh;land.AddComponent<MeshRenderer>().sharedMaterial=grass;
  var foliage=Mat("Pine foliage",new Color(.17f,.27f,.18f));var bark=Mat("Pine trunks",new Color(.28f,.24f,.19f));var stone=Mat("Limestone",new Color(.61f,.60f,.52f));UnityEngine.Random.InitState(2000);
  for(int i=0;i<700;i++){var p=new Vector3(UnityEngine.Random.Range(-1600,1600),0,UnityEngine.Random.Range(-1600,1600));float dist=DistanceToTrack(p,out _);if(dist<24)continue;p.y=Ground(p.x,p.z)-3;float h=UnityEngine.Random.Range(5,12);VisualUpgrade.Pine(p,h,foliage,bark);}

  for(int i=0;i<track.Count;i+=26){var d=(track[(i+1)%track.Count]-track[i]).normalized;var r=Vector3.Cross(Vector3.up,d);foreach(int side in new[]{-1,1}){var p=track[i]+r*side*UnityEngine.Random.Range(20,65);float h=UnityEngine.Random.Range(3,8);if(DistanceToTrack(p,out _)<24+h*.45f)continue;p.y=Ground(p.x,p.z)-3;VisualUpgrade.Pine(p,h,foliage,bark);}}
  for(int i=0;i<160;i++){var p=new Vector3(UnityEngine.Random.Range(-1400,1400),0,UnityEngine.Random.Range(-1400,1400));if(DistanceToTrack(p,out _)<25)continue;p.y=Ground(p.x,p.z)-3;var b=Box("Quarry stone",p,new Vector3(4,2,3)*UnityEngine.Random.Range(.6f,2),stone);b.transform.rotation=Quaternion.Euler(0,UnityEngine.Random.Range(0,180),0);}
  for(int i=0;i<7;i++){var p=new Vector3(-1150+i*360,0,1200);p.y=Ground(p.x,p.z)-3;Box("Wind turbine tower",p+Vector3.up*37,new Vector3(2.5f,74,2.5f),white);var hub=p+Vector3.up*75;for(int j=0;j<3;j++){float a=j*120*Mathf.Deg2Rad;var blade=Box("Wind turbine blade",hub+new Vector3(Mathf.Sin(a),Mathf.Cos(a),0)*17,new Vector3(2,34,.7f),white);blade.transform.rotation=Quaternion.Euler(0,0,-j*120);}}

  for(int i=0;i<45;i++){var p=track[i];var d=(track[i+1]-p).normalized;var r=Vector3.Cross(Vector3.up,d);var wall=Box("Pit wall",p-r*12+Vector3.up*.6f,new Vector3(.35f,1.2f,Vector3.Distance(p,track[i+1])+.2f),white);wall.transform.rotation=Quaternion.LookRotation(d);Box("Fence post",p-r*12+Vector3.up*2,new Vector3(.07f,2.8f,.07f),silver);}
  var st=track[0];var tangent=(track[1]-st).normalized;var right=Vector3.Cross(Vector3.up,tangent);for(int i=0;i<6;i++){var p=st+right*42+tangent*(i*15-35);p.y=Ground(p.x,p.z);var garage=Box("Pit garage",p,new Vector3(15,6,12),stone);garage.transform.rotation=Quaternion.LookRotation(tangent);var roof=Box("Pit roof",p+Vector3.up*3.3f,new Vector3(16,.6f,13),black);roof.transform.rotation=garage.transform.rotation;}
 }
 void ValidateTreeClearance(){int count=0;float minimum=float.MaxValue;foreach(var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){if(r.name!="Coastal pine billboard")continue;float edge=DistanceToTrack(r.transform.position,out _)-r.transform.localScale.x*.5f;minimum=Mathf.Min(minimum,edge);count++;if(edge<11.5f)throw new InvalidOperationException("Tree overlaps track/runoff: "+r.transform.position);}Debug.Log($"TREE_CLEARANCE_TEST trees={count} minimumCanopyClearanceFromCenterline={minimum:F2} pass=True");}
 void BatchScenery(){var root=new GameObject("Static circuit geometry");foreach(var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){if(r.GetComponent<TextMesh>()||r.name=="Coastal pine billboard")continue;r.transform.SetParent(root.transform,true);}StaticBatchingUtility.Combine(root);}
 void MakeCar(){
  car=new GameObject("2000 Impreza GT - photo-inspired").transform;
  wheel=ImprezaModel.Create(car,red,black,silver);
  head=new GameObject("Driver head").transform;head.SetParent(car,false);head.localPosition=new Vector3(-.4f,1.34f,-.18f);
  cam=new GameObject("Driver camera").AddComponent<Camera>();cam.transform.SetParent(head,false);cam.nearClipPlane=.035f;cam.farClipPlane=3000;cam.fieldOfView=76;cam.backgroundColor=new Color(.66f,.79f,.89f);cam.clearFlags=CameraClearFlags.SolidColor;cam.gameObject.AddComponent<AudioListener>();
  motor=car.gameObject.AddComponent<BoxerAudio>();
 }
 public float DistanceToTrack(Vector3 p,out int index)=>centerline.Nearest(p.x,p.z,out index,out _);
 void ResetCar(int i){nearest=i;car.position=track[i]+Vector3.up*.04f;yaw=Quaternion.LookRotation(track[(i+1)%track.Count]-track[i]).eulerAngles.y;car.rotation=Quaternion.Euler(0,yaw,0);velocity=Vector3.zero;reversing=false;steer=0;lookYaw=lookPitch=0;checkpoints=0;lapStart=Time.time;lastIndex=i;}
 void Update(){
  fpsElapsed+=Time.unscaledDeltaTime;fpsFrames++;
  if(fpsElapsed>=.5f){fpsText=$"{fpsFrames/fpsElapsed:0} FPS  /  {fpsElapsed*1000/fpsFrames:0.0} ms";fpsElapsed=0;fpsFrames=0;}

  if(Input.GetKeyDown(KeyCode.F3)){if(settingsOpen)CloseSettings(false);else OpenSettings();}
  if(Input.GetKeyDown(KeyCode.Escape)){if(settingsOpen)CloseSettings(false);else SetPaused(!paused);}
  if(!settingsOpen && Input.GetKeyDown(KeyCode.P) && ShortcutModifierHeld)SetAutopilot(!autopilotEnabled);
  if(Input.GetKeyDown(KeyCode.F) && ShortcutModifierHeld)ToggleFullscreen();
  if(autopilotEnabled && !paused && !autopilotTest && ManualDrivingInput())SetAutopilot(false);
  if(!settingsOpen && Input.GetKeyDown(KeyCode.M))muted=!muted;if(!settingsOpen && Input.GetKeyDown(KeyCode.C))view=(view+1)%3;if(!settingsOpen && Input.GetKeyDown(KeyCode.R))ResetCar(nearest);if(!settingsOpen && Input.GetKeyDown(KeyCode.Home))ResetCar(0);if(Input.GetKeyDown(KeyCode.F2))CaptureScreenshot("GotlandRing-screenshot.png");
  if(modelPreview){lookYaw=lookPitch=0;}
  if(!paused && !modelPreview && !signTest){lookYaw=Mathf.Clamp(lookYaw+Input.GetAxis("Mouse X")*2,-115,115);lookPitch=Mathf.Clamp(lookPitch-Input.GetAxis("Mouse Y")*1.5f,-45,40);if(Input.GetMouseButtonDown(1))lookYaw=lookPitch=0;}
  if(view==0){head.localPosition=new Vector3(-.4f,1.34f,-.18f);head.localRotation=Quaternion.Euler(lookPitch,lookYaw,-steer*velocity.magnitude*.015f);}
  else{head.localPosition=view==1?new Vector3(0,1.05f,1.75f):new Vector3(0,3.1f,-6.4f);head.localRotation=Quaternion.Euler(view==1?lookPitch:12+lookPitch,lookYaw,0);}
  if(modelPreview && Time.timeSinceLevelLoad>5){head.localPosition=(Time.timeSinceLevelLoad>10?new Vector3(-3.3f,1.95f,-5.1f):new Vector3(3.6f,1.85f,5.1f));head.localRotation=Quaternion.LookRotation(new Vector3(0,.8f,0)-head.localPosition);}
  cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,76+velocity.magnitude*.12f,Time.deltaTime*3);wheel.localRotation=Quaternion.Euler(0,0,-steer*110);
  motor.Rpm=rpm;motor.Load=throttle;motor.Speed=velocity.magnitude;motor.Slip=slip;motor.Muted=muted||paused;
 }
 bool reversing;
 float slip;
 void FixedUpdate(){if(!autopilotTest)StepDriving(Time.fixedDeltaTime,Time.time);}
 void StepDriving(float dt,float now){if(paused||modelPreview||signTest)return;float speed=velocity.magnitude;float dist=DistanceToTrack(car.position,out nearest);bool road=dist<7.5f;float input=((Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.D))?1:0)-((Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.A))?1:0);throttle=(Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.W))?1:0;float brake=(Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.S))?1:0;
  if(autopilotEnabled){pilotControls=autopilot.Drive(car.position,yaw,velocity,nearest,dist);input=pilotControls.Steering;throttle=pilotControls.Throttle;brake=pilotControls.Brake;}
  if(automatic){var aim=track[(nearest+20)%track.Count]-car.position;float angle=Vector3.SignedAngle(car.forward,aim,Vector3.up);input=Mathf.Clamp(angle/18,-1,1);throttle=speed<24?1:0;brake=speed>27?1:0;if(smokeBrake){throttle=0;brake=1;}}
  steer=Mathf.MoveTowards(steer,input,dt*dynamics.response);Vector3 f=new Vector3(Mathf.Sin(yaw*Mathf.Deg2Rad),0,Mathf.Cos(yaw*Mathf.Deg2Rad));Vector3 right=Vector3.Cross(Vector3.up,f);float longitudinal=Vector3.Dot(velocity,f),lateral=Vector3.Dot(velocity,right);
  float wheelRpm=Mathf.Abs(longitudinal)/(.32f*2*Mathf.PI)*60;float target=Mathf.Max(900,wheelRpm*ratios[gear-1]*4.11f);if(target>6400&&gear<5){gear++;target*=.72f;}else if(target<2200&&gear>1){gear--;target*=1.3f;}rpm=Mathf.Lerp(rpm,target+throttle*350,dt*8);boost=Mathf.MoveTowards(boost,throttle*Mathf.InverseLerp(2100,4000,rpm),dt*.7f);
  bool reverseRequested=!automatic && !autopilotEnabled && Input.GetKey(KeyCode.X);
  if(reverseRequested){throttle=1;if(longitudinal>.3f){brake=1;throttle=0;}else reversing=true;}
  else if(throttle>0 && longitudinal<-.3f){brake=1;throttle=0;}
  else if(longitudinal>=-.3f)reversing=false;
  float drive=throttle*dynamics.acceleration*(reversing?-3f:2.6f+boost*3.3f)*Mathf.Clamp01(((reversing?8:72)-speed)/(reversing?2:15));float drag=.10f+speed*speed*.0012f+(road?0:2.5f);longitudinal=Mathf.MoveTowards(longitudinal,0,(drag+brake*dynamics.braking)*dt);longitudinal+=drive*dt;
  float steeringAngle=steer*Mathf.Lerp(dynamics.steering,dynamics.highSpeedSteering,Mathf.Clamp01(speed/65))*Mathf.Deg2Rad;float yawRate=longitudinal/2.52f*Mathf.Tan(steeringAngle);float maxRate=(road?dynamics.grip:dynamics.offRoadGrip)/Mathf.Max(speed,3);yawRate=Mathf.Clamp(yawRate,-maxRate,maxRate);yaw+=yawRate*Mathf.Rad2Deg*dt;
  lateral=Mathf.MoveTowards(lateral,0,(road?dynamics.lateralGrip:dynamics.offRoadGrip)*dt);slip=Mathf.Clamp01(Mathf.Abs(yawRate*speed)/(road?dynamics.grip:dynamics.offRoadGrip))*.6f+(road?0:.3f);f=new Vector3(Mathf.Sin(yaw*Mathf.Deg2Rad),0,Mathf.Cos(yaw*Mathf.Deg2Rad));right=Vector3.Cross(Vector3.up,f);velocity=f*longitudinal+right*lateral;var pos=car.position+velocity*dt;pos.y=Ground(pos.x,pos.z)-3*Mathf.Clamp01((dist-11.4f)/30.6f)+.04f;car.position=pos;float slope=(Ground(pos.x+f.x*2,pos.z+f.z*2)-Ground(pos.x-f.x*2,pos.z-f.z*2))/4;car.rotation=Quaternion.Euler(-Mathf.Atan(slope)*Mathf.Rad2Deg,yaw,-steer*speed*.035f);
  int quarter=track.Count/4;int nextCheckpoint=(checkpoints+1)*quarter;
  if(checkpoints<3 && lastIndex<nextCheckpoint && nearest>=nextCheckpoint && nearest-lastIndex<track.Count/8)checkpoints++;
  if(lastIndex>track.Count*.9f&&nearest<track.Count*.1f&&checkpoints==3){float lapTime=now-lapStart;if(best==0||lapTime<best)best=lapTime;lap++;lapStart=now;checkpoints=0;}lastIndex=nearest;
 }
 void MakeMap(){map=new Texture2D(256,256);var pix=new Color[65536];for(int i=0;i<pix.Length;i++)pix[i]=new Color(.035f,.065f,.08f,.9f);float minX=1e9f,maxX=-1e9f,minZ=1e9f,maxZ=-1e9f;foreach(var p in track){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}mapBounds=new Vector4(minX,maxX,minZ,maxZ);foreach(var p in track){var q=MapPoint(p);for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)pix[Mathf.Clamp((int)q.y+y,0,255)*256+Mathf.Clamp((int)q.x+x,0,255)]=new Color(.55f,.68f,.69f);}map.SetPixels(pix);map.Apply();}
 Vector4 mapBounds;Vector2 MapPoint(Vector3 p)=>new Vector2(128+(p.x-(mapBounds.x+mapBounds.y)*.5f)*232/Mathf.Max(mapBounds.y-mapBounds.x,mapBounds.w-mapBounds.z),128+(p.z-(mapBounds.z+mapBounds.w)*.5f)*232/Mathf.Max(mapBounds.y-mapBounds.x,mapBounds.w-mapBounds.z));
 void OnGUI(){if(label==null){label=new GUIStyle(GUI.skin.label){fontSize=20};label.normal.textColor=Color.white;big=new GUIStyle(label){fontSize=54,fontStyle=FontStyle.Bold};small=new GUIStyle(label){fontSize=13};}float sx=Screen.width/1600f,sy=Screen.height/900f;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(sx,sy,1));GUI.color=new Color(.035f,.055f,.07f,.9f);GUI.DrawTexture(new Rect(25,25,450,88),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(25,735,370,135),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(45,35,420,32),"GOTLAND RING / OPEN PRACTICE",label);GUI.Label(new Rect(45,73,430,28),"IMPREZA 2000 GT 2.0 S  /  PHOTO-INSPIRED",small);
  GUI.Label(new Rect(45,742,180,75),(velocity.magnitude*3.6f).ToString("000"),big);GUI.Label(new Rect(180,787,90,25),"km/h",label);GUI.Label(new Rect(285,746,100,65),(reversing?"R":gear.ToString()),big);GUI.Label(new Rect(45,825,320,25),$"{rpm:0} RPM     BOOST {boost*.9f:0.00} bar",small);
  GUI.color=new Color(.15f,.2f,.24f);GUI.DrawTexture(new Rect(45,815,315,5),Texture2D.whiteTexture);GUI.color=new Color(.96f,.3f,.2f);GUI.DrawTexture(new Rect(45,815,315*Mathf.Clamp01(rpm/7000),5),Texture2D.whiteTexture);GUI.color=Color.white;
  GUI.DrawTexture(new Rect(1340,25,235,235),map);var q=MapPoint(car.position);GUI.color=new Color(1,.3f,.2f);GUI.DrawTexture(new Rect(1340+q.x/256*235-4,25+(1-q.y/256)*235-4,8,8),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(1340,268,255,30),$"LAP {lap}   {Format(Time.time-lapStart)}",label);GUI.Label(new Rect(1340,300,255,30),best>0?"BEST "+Format(best):$"{length/1000:0.000} km / CSV layout",small);
  GUI.color=new Color(.035f,.055f,.07f,.9f);GUI.DrawTexture(new Rect(1340,335,235,36),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(1352,341,215,26),fpsText,label);
  GUI.color=new Color(.035f,.055f,.07f,.9f);GUI.DrawTexture(new Rect(25,163,370,autopilotEnabled?194:40),Texture2D.whiteTexture);GUI.color=autopilotEnabled?new Color(.35f,1,.65f):Color.white;
  GUI.Label(new Rect(45,169,340,30),$"AUTO(P)ILOT  {(autopilotEnabled?(paused?"PAUSED":"ON"):"OFF")}  /  {PilotShortcut}",label);GUI.color=Color.white;
  if(autopilotEnabled){GUI.Label(new Rect(45,204,340,24),$"TARGET {pilotControls.TargetSpeed*3.6f:0} km/h    GAS {pilotControls.Throttle*100:0}%    BRAKE {pilotControls.Brake*100:0}%",small);GUI.Label(new Rect(45,230,340,24),"WASD / arrows / X to take over",small);DrawPilotKeyboard();}
  GUI.Label(new Rect(440,850,880,30),"WASD / ARROWS  Drive     MOUSE  Look     C  Camera     R  Recover     X  Reverse    F3  Dynamics",small);GUI.Label(new Rect(1250,855,350,25),"Mannetroll Solutions AB / Prototype",small);
  if(GUI.Button(new Rect(45,120,170,32),"Dynamics [F3]"))OpenSettings();
  if(GUI.Button(new Rect(225,120,170,32),$"{(Screen.fullScreen?"Windowed":"Fullscreen")} [{ShortcutModifier}+F]"))ToggleFullscreen();
  if(paused && !settingsOpen){GUI.color=new Color(0,0,0,.75f);GUI.DrawTexture(new Rect(450,270,700,300),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(510,310,600,65),"PRACTICE PAUSED",big);GUI.Label(new Rect(510,395,600,100),"Escape to resume  |  Right mouse to center head\nHome: restart lap  |  M: mute  |  F2: screenshot",label);if(GUI.Button(new Rect(510,510,180,40),"Quit"))Application.Quit();if(GUI.Button(new Rect(710,510,220,40),"Driving dynamics"))OpenSettings();}
  if(settingsOpen)DrawSettings();
 }
 void SetPaused(bool value){if(value==paused)return;if(value)pauseStarted=Time.time;else lapStart+=Time.time-pauseStarted;paused=value;Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;}
 void OpenSettings(){if(settingsOpen)return;wasPaused=paused;draft=dynamics.Copy();settingsOpen=true;SetPaused(true);}
 void CloseSettings(bool apply){if(apply){dynamics=draft;dynamics.Save();autopilot.Configure(dynamics);}settingsOpen=false;SetPaused(wasPaused);}
 string PilotShortcut=>ShortcutModifier+"+P";
 void SetAutopilot(bool enabled){autopilotEnabled=enabled;if(enabled)autopilot.Configure(dynamics);else pilotControls=default;Debug.Log("AUTOPILOT "+(enabled?"ON":"OFF"));}
 bool ManualDrivingInput()=>Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.X);
 void DrawPilotKeyboard(){
  float active=paused?0:1;
  DrawPilotKey("W",91,264,pilotControls.Throttle*active,new Color(.1f,.65f,.35f));
  DrawPilotKey("A",45,306,Mathf.Max(0,-steer)*active,new Color(.1f,.55f,.85f));
  DrawPilotKey("S",91,306,pilotControls.Brake*active,new Color(.95f,.4f,.12f));
  DrawPilotKey("D",137,306,Mathf.Max(0,steer)*active,new Color(.1f,.55f,.85f));
  GUI.Label(new Rect(198,267,180,24),paused?"INPUTS PAUSED":"LIVE INPUTS",small);
  GUI.Label(new Rect(198,292,180,24),"W gas   /   S brake",small);
  GUI.Label(new Rect(198,317,180,24),$"A/D steer  {Mathf.Abs(steer)*active*100:0}% {(Mathf.Abs(steer)<.005f||paused?"":steer<0?"L":"R")}",small);
 }
 void DrawPilotKey(string text,float x,float y,float amount,Color color){
  if(keyLabel==null)keyLabel=new GUIStyle(label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
  amount=Mathf.Clamp01(amount);
  GUI.color=amount>.005f?color:new Color(.3f,.37f,.4f);GUI.DrawTexture(new Rect(x,y,40,36),Texture2D.whiteTexture);
  GUI.color=new Color(.07f,.1f,.13f);GUI.DrawTexture(new Rect(x+2,y+2,36,32),Texture2D.whiteTexture);
  GUI.color=color;GUI.DrawTexture(new Rect(x+2,y+34-32*amount,36,32*amount),Texture2D.whiteTexture);
  GUI.color=Color.white;GUI.Label(new Rect(x,y,40,36),text,keyLabel);
 }
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
  draft.response=Setting("Steering response",draft.response,1,9,444);
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
  SetPaused(false);OpenSettings();yield return new WaitForSeconds(2);CaptureScreenshot("dynamics-dialog.png");
  Debug.Log("SETTINGS_TEST passed: cancel, apply, persistence, pause restoration");yield return new WaitForSeconds(2);Application.Quit();
 }
 string Format(float t)=>$"{(int)t/60:00}:{t%60:00.00}";
 IEnumerator RenderStats(){yield return new WaitForSeconds(2);int first=Time.frameCount;float start=Time.realtimeSinceStartup;yield return new WaitForSeconds(5);Debug.Log($"RENDER_STATS fps={(Time.frameCount-first)/(Time.realtimeSinceStartup-start):F1} resolution={Screen.width}x{Screen.height} gpu={SystemInfo.graphicsDeviceName}");}
 void CaptureScreenshot(string filename){var directory=Application.platform==RuntimePlatform.OSXPlayer?Application.persistentDataPath:System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,".."));var path=System.IO.Path.Combine(directory,filename);ScreenCapture.CaptureScreenshot(path);Debug.Log("SCREENSHOT "+path);}
 IEnumerator SignTest(){
  yield return new WaitForSeconds(1);float clearance=float.MaxValue;
  if(trackSignBoards.Count!=42)throw new InvalidOperationException("Expected all 42 numbered track signs");
  for(int n=0;n<trackSignBoards.Count;n++){
   var sign=trackSignBoards[n];int i=TrackLandmarks.All[n].Point;var approaching=track[(i+track.Count-8)%track.Count];
   if(Vector3.Dot(sign.position-track[i],sign.right)<12 || Vector3.Dot(-sign.forward,(approaching-sign.position).normalized)<.4f)throw new InvalidOperationException($"Sign {n+1} is not on the right facing approaching drivers");
   for(int sample=0;sample<=16;sample++){var p=sign.TransformPoint(new Vector3(Mathf.Lerp(-4.4f,4.4f,sample/16f),0,0));clearance=Mathf.Min(clearance,DistanceToTrack(p,out _));}
   foreach(var label in sign.GetComponentsInChildren<TextMesh>()){
    var extent=Vector3.Scale(label.GetComponent<MeshRenderer>().localBounds.extents,label.transform.localScale);
    if(!float.IsFinite(extent.x)||!float.IsFinite(extent.y)||extent.x<=0||extent.y<=0||Mathf.Abs(label.transform.localPosition.x)+extent.x>4.3f||extent.y>1.1f)throw new InvalidOperationException($"Sign {n+1} text does not fit its board");
   }
  }
  if(clearance<11.5f)throw new InvalidOperationException("Track sign overlaps the road or runoff");
  foreach(int n in new[]{0,2,10,30,34,41}){
   ResetCar((TrackLandmarks.All[n].Point+track.Count-8)%track.Count);var direction=trackSignBoards[n].position-(car.position+Vector3.up*1.34f);
   lookYaw=Vector3.SignedAngle(car.forward,new Vector3(direction.x,0,direction.z),Vector3.up);lookPitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
   yield return new WaitForSeconds(1);CaptureScreenshot($"track-sign-{n+1:00}.png");yield return new WaitForSeconds(.5f);
  }
  Debug.Log($"TRACK_SIGNS_TEST passed: {trackSignBoards.Count} boards, right side, approach facing, text fits, minimum clearance={clearance:F2} m");Application.Quit();
 }
 IEnumerator ModelPreview(){yield return new WaitForSeconds(3);CaptureScreenshot("cockpit-detail.png");yield return new WaitForSeconds(5);CaptureScreenshot("model-detail.png");yield return new WaitForSeconds(5);CaptureScreenshot("rear-detail.png");yield return new WaitForSeconds(2);Application.Quit();}
 IEnumerator SmokeTest(){yield return new WaitForSeconds(30);Debug.Log($"SMOKE_TEST speed={velocity.magnitude:F1} distanceFromStart={Vector3.Distance(car.position,track[0]):F1} rpm={rpm:F0} track={track.Count} length={length}");CaptureScreenshot("smoke-test.png");smokeBrake=true;yield return new WaitForSeconds(4);Debug.Log("BRAKE_TEST speed="+velocity.magnitude.ToString("F2")+" pass="+(velocity.magnitude<1));Application.Quit();}
}
