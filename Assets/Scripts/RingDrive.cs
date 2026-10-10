using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class RingDrive : MonoBehaviour
{
 public readonly List<Vector3> track=new List<Vector3>();
 Transform car, head; Camera cam; BoxerAudio motor; ImprezaModel carModel;
 ImprezaModel[] carModels;int selectedCar;
 Material black, silver, asphalt, grass, white, kerbBlue;
 Vector3 velocity; float yaw, steer, lookYaw, lookPitch, chaseSlipYaw, throttle, rpm=900, boost, lapStart, best;
 int nearest, lastIndex, checkpoints, lap=1, gear=1, view=2; bool paused, muted, automatic, smokeBrake, modelPreview, signTest, surfaceTest;
 DrivingSettings dynamics, draft; bool settingsOpen, wasPaused; float pauseStarted;
 float fpsElapsed; int fpsFrames; string fpsText="FPS --";
 float length, smokeTime; GUIStyle label, big, small, keyLabel;
 readonly float[] ratios={3.45f,1.95f,1.37f,1.03f,.78f};
 TrackData centerline;
 QuarryTerrain quarry;
 AutopilotController autopilot; AutopilotController.Controls pilotControls;
 bool autopilotEnabled, autopilotTest;
 readonly List<Transform> trackSignBoards=new List<Transform>();
 Font trackSignFont; Material trackSignTextMaterial;
 float Ground(float x,float z)=>centerline.Sample(x,z).Height;
 Material Mat(string name,Color c,float shine=0){var m=new Material(Resources.Load<Material>("DrivingMaterial"));m.name=name;m.color=c;m.SetFloat("_Glossiness",shine);return m;}
 GameObject Box(string n,Vector3 p,Vector3 s,Material m,Transform parent=null){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=m;Destroy(o.GetComponent<Collider>());return o;}
 void Awake(){
  dynamics=DrivingSettings.Load();
  var args=Environment.GetCommandLineArgs();
  if(Array.Exists(args,x=>x=="--arcade"||x=="--autopilot-test"||x=="--surface-test"||x=="--car-switch-test"||x=="--model-preview"||x=="--sign-test"))dynamics.awdMode=false;
  awdTest=Array.Exists(args,x=>x=="--awd-test");
  if(awdTest||Array.Exists(args,x=>x=="--awd"))dynamics.awdMode=true;
  QualitySettings.vSyncCount=1;Application.targetFrameRate=60;Time.fixedDeltaTime=.01f;
  black=Mat("Charcoal",new Color(.025f,.032f,.039f));silver=Mat("Alloy",new Color(.6f,.65f,.68f),.7f);
  asphalt=Mat("Asphalt",new Color(.19f,.21f,.22f));grass=Mat("Gotland dry meadow",new Color(.42f,.46f,.27f));white=Mat("Paint",new Color(.88f,.88f,.8f));
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.53f,.58f);RenderSettings.fog=true;RenderSettings.fogColor=new Color(.70f,.80f,.85f);RenderSettings.fogDensity=.0005f;
  var sun=new GameObject("Baltic afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.15f;sun.transform.rotation=Quaternion.Euler(38,-32,0);sun.shadows=LightShadows.Soft;QualitySettings.shadowDistance=130;
  VisualUpgrade.Surface(asphalt,"Asphalt",.22f,.45f);VisualUpgrade.Surface(grass,"Grass",.05f,.7f);silver.SetFloat("_Metallic",.85f);
  kerbBlue=Mat("Blue kerbs",new Color(.2f,.55f,.78f));MakeTrack();MakeLandscape();BatchScenery();MakeCar();MakeAwdCar();MakeMap();RecoverCar(TrackLandmarks.StartFinishPoint);
  VisualUpgrade.Lighting(cam,car);
  lapStart=Time.time;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
  automatic=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--smoke-test");
  if(automatic) StartCoroutine(SmokeTest());
  autopilot=new AutopilotController(centerline,dynamics.awdMode?awd.PilotSettings(dynamics):dynamics);
  referenceLine=autopilot.RacingLine;
  if(Array.Exists(args,x=>x=="--training"))ToggleTrainingArea();
  if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--autopilot"))SetAutopilot(true);
  autopilotTest=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--autopilot-test");
  if(autopilotTest)StartCoroutine(AutopilotTest());
  if(awdTest)StartCoroutine(AwdTest());
  StartCoroutine(RenderStats());
  if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--settings-test"))StartCoroutine(SettingsTest());
  modelPreview=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--model-preview");if(modelPreview){muted=true;StartCoroutine(ModelPreview());}
  if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--car-switch-test")){modelPreview=true;muted=true;StartCoroutine(CarSwitchTest());}
  if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--wind-test")){modelPreview=true;muted=true;StartCoroutine(WindTest());}
  if(Array.Exists(args,x=>x=="--instruments-test")){modelPreview=true;muted=true;StartCoroutine(InstrumentsTest());}
  if(Array.Exists(args,x=>x=="--feedback-test")){modelPreview=true;muted=true;StartCoroutine(FeedbackTest());}
  if(Array.Exists(args,x=>x=="--reference-lap-test")){modelPreview=true;StartCoroutine(ReferenceLapTest());}
  if(Array.Exists(args,x=>x=="--reference-lap"))ToggleReferenceLap();
  if(Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--scenery-test")){modelPreview=true;muted=true;StartCoroutine(SceneryTest());}
  signTest=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--sign-test");if(signTest){muted=true;StartCoroutine(SignTest());}
  surfaceTest=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--surface-test");if(surfaceTest)StartCoroutine(SurfaceTest());
 }
 void MakeTrackSigns(){
  var boardMaterial=Mat("Track sign navy",new Color(.025f,.075f,.11f),.12f);
  trackSignFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
  trackSignTextMaterial=new Material(Resources.Load<Shader>("Visuals/TrackSignText"));Font.textureRebuilt+=RefreshTrackSignFont;
  for(int number=0;number<TrackLandmarks.All.Length;number++){
   var landmark=TrackLandmarks.All[number];int i=landmark.Point;
   var forward=track[(i+4)%track.Count]-track[(i+track.Count-4)%track.Count];forward.y=0;forward.Normalize();
   var position=track[i]+Vector3.Cross(Vector3.up,forward)*Mathf.Max(17,centerline.Sections[i].RightWidth+9);
   position.y=Ground(position.x,position.z)+2.9f;
   var sign=new GameObject($"Track sign {number+1:00} - {landmark.Name.Replace("\n"," / ")}").transform;
   sign.position=position;sign.rotation=Quaternion.LookRotation(forward);trackSignBoards.Add(sign);
   Box("Sign border",Vector3.zero,new Vector3(8.8f,2.6f,.18f),white,sign);
   Box("Sign face",new Vector3(0,0,-.12f),new Vector3(8.6f,2.4f,.1f),boardMaterial,sign);
   Box("Sign number panel",new Vector3(-3.45f,0,-.181f),new Vector3(1.4f,2.2f,.015f),kerbBlue,sign);
   TrackSignText("Number",(number+1).ToString("00"),sign,new Vector3(-3.45f,0,-.2f),1.18f,1.2f);
   TrackSignText("Name",landmark.Name,sign,new Vector3(.75f,0,-.2f),6.3f,landmark.Name.Contains("\n")?1.7f:.85f);
   foreach(int side in new[]{-1,1}){
    var foot=sign.TransformPoint(new Vector3(side*3.2f,0,0));float ground=Ground(foot.x,foot.z);
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
 void MakeLandscape(){
  const int n=220;var vs=new Vector3[(n+1)*(n+1)];var terrainUv=new Vector2[vs.Length];var ts=new int[n*n*6];for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){float px=(x-n/2)*18,pz=(z-n/2)*18;vs[z*(n+1)+x]=new Vector3(px,centerline.Height(px,pz)-3,pz);terrainUv[z*(n+1)+x]=new Vector2(px/12,pz/12);}int ti=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=z*(n+1)+x;ts[ti++]=a;ts[ti++]=a+n+1;ts[ti++]=a+1;ts[ti++]=a+1;ts[ti++]=a+n+1;ts[ti++]=a+n+2;}var mesh=new Mesh();mesh.vertices=vs;mesh.uv=terrainUv;mesh.triangles=ts;mesh.RecalculateNormals();var land=new GameObject("Rolling limestone meadow");land.AddComponent<MeshFilter>().sharedMesh=mesh;land.AddComponent<MeshRenderer>().sharedMaterial=grass;land.AddComponent<MeshCollider>().sharedMesh=mesh;
  var limestone=new Material(Resources.Load<Shader>("Visuals/QuarryLimestone")){name="Quarry limestone and sand"};limestone.mainTexture=Resources.Load<Texture2D>("Visuals/Gravel");
  foreach(var patch in quarry.Patches){
   var bank=new GameObject(patch.Mesh.name);bank.AddComponent<MeshFilter>().sharedMesh=patch.Mesh;
   bank.AddComponent<MeshRenderer>().sharedMaterial=limestone;bank.AddComponent<MeshCollider>().sharedMesh=patch.Mesh;
  }
  TrackForest.Create();
  var stone=Mat("Limestone",new Color(.61f,.60f,.52f));UnityEngine.Random.InitState(2000);
  for(int i=0;i<160;i++){
   var p=new Vector3(UnityEngine.Random.Range(-1400,1400),0,UnityEngine.Random.Range(-1400,1400));if(DistanceToTrack(p,out _)<25)continue;
   float size=UnityEngine.Random.Range(.6f,2),angle=UnityEngine.Random.Range(0,180);
   if(QuarryTerrain.BareGround(p.x,p.z))continue;
   p.y=SceneryGroundHeight(p.x,p.z);var b=Box("Quarry stone",p,new Vector3(4,2,3)*size,stone);b.transform.rotation=Quaternion.Euler(0,angle,0);
  }
  MakeWindTurbines();

  MakeMainBuilding();MakeTrackBarriers();
 }
 void BatchScenery(){var root=new GameObject("Static circuit geometry");foreach(var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){if(r.GetComponent<TextMesh>()||r.GetComponentInParent<TrackForest>()||turbineRotors.Contains(r.transform.parent))continue;r.transform.SetParent(root.transform,true);}StaticBatchingUtility.Combine(root);}
 void MakeCar(){
  car=new GameObject("Player car").transform;
  carModels=new ImprezaModel[2];
  var prefabs=new[]{"RallyCar","SubaruImpreza"};
  for(int i=0;i<prefabs.Length;i++){
   carModels[i]=Instantiate(Resources.Load<GameObject>(prefabs[i]),car,false).GetComponent<ImprezaModel>();
   carModels[i].gameObject.SetActive(i==0);
  }
  carModel=carModels[0];
  head=new GameObject("Driver head").transform;head.SetParent(car,false);head.localPosition=carModel.cockpitView.localPosition;
  cam=new GameObject("Driver camera").AddComponent<Camera>();cam.transform.SetParent(head,false);cam.nearClipPlane=.035f;cam.farClipPlane=3000;cam.fieldOfView=76;cam.backgroundColor=new Color(.66f,.79f,.89f);cam.clearFlags=CameraClearFlags.SolidColor;cam.gameObject.AddComponent<AudioListener>();
  motor=car.gameObject.AddComponent<BoxerAudio>();
 }
 void SwitchCarModel(){
  ResetFeedback();
  carModels[selectedCar].gameObject.SetActive(false);
  selectedCar=(selectedCar+1)%carModels.Length;
  carModel=carModels[selectedCar];carModel.gameObject.SetActive(true);
  AnimateCar(0);
  UpdateCameraPose();
  Debug.Log("CAR_MODEL "+carModel.displayName);
 }
 public float DistanceToTrack(Vector3 p,out int index)=>centerline.Nearest(p.x,p.z,out index,out _);
 void RecoverCar(int i){
  ResetFeedback();
  if(drivingSurface.Training){RecoverTrainingCar();return;}
  nearest=i;yaw=Quaternion.LookRotation(track[(i+1)%track.Count]-track[i]).eulerAngles.y;
  if(dynamics.awdMode){
   var p=track[i];var normal=centerline.Sample(p.x,p.z).Normal;
   var heading=Vector3.ProjectOnPlane(track[(i+1)%track.Count]-p,normal);
   awd.ResetCar(p+normal*.30f,Quaternion.LookRotation(heading,normal));
  }else PlaceCarOnSurface(track[i]+Vector3.up*.04f,0);
  velocity=Vector3.zero;reversing=false;steer=throttle=boost=0;rpm=900;gear=1;lookYaw=lookPitch=chaseSlipYaw=0;lastIndex=i;ResetMouseSteering();
 }
 void RestartLap(){RecoverCar(TrackLandmarks.StartFinishPoint);checkpoints=0;lapStart=Time.time;if(paused)pauseStarted=Time.time;}
 void Update(){
  if(!settingsOpen && Input.GetKeyDown(KeyCode.O))ToggleReferenceLap();
  if(referenceLap){UpdateReferenceLap();return;}
  if(!paused)AnimateWindTurbines(Time.deltaTime);
  fpsElapsed+=Time.unscaledDeltaTime;fpsFrames++;
  if(fpsElapsed>=.5f){fpsText=$"{fpsFrames/fpsElapsed:0} FPS  /  {fpsElapsed*1000/fpsFrames:0.0} ms";fpsElapsed=0;fpsFrames=0;}

  if(!settingsOpen&&Input.GetKeyDown(KeyCode.F4))ToggleHandlingMode();
  if(Input.GetKeyDown(KeyCode.F3)){if(settingsOpen)CloseSettings(false);else OpenSettings();}
  if(Input.GetKeyDown(KeyCode.Escape)){if(settingsOpen)CloseSettings(false);else SetPaused(!paused);}
  if(!settingsOpen && Input.GetKeyDown(KeyCode.P) && ShortcutModifierHeld)SetAutopilot(!autopilotEnabled);
  if(!settingsOpen && Input.GetKeyDown(KeyCode.U))ToggleMouseSteering();
  if(!settingsOpen && Input.GetKeyDown(KeyCode.Z))ToggleTrainingArea();
  if(Input.GetKeyDown(KeyCode.F) && ShortcutModifierHeld)ToggleFullscreen();
  if(!settingsOpen && Input.GetKeyDown(KeyCode.Y))SwitchCarModel();
  if(!settingsOpen && Input.GetKeyDown(KeyCode.T) && carModel.CanChangePaint)carModel.CyclePaint();
  if(!settingsOpen && Input.GetKeyDown(KeyCode.M))muted=!muted;if(!settingsOpen && Input.GetKeyDown(KeyCode.C)){view=(view+1)%3;lookYaw=lookPitch=0;}if(!settingsOpen && Input.GetKeyDown(KeyCode.R))RecoverCar(nearest);if(!settingsOpen && Input.GetKeyDown(KeyCode.Home))RestartLap();if(Input.GetKeyDown(KeyCode.F2))CaptureScreenshot("GotlandRing-screenshot.png");
  UpdatePointer(new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y")),Input.GetMouseButtonDown(1));
  if(!modelPreview){
   UpdateCameraPose();
   AnimateCar(paused?0:Time.deltaTime);
  }
  cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,76+velocity.magnitude*.12f,Time.deltaTime*3);
  motor.Rpm=rpm;motor.Load=throttle;motor.Speed=velocity.magnitude;motor.Muted=muted||paused;
 }
 void UpdateCameraPose(){
  if(view<2){var anchor=view==0?carModel.cockpitView:carModel.bonnetView;head.localPosition=anchor.localPosition;head.localRotation=Quaternion.Euler(anchor.localEulerAngles.x+lookPitch,lookYaw,0);ApplyCameraFeedback();}
  else{
   float slipYaw=dynamics.awdMode&&awd.ForwardSpeed>8?Mathf.Clamp(awd.SideslipDegrees,-20,20):0;
   if(!paused)chaseSlipYaw=Mathf.Lerp(chaseSlipYaw,slipYaw,1-Mathf.Exp(-Time.deltaTime*5));
   var orbit=Quaternion.Euler(18+lookPitch,lookYaw+chaseSlipYaw,0);
   head.localPosition=new Vector3(0,.7f,0)+orbit*new Vector3(0,0,-5.5f);
   head.localRotation=orbit;
  }
 }
 bool reversing;
 void FixedUpdate(){
  if(!autopilotTest&&!awdTest){
   StepDriving(Time.fixedDeltaTime,Time.time);
   if(!paused&&!modelPreview&&!signTest&&!surfaceTest)UpdateDrivingFeedback(Time.fixedDeltaTime);
  }
 }
 void StepDriving(float dt,float now){
  float keyboard=((Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.D))?1:0)-((Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.A))?1:0);
  StepDriving(dt,now,keyboard,(Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.W))?1:0,
   (Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.S))?1:0,Input.GetKey(KeyCode.X));
 }
 void StepDriving(float dt,float now,float keyboardSteering,float accelerator,float brake,bool reverseRequested){if(paused||modelPreview||signTest||surfaceTest)return;if(dynamics.awdMode)SyncAwdMotion();float speed=velocity.magnitude;var surface=drivingSurface.Sample(car.position.x,car.position.z);nearest=surface.Segment;bool road=surface.OnRoad;float input=mouseSteering?mouseSteer:keyboardSteering;throttle=accelerator;
  if(autopilotEnabled){pilotControls=autopilot.Drive(car.position,yaw,velocity,surface);if(dynamics.awdMode)pilotControls=awd.CorrectPilot(pilotControls,dynamics);input=pilotControls.Steering;throttle=pilotControls.Throttle;brake=pilotControls.Brake;}
  if(automatic){var aim=track[(nearest+20)%track.Count]-car.position;float angle=Vector3.SignedAngle(car.forward,aim,Vector3.up);input=Mathf.Clamp(angle/18,-1,1);throttle=speed<24?1:0;brake=speed>27?1:0;if(smokeBrake){throttle=0;brake=1;}}
  reverseRequested=reverseRequested&&!automatic&&!autopilotEnabled&&throttle<=0;
  if(dynamics.awdMode){StepAwdDriving(dt,input,brake,reverseRequested);UpdateLap(now);return;}
  steer=Mathf.MoveTowards(steer,input,dt*dynamics.response);Vector3 f=new Vector3(Mathf.Sin(yaw*Mathf.Deg2Rad),0,Mathf.Cos(yaw*Mathf.Deg2Rad));Vector3 right=Vector3.Cross(Vector3.up,f);float longitudinal=Vector3.Dot(velocity,f),lateral=Vector3.Dot(velocity,right);
  float wheelRpm=Mathf.Abs(longitudinal)/(.32f*2*Mathf.PI)*60;float target=Mathf.Max(900,wheelRpm*ratios[gear-1]*4.11f);if(target>6400&&gear<5){gear++;target*=.72f;}else if(target<2200&&gear>1){gear--;target*=1.3f;}rpm=Mathf.Lerp(rpm,target+throttle*350,dt*8);boost=Mathf.MoveTowards(boost,throttle*Mathf.InverseLerp(2100,4000,rpm),dt*.7f);
  if(reverseRequested){throttle=1;if(longitudinal>.3f){brake=1;throttle=0;}else reversing=true;}
  else if(throttle>0 || longitudinal>=-.3f)reversing=false;
  float drive=throttle*dynamics.acceleration*(reversing?-3f:2.6f+boost*3.3f)*Mathf.Clamp01(((reversing?8:72)-speed)/(reversing?2:15));float drag=DrivingSettings.ArcadeDrag(speed,road);longitudinal=Mathf.MoveTowards(longitudinal,0,(drag+brake*dynamics.braking)*dt);longitudinal+=drive*dt;
  float steeringAngle=steer*dynamics.SteeringLimit(speed)*Mathf.Deg2Rad;float yawRate=longitudinal/2.52f*Mathf.Tan(steeringAngle);float grip=road?dynamics.grip:dynamics.offRoadGrip,bank=TrackData.BankAcceleration(surface.Normal,right);float limitSpeed=Mathf.Max(speed,3),travelBank=longitudinal<0?-bank:bank;yawRate=Mathf.Clamp(yawRate,(travelBank-grip)/limitSpeed,(travelBank+grip)/limitSpeed);yaw+=yawRate*Mathf.Rad2Deg*dt;
  lateral=Mathf.MoveTowards(lateral,0,(road?dynamics.lateralGrip:dynamics.offRoadGrip)*dt);f=new Vector3(Mathf.Sin(yaw*Mathf.Deg2Rad),0,Mathf.Cos(yaw*Mathf.Deg2Rad));right=Vector3.Cross(Vector3.up,f);velocity=f*longitudinal+right*lateral;PlaceCarOnSurface(car.position+velocity*dt,-steer*speed*.035f);
  UpdateLap(now);
 }
 void UpdateLap(float now){
  if(drivingSurface.Training)return;
  int count=track.Count,start=TrackLandmarks.StartFinishPoint;
  int before=(lastIndex-start+count)%count,after=(nearest-start+count)%count,advance=(nearest-lastIndex+count)%count;
  if(advance>0&&advance<count/8){
   int nextCheckpoint=(checkpoints+1)*(count/4);
   if(checkpoints<3&&before<nextCheckpoint&&after>=nextCheckpoint)checkpoints++;
   if(before>after&&checkpoints==3){float lapTime=now-lapStart;if(best==0||lapTime<best)best=lapTime;lap++;lapStart=now;checkpoints=0;}
  }
  lastIndex=nearest;
 }
 void OnGUI(){if(referenceLap){referenceLap.Draw();return;}if(label==null){label=new GUIStyle(GUI.skin.label){fontSize=20};label.normal.textColor=Color.white;big=new GUIStyle(label){fontSize=54,fontStyle=FontStyle.Bold};small=new GUIStyle(label){fontSize=13};}float sx=Screen.width/1600f,sy=Screen.height/900f;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(sx,sy,1));GUI.color=new Color(.035f,.055f,.07f,.9f);GUI.DrawTexture(new Rect(25,25,450,88),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(25,735,370,152),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(45,35,420,32),drivingSurface.Training?"TRAINING AREA / SLIDE PRACTICE":"GOTLAND RING / OPEN PRACTICE",label);GUI.Label(new Rect(45,73,430,28),$"{carModel.displayName.ToUpperInvariant()}  /  {carModel.PaintName.ToUpperInvariant()}",small);
  GUI.Label(new Rect(45,742,180,75),(velocity.magnitude*3.6f).ToString("000"),big);GUI.Label(new Rect(180,787,90,25),"km/h",label);GUI.Label(new Rect(285,746,100,65),(reversing?"R":gear.ToString()),big);GUI.Label(new Rect(45,825,320,25),$"{rpm:0} RPM     BOOST {boost*.9f:0.00} bar",small);
  GUI.color=new Color(.15f,.2f,.24f);GUI.DrawTexture(new Rect(45,815,315,5),Texture2D.whiteTexture);GUI.color=new Color(.96f,.3f,.2f);GUI.DrawTexture(new Rect(45,815,315*Mathf.Clamp01(rpm/7000),5),Texture2D.whiteTexture);GUI.color=Color.white;
  GUI.Label(new Rect(45,855,340,24),dynamics.awdMode?"AWD PHYSICS / F4 TO COMPARE":"ARCADE HANDLING / F4 FOR AWD",small);
  if(dynamics.awdMode){float angle=Mathf.Abs(awd.SideslipDegrees);GUI.color=angle>3?new Color(1,.72f,.3f):Color.white;GUI.Label(new Rect(440,818,260,25),$"SIDE SLIP  {angle:0.0}°",small);GUI.color=Color.white;}
  DrawMap();GUI.Label(new Rect(1340,268,255,30),drivingSurface.Training?"800 × 800 m / ASPHALT":$"LAP {lap}   {Format((paused?pauseStarted:Time.time)-lapStart)}",label);GUI.Label(new Rect(1340,300,255,30),drivingSurface.Training?"R or Home: return to start":best>0?"BEST "+Format(best):$"{length/1000:0.000} km / CSV layout",small);
  GUI.enabled=!settingsOpen;if(GUI.Button(new Rect(1340,382,235,32),drivingSurface.Training?"Gotland Ring [Z]":"Training area [Z]"))ToggleTrainingArea();GUI.enabled=true;
  GUI.color=new Color(.035f,.055f,.07f,.9f);GUI.DrawTexture(new Rect(1340,335,235,36),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(1352,341,215,26),fpsText,label);
  GUI.color=new Color(.035f,.055f,.07f,.9f);GUI.DrawTexture(new Rect(25,163,370,autopilotEnabled?194:40),Texture2D.whiteTexture);GUI.color=autopilotEnabled?new Color(.35f,1,.65f):Color.white;
  GUI.Label(new Rect(45,169,340,30),$"AUTO(P)ILOT  {(autopilotEnabled?(paused?"PAUSED":"ON"):"OFF")}  /  {PilotShortcut}",label);GUI.color=Color.white;
  if(autopilotEnabled){GUI.Label(new Rect(45,204,340,24),$"TARGET {pilotControls.TargetSpeed*3.6f:0} km/h    GAS {pilotControls.Throttle*100:0}%    BRAKE {pilotControls.Brake*100:0}%",small);GUI.Label(new Rect(45,230,340,24),$"{PilotShortcut} to return to manual driving",small);DrawPilotKeyboard();}
  DrawSteeringInput();
  GUI.Label(new Rect(440,850,880,30),mouseSteering?"MOUSE Steer   W/S Pedals   U Keyboard   C Camera   Y Car   R Recover   O Replay   X Reverse   F3 Dynamics":"WASD Drive   MOUSE Look   U Mouse steering   C Camera   Y Car   R Recover   O Replay   X Reverse   F3 Dynamics",small);GUI.Label(new Rect(1250,830,350,25),carModel.credit,small);GUI.Label(new Rect(1250,855,350,25),$"Mannetroll Solutions AB / {Application.version}",small);
  if(GUI.Button(new Rect(45,120,170,32),"Dynamics [F3]"))OpenSettings();
  if(GUI.Button(new Rect(225,120,170,32),$"{(Screen.fullScreen?"Windowed":"Fullscreen")} [{ShortcutModifier}+F]"))ToggleFullscreen();
  if(paused && !settingsOpen){GUI.color=new Color(0,0,0,.75f);GUI.DrawTexture(new Rect(450,270,700,300),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(510,310,600,65),"PRACTICE PAUSED",big);GUI.Label(new Rect(510,395,600,100),$"Escape to resume  |  U: steering mode\nRight mouse: centre {(mouseSteering?"steering":"view")}  |  Home: reset  |  Z: training area\nY: car  |  T: colour  |  M: mute  |  O: reference lap  |  F2: screenshot",label);if(GUI.Button(new Rect(510,510,180,40),"Quit"))Application.Quit();if(GUI.Button(new Rect(710,510,220,40),"Driving dynamics"))OpenSettings();}
  if(settingsOpen)DrawSettings();
 }
 void SetPaused(bool value){if(value==paused)return;if(value)pauseStarted=Time.time;else lapStart+=Time.time-pauseStarted;paused=value;ResetMouseSteering();awd.SetSimulationActive(dynamics.awdMode&&!paused);Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;}
 void OpenSettings(){if(settingsOpen)return;wasPaused=paused;draft=dynamics.Copy();settingsOpen=true;SetPaused(true);}
 void CloseSettings(bool apply){if(apply){ApplyDrivingSettings(draft);dynamics.Save();}settingsOpen=false;SetPaused(wasPaused);}
 string PilotShortcut=>ShortcutModifier+"+P";
 void SetAutopilot(bool enabled){autopilotEnabled=enabled;ResetMouseSteering();if(enabled)ConfigureAutopilot();else pilotControls=default;Debug.Log("AUTOPILOT "+(enabled?"ON":"OFF"));}
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
  settingsPage=GUI.Toolbar(new Rect(470,211,650,30),settingsPage,new[]{"Driving","View & sound"});
  if(settingsPage==0){
  draft.awdMode=GUI.SelectionGrid(new Rect(470,244,650,32),draft.awdMode?0:1,new[]{"AWD physics","Arcade comparison"},2)==0;
  if(draft.awdMode){
   draft.awdGrip=Setting("Tyre grip multiplier",draft.awdGrip,.7f,1.3f,288,"0.00");
   draft.awdFrontTorque=Setting("Front torque share",draft.awdFrontTorque,.1f,.9f,330,"0.00");
   draft.steering=Setting("Low-speed steering (degrees)",draft.steering,25,55,372);
   draft.highSpeedSteering=Setting("High-speed steering (degrees)",draft.highSpeedSteering,8,25,414);
   draft.awdSteeringRate=Setting("Steering speed (degrees/s)",draft.awdSteeringRate,40,150,456,"0");
   GUI.Label(new Rect(470,488,650,24),"Steering angles are shared with Arcade comparison.",small);
   draft.awdTractionControl=GUI.Toggle(new Rect(470,520,600,30),draft.awdTractionControl," Traction control assistance");
   GUI.Label(new Rect(470,558,650,48),"Stock GT baseline: 160 kW / 290 Nm / five-speed AWD.\nABS is enabled. Tyres, suspension and clutch are approximate.",small);
  }else{
  draft.grip=Setting("Cornering grip (m/s²)",draft.grip,10,40,288);
  draft.lateralGrip=Setting("Side-slip recovery (m/s²)",draft.lateralGrip,10,60,330);
  draft.steering=Setting("Low-speed steering (degrees)",draft.steering,25,55,372);
  draft.highSpeedSteering=Setting("High-speed steering (degrees)",draft.highSpeedSteering,8,25,414);
  draft.response=Setting("Steering response",draft.response,1,9,456);
  draft.acceleration=Setting("Acceleration multiplier",draft.acceleration,.5f,1.8f,498,"0.00");
  draft.braking=Setting("Braking (m/s²)",draft.braking,6,20,540);
  draft.offRoadGrip=Setting("Off-road grip (m/s²)",draft.offRoadGrip,3,12,582);
  }
  }else{
   draft.cockpitMovement=Setting("Cockpit movement",draft.cockpitMovement,0,1,288,"0%");
   GUI.Label(new Rect(470,324,650,48),"Gentle head movement under braking and cornering.\nBonnet view gets a smaller amount of road vibration.",small);
   draft.surfaceSound=Setting("Surface sound",draft.surfaceSound,0,1,408,"0%");
   GUI.Label(new Rect(470,444,650,48),"Asphalt hum, kerb rumble and loose-ground sound.\nEach grounded wheel contributes to the mix.",small);
   GUI.Label(new Rect(470,540,650,48),"Set either slider to 0 to switch that effect off.\nThese effects do not change grip, steering or lap performance.",small);
  }
  if(settingsPage==0)GUI.Label(new Rect(470,650,650,25),"Changing handling mode restarts the lap and clears its best time.",small);
  if(GUI.Button(new Rect(470,685,180,40),"Restore defaults"))draft=new DrivingSettings();
  if(GUI.Button(new Rect(735,685,180,40),"Cancel"))CloseSettings(false);
  if(GUI.Button(new Rect(935,685,190,40),"Apply & close"))CloseSettings(true);
 }
 IEnumerator SettingsTest(){
  yield return new WaitForSeconds(2);var original=dynamics.Copy();OpenSettings();draft.grip=39;draft.steering=35;draft.highSpeedSteering=12;draft.cockpitMovement=.13f;draft.surfaceSound=.27f;CloseSettings(false);
  Debug.Assert(dynamics.grip==original.grip && dynamics.steering==original.steering && dynamics.highSpeedSteering==original.highSpeedSteering && dynamics.cockpitMovement==original.cockpitMovement && dynamics.surfaceSound==original.surfaceSound && !paused,"Cancel must preserve dynamics and sound settings and resume");
  OpenSettings();draft.grip=31;draft.steering=48;draft.highSpeedSteering=22;draft.cockpitMovement=.73f;draft.surfaceSound=.81f;CloseSettings(true);var saved=DrivingSettings.Load();Debug.Assert(saved.grip==31 && saved.steering==48 && saved.highSpeedSteering==22 && saved.cockpitMovement==.73f && saved.surfaceSound==.81f,"Apply must persist dynamics, speed steering and view and surface sound");
  OpenSettings();draft.cockpitMovement=draft.surfaceSound=0;CloseSettings(true);saved=DrivingSettings.Load();Debug.Assert(saved.cockpitMovement==0&&saved.surfaceSound==0,"Apply must persist disabled view and surface effects");
  dynamics=original;dynamics.Save();SetPaused(true);OpenSettings();CloseSettings(false);Debug.Assert(paused,"Dialog must preserve existing pause");
  SetPaused(false);OpenSettings();yield return new WaitForSeconds(2);CaptureScreenshot("dynamics-dialog.png");yield return new WaitForSeconds(1);settingsPage=1;yield return new WaitForSeconds(1);CaptureScreenshot("view-sound-dialog.png");
  Debug.Log("SETTINGS_TEST passed: cancel, apply, dynamics, view and sound persistence, pause restoration");yield return new WaitForSeconds(2);Application.Quit();
 }
 string Format(float t)=>$"{(int)t/60:00}:{t%60:00.00}";
 IEnumerator RenderStats(){yield return new WaitForSeconds(2);int first=Time.frameCount;float start=Time.realtimeSinceStartup;yield return new WaitForSeconds(5);Debug.Log($"RENDER_STATS fps={(Time.frameCount-first)/(Time.realtimeSinceStartup-start):F1} resolution={Screen.width}x{Screen.height} gpu={SystemInfo.graphicsDeviceName}");}
 void CaptureScreenshot(string filename){var directory=Application.platform==RuntimePlatform.OSXPlayer?Application.persistentDataPath:System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,".."));var path=System.IO.Path.Combine(directory,filename);ScreenCapture.CaptureScreenshot(path);Debug.Log("SCREENSHOT "+path);}
 IEnumerator SignTest(){
  view=0;
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
   RecoverCar((TrackLandmarks.All[n].Point+track.Count-8)%track.Count);var direction=trackSignBoards[n].position-car.TransformPoint(carModel.cockpitView.localPosition);
   lookYaw=Vector3.SignedAngle(car.forward,new Vector3(direction.x,0,direction.z),Vector3.up);lookPitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg-carModel.cockpitView.localEulerAngles.x;
   yield return new WaitForSeconds(1);CaptureScreenshot($"track-sign-{n+1:00}.png");yield return new WaitForSeconds(.5f);
  }
  Debug.Log($"TRACK_SIGNS_TEST passed: {trackSignBoards.Count} boards, right side, approach facing, text fits, minimum clearance={clearance:F2} m");Application.Quit();
 }
 IEnumerator SmokeTest(){yield return new WaitForSeconds(30);Debug.Log($"SMOKE_TEST speed={velocity.magnitude:F1} distanceFromStart={Vector3.Distance(car.position,track[TrackLandmarks.StartFinishPoint]):F1} rpm={rpm:F0} track={track.Count} length={length}");CaptureScreenshot("smoke-test.png");smokeBrake=true;yield return new WaitForSeconds(4);Debug.Log("BRAKE_TEST speed="+velocity.magnitude.ToString("F2")+" pass="+(velocity.magnitude<1));Application.Quit();}
}
