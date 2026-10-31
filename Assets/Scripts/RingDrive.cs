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
 float length, smokeTime; GUIStyle label, big, small, keyLabel, circuitHeading, circuitStats, circuitButton;
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
  awdTest=Array.Exists(args,x=>x=="--awd-test"||x=="--drivetrain-test"||x=="--turnaround-test");
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
  if(Array.Exists(args,x=>x=="--scenery-views-test")){modelPreview=true;muted=true;StartCoroutine(SceneryViewsTest());}
  signTest=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--sign-test");if(signTest){muted=true;StartCoroutine(SignTest());}
  surfaceTest=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--surface-test");if(surfaceTest)StartCoroutine(SurfaceTest());
  if(Array.Exists(args,x=>x=="--ui-theme-test"))StartCoroutine(UiThemePreview());
  if(Array.Exists(args,x=>x=="--keyboard-help-test"))StartCoroutine(KeyboardHelpCheck());
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
 void OnDestroy(){if(map)Destroy(map);if(bankLegend)Destroy(bankLegend);if(trainingMap)Destroy(trainingMap);Font.textureRebuilt-=RefreshTrackSignFont;}
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
  if(autopilotEnabled)autopilot.ResetControls();
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
  if(keyboardHelpOpen){
   if(Input.GetKeyDown(KeyCode.Escape)){
    if(keyboardCapture!=KeyCode.None){keyboardCapture=KeyCode.None;keyboardMessage="";}
    else CloseKeyboardHelp(false);
   }
   motor.Muted=true;return;
  }
  if(DrivingHudActive&&DriveKeyDown(KeyCode.J))hideDrivingHud=!hideDrivingHud;
  if(DrivingHudActive&&DriveKeyDown(KeyCode.K))StartCoroutine(SaveDesktopScreenshot());
  if(Input.GetKeyDown(KeyCode.Escape))ToggleDrivingMenu();
  if(!referenceLap&&!SceneryViewing&&DriveKeyDown(KeyCode.F3)){if(settingsOpen)CloseSettings(false);else OpenSettings();}
  if(DriveKeyDown(KeyCode.F))ToggleFullscreen();
  if(paused&&!settingsOpen&&!referenceLap&&!SceneryViewing){motor.Muted=true;return;}
  if(!settingsOpen&&!referenceLap&&DriveKeyDown(KeyCode.V))NextSceneryView();
  if(SceneryViewing){UpdateSceneryViews();return;}
  if(!settingsOpen && DriveKeyDown(KeyCode.O))ToggleReferenceLap();
  if(referenceLap){UpdateReferenceLap();return;}
  if(!paused)AnimateWindTurbines(Time.deltaTime);
  fpsElapsed+=Time.unscaledDeltaTime;fpsFrames++;
  if(fpsElapsed>=.5f){fpsText=$"{fpsFrames/fpsElapsed:0} FPS  /  {fpsElapsed*1000/fpsFrames:0.0} ms";fpsElapsed=0;fpsFrames=0;}

  if(!settingsOpen&&DriveKeyDown(KeyCode.H))ToggleHandlingMode();
  if(!settingsOpen && DriveKeyDown(KeyCode.P))SetAutopilot(!autopilotEnabled);
  if(!settingsOpen && DriveKeyDown(KeyCode.U))ToggleMouseSteering();
  if(!settingsOpen && DriveKeyDown(KeyCode.Z))ToggleTrainingArea();
  if(!settingsOpen && DriveKeyDown(KeyCode.Y))SwitchCarModel();
  if(!settingsOpen && DriveKeyDown(KeyCode.T) && carModel.CanChangePaint)carModel.CyclePaint();
  if(!settingsOpen && DriveKeyDown(KeyCode.M))muted=!muted;if(!settingsOpen && DriveKeyDown(KeyCode.C)){CycleCamera();}if(!settingsOpen && DriveKeyDown(KeyCode.R))RecoverCar(nearest);if(!settingsOpen && DriveKeyDown(KeyCode.Home))RestartLap();if(DriveKeyDown(KeyCode.F2))CaptureScreenshot("GotlandRing-screenshot.png");
  UpdatePointer(new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y")),Input.GetMouseButtonDown(1));
  if(!modelPreview){
   UpdateCameraPose();
   AnimateCar(paused?0:Time.deltaTime);
  }
  cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,76+velocity.magnitude*.12f,Time.deltaTime*3);
  motor.Rpm=rpm;motor.Load=throttle;motor.Speed=velocity.magnitude;motor.Muted=muted||paused;
 }
 float DronePitch=>Mathf.Atan2(dynamics.droneHeight,dynamics.droneDistance)*Mathf.Rad2Deg*.5f;
 const int CameraViewCount=4;
 void CycleCamera(){
  view=(view+1)%CameraViewCount;lookYaw=lookPitch=0;ResetMouseSteering();
 }
 void UpdateCameraPose(){
  if(SceneryViewing)return;
  if(view<2){var anchor=view==0?carModel.cockpitView:carModel.bonnetView;head.localPosition=anchor.localPosition;head.localRotation=Quaternion.Euler(anchor.localEulerAngles.x+lookPitch,lookYaw,0);ApplyCameraFeedback();}
  else if(view==3){
   float heading=car.eulerAngles.y+lookYaw;
   var bearing=Quaternion.Euler(0,heading,0);
   head.position=car.position+Vector3.up*dynamics.droneHeight-bearing*Vector3.forward*dynamics.droneDistance;
   head.rotation=Quaternion.Euler(DronePitch+lookPitch,heading,0);
  }
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
  float keyboard=((DriveKey(KeyCode.RightArrow)||DriveKey(KeyCode.D))?1:0)-((DriveKey(KeyCode.LeftArrow)||DriveKey(KeyCode.A))?1:0);
  StepDriving(dt,now,keyboard,(DriveKey(KeyCode.UpArrow)||DriveKey(KeyCode.W))?1:0,
   (DriveKey(KeyCode.DownArrow)||DriveKey(KeyCode.S))?1:0,DriveKey(KeyCode.X));
 }
 void StepDriving(float dt,float now,float keyboardSteering,float accelerator,float brake,bool reverseRequested){if(paused||modelPreview||signTest||surfaceTest)return;if(dynamics.awdMode)SyncAwdMotion();float speed=velocity.magnitude;var surface=drivingSurface.Sample(car.position.x,car.position.z);nearest=surface.Segment;bool road=surface.OnRoad;float input=mouseSteering?mouseSteer:keyboardSteering;throttle=accelerator;
  if(autopilotEnabled){pilotControls=autopilot.Drive(car.position,yaw,velocity,surface,dt);if(dynamics.awdMode)pilotControls=awd.CorrectPilot(pilotControls,dynamics,dt);input=pilotControls.Steering;throttle=pilotControls.Throttle;brake=pilotControls.Brake;}
  if(automatic){var aim=track[(nearest+20)%track.Count]-car.position;float angle=Vector3.SignedAngle(car.forward,aim,Vector3.up);input=Mathf.Clamp(angle/18,-1,1);throttle=speed<24?1:0;brake=speed>27?1:0;if(smokeBrake){throttle=0;brake=1;}}
  reverseRequested=reverseRequested&&!automatic&&!autopilotEnabled&&throttle<=0;
  if(dynamics.awdMode){StepAwdDriving(dt,input,brake,reverseRequested);UpdateLap(now);return;}
  StepArcadeDriving(dt,input,brake,reverseRequested);
  UpdateLap(now);
 }
 void StepArcadeDriving(float dt,float input,float brake,bool reverseRequested){
  float speed=velocity.magnitude;var surface=drivingSurface.Sample(car.position.x,car.position.z);bool road=surface.OnRoad;
  steer=Mathf.MoveTowards(steer,input,dt*dynamics.response);Vector3 f=new Vector3(Mathf.Sin(yaw*Mathf.Deg2Rad),0,Mathf.Cos(yaw*Mathf.Deg2Rad));Vector3 right=Vector3.Cross(Vector3.up,f);float longitudinal=Vector3.Dot(velocity,f),lateral=Vector3.Dot(velocity,right);
  float wheelRpm=Mathf.Abs(longitudinal)/(.32f*2*Mathf.PI)*60;float target=Mathf.Max(900,wheelRpm*ratios[gear-1]*4.11f);if(target>6400&&gear<5){gear++;target*=.72f;}else if(target<2200&&gear>1){gear--;target*=1.3f;}rpm=Mathf.Lerp(rpm,target+throttle*350,dt*8);boost=Mathf.MoveTowards(boost,throttle*Mathf.InverseLerp(2100,4000,rpm),dt*.7f);
  if(reverseRequested){throttle=1;if(longitudinal>.3f){brake=1;throttle=0;}else reversing=true;}
  else if(throttle>0 || longitudinal>=-.3f)reversing=false;
  float drive=throttle*dynamics.acceleration*(reversing?-3f:2.6f+boost*3.3f)*Mathf.Clamp01(((reversing?8:72)-speed)/(reversing?2:15));float drag=DrivingSettings.ArcadeDrag(speed,road);longitudinal=Mathf.MoveTowards(longitudinal,0,(drag+brake*dynamics.braking)*dt);longitudinal+=drive*dt;
  float gripScale=throttle==0?2:1;
  float steeringAngle=steer*dynamics.SteeringLimit(speed)*Mathf.Deg2Rad;float yawRate=longitudinal/2.52f*Mathf.Tan(steeringAngle);float grip=(road?dynamics.grip:dynamics.offRoadGrip)*gripScale,bank=TrackData.BankAcceleration(surface.Normal,right);float limitSpeed=Mathf.Max(speed,3),travelBank=longitudinal<0?-bank:bank;yawRate=Mathf.Clamp(yawRate,(travelBank-grip)/limitSpeed,(travelBank+grip)/limitSpeed);yaw+=yawRate*Mathf.Rad2Deg*dt;
  lateral=Mathf.MoveTowards(lateral,0,(road?dynamics.lateralGrip:dynamics.offRoadGrip)*gripScale*dt);f=new Vector3(Mathf.Sin(yaw*Mathf.Deg2Rad),0,Mathf.Cos(yaw*Mathf.Deg2Rad));right=Vector3.Cross(Vector3.up,f);velocity=f*longitudinal+right*lateral;PlaceCarOnSurface(car.position+velocity*dt,-steer*speed*.035f);
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
 void DrawGameGUI(){if(paused&&!settingsOpen&&!referenceLap&&!SceneryViewing){DrawDrivingMenu();return;}if(referenceLap){referenceLap.Draw();return;}if(label==null){label=new GUIStyle(GUI.skin.label){fontSize=20};label.normal.textColor=Color.white;big=new GUIStyle(label){fontSize=54,fontStyle=FontStyle.Bold};small=new GUIStyle(label){fontSize=13};}float sx=Screen.width/1600f,sy=Screen.height/900f;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(sx,sy,1));if(SceneryViewing){DrawSceneryViews();return;}DrawCircuitControls();GameUi.Panel(new Rect(25,735,370,152));GUI.color=Color.white;
  GUI.Label(new Rect(45,742,180,75),(velocity.magnitude*3.6f).ToString("000"),big);GUI.Label(new Rect(180,787,90,25),"km/h",label);GUI.Label(new Rect(285,746,100,65),(reversing?"R":gear.ToString()),big);GUI.Label(new Rect(45,825,320,25),$"{rpm:0} RPM     BOOST {boost*.9f:0.00} bar",small);
  GUI.color=new Color(.15f,.2f,.24f);GUI.DrawTexture(new Rect(45,815,315,5),Texture2D.whiteTexture);GUI.color=new Color(.96f,.3f,.2f);GUI.DrawTexture(new Rect(45,815,315*Mathf.Clamp01(rpm/7000),5),Texture2D.whiteTexture);GUI.color=Color.white;
  GUI.Label(new Rect(45,855,340,24),dynamics.awdMode?$"AWD PHYSICS / {KeyHint(KeyCode.H)} FOR ARCADE":$"ARCADE HANDLING / {KeyHint(KeyCode.H)} FOR AWD",small);
  DrawMap();
  if(settingsOpen)DrawSettings();
 }
 void DrawCircuitStats(){
  if(circuitStats==null)circuitStats=new GUIStyle(small){richText=true,alignment=TextAnchor.MiddleLeft};
  string text=fpsText;
  if(dynamics.awdMode){
   float angle=Mathf.Abs(awd.SideslipDegrees);
   string slip=$"Side slip {angle:0.0}°";
   text+=" · "+(angle>3?$"<color=#FFB84D>{slip}</color>":slip);
  }
  GUI.Label(new Rect(18,100,390,24),text,circuitStats);
 }
 void DrawCircuitControls(){
  if(circuitHeading==null)circuitHeading=new GUIStyle(label){fontStyle=FontStyle.Bold};
  if(circuitButton==null)circuitButton=new GUIStyle(GUI.skin.button){fontSize=11,padding=new RectOffset(3,3,2,2)};
  float height=202+(mouseSteering?24:0)+(autopilotEnabled?122:0);
  GUI.BeginGroup(new Rect(1149,887-height,426,height));
  GameUi.Panel(new Rect(0,0,426,height));
  GUI.color=Color.white;
  GUI.Label(new Rect(18,14,390,28),drivingSurface.Training?"Training area · Slide practice":"Gotland Ring · Open practice",circuitHeading);
  GUI.color=GameUi.Muted;
  GUI.Label(new Rect(18,44,390,22),$"{carModel.displayName} · {carModel.PaintName}",small);
  GUI.color=Color.white;
  GUI.Label(new Rect(18,70,390,28),drivingSurface.Training?"800 × 800 m · Asphalt":$"Lap {lap} · {Format((paused?pauseStarted:Time.time)-lapStart)}",label);
  GUI.color=GameUi.Muted;
  if(!drivingSurface.Training)GUI.Label(new Rect(238,74,170,24),best>0?"Best "+Format(best):$"{length/1000:0.000} km",small);
  DrawCircuitStats();
  GUI.color=Color.white;GUI.enabled=!settingsOpen;
  if(GUI.Button(new Rect(18,132,74,26),$"Dynamics [{KeyHint(KeyCode.F3)}]",circuitButton))OpenSettings();
  if(GUI.Button(new Rect(96,132,75,26),$"{(Screen.fullScreen?"Windowed":"Fullscreen")} [{KeyHint(KeyCode.F)}]",circuitButton))ToggleFullscreen();
  GUI.enabled=!settingsOpen;
  bool pilot=GUI.Toggle(new Rect(175,132,84,26),autopilotEnabled,$"Autopilot {(autopilotEnabled?"on":"off")} [{KeyHint(KeyCode.P)}]",circuitButton);
  if(pilot!=autopilotEnabled)SetAutopilot(pilot);
  if(GUI.Button(new Rect(263,132,73,26),$"{(mouseSteering?"Mouse":"Keyboard")} [{KeyHint(KeyCode.U)}]",circuitButton))ToggleMouseSteering();
  if(GUI.Button(new Rect(340,132,68,26),$"{(drivingSurface.Training?"Ring":"Training")} [{KeyHint(KeyCode.Z)}]",circuitButton))ToggleTrainingArea();
  GUI.enabled=true;
  DrawMouseSteeringMeter(166);
  if(autopilotEnabled){
   float y=166+(mouseSteering?24:0);
   GUI.color=GameUi.Muted;
   GUI.Label(new Rect(18,y,390,24),$"Target {pilotControls.TargetSpeed*3.6f:0} km/h · Gas {pilotControls.Throttle*100:0}% · Brake {pilotControls.Brake*100:0}%",small);
   GUI.color=Color.white;DrawPilotKeyboard(18,y+26);
  }
  GUI.color=GameUi.Muted;
  GUI.Label(new Rect(18,height-30,390,22),"Copyright © Mannetroll Solutions AB",small);
  GUI.color=Color.white;
  GUI.EndGroup();
 }
 void SetPaused(bool value){if(value==paused)return;if(value)pauseStarted=Time.time;else lapStart+=Time.time-pauseStarted;paused=value;ResetMouseSteering();awd.SetSimulationActive(dynamics.awdMode&&!paused);Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;}
 void OpenSettings(){if(settingsOpen)return;wasPaused=paused;draft=dynamics.Copy();settingsOpen=true;SetPaused(true);}
 void CloseSettings(bool apply){if(apply){ApplyDrivingSettings(draft);dynamics.Save();}settingsOpen=false;SetPaused(wasPaused);}
 string PilotShortcut=>KeyHint(KeyCode.P);
 void SetAutopilot(bool enabled){autopilotEnabled=enabled;ResetMouseSteering();if(enabled)ConfigureAutopilot();else pilotControls=default;Debug.Log("AUTOPILOT "+(enabled?"ON":"OFF"));}
 void DrawPilotKeyboard(float x,float y){
  float active=paused?0:1;
  DrawPilotKey(KeyHint(KeyCode.W),x+46,y,pilotControls.Throttle*active,new Color(.1f,.65f,.35f));
  DrawPilotKey(KeyHint(KeyCode.A),x,y+42,Mathf.Max(0,-steer)*active,new Color(.1f,.55f,.85f));
  DrawPilotKey(KeyHint(KeyCode.S),x+46,y+42,pilotControls.Brake*active,new Color(.95f,.4f,.12f));
  DrawPilotKey(KeyHint(KeyCode.D),x+92,y+42,Mathf.Max(0,steer)*active,new Color(.1f,.55f,.85f));
  GUI.Label(new Rect(x+153,y+3,230,24),paused?"Inputs paused":"Live inputs",small);
  GUI.Label(new Rect(x+153,y+28,230,24),$"{KeyHint(KeyCode.W)} gas · {KeyHint(KeyCode.S)} brake",small);
  GUI.Label(new Rect(x+153,y+53,230,24),$"{KeyHint(KeyCode.A)}/{KeyHint(KeyCode.D)} steer  {Mathf.Abs(steer)*active*100:0}% {(Mathf.Abs(steer)<.005f||paused?"":steer<0?"L":"R")}",small);
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
  if(label==null){label=new GUIStyle(GUI.skin.label){fontSize=20};label.normal.textColor=Color.white;small=new GUIStyle(label){fontSize=13};}
  GameUi.Dialog(new Rect(425,145,750,610));GUI.color=Color.white;
  GUI.Label(new Rect(470,170,650,40),"Driving dynamics",GameUi.Heading);
  settingsPage=GameUi.Tabs(new Rect(470,211,650,32),settingsPage,new[]{"Driving","View & sound"});
  if(settingsPage==0){
  draft.awdMode=GameUi.Tabs(new Rect(470,252,650,28),draft.awdMode?0:1,new[]{"AWD physics","Arcade comparison"},15)==0;
  if(draft.awdMode){
   draft.awdGrip=Setting("Tyre grip multiplier",draft.awdGrip,.7f,1.3f,288,"0.00");
   draft.awdFrontTorque=Setting("Front torque share",draft.awdFrontTorque,.1f,.9f,330,"0.00");
   draft.steering=Setting("Low-speed steering (degrees)",draft.steering,25,55,372);
   draft.highSpeedSteering=Setting("High-speed steering (degrees)",draft.highSpeedSteering,8,25,414);
   draft.awdSteeringRate=Setting("Steering speed (degrees/s)",draft.awdSteeringRate,40,150,456,"0");
   GUI.Label(new Rect(470,488,650,24),"Steering angles are shared with Arcade comparison.",small);
   draft.awdTractionControl=GameUi.Switch(new Rect(470,520,650,30),draft.awdTractionControl,"Traction control assistance");
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
   GUI.Label(new Rect(470,564,650,65),$"{KeyHint(KeyCode.M)} mutes all driving sound.\nSet a slider to 0 to switch that effect off.",small);
  }
  if(settingsPage==0)GUI.Label(new Rect(470,650,650,25),"Changing handling mode restarts the lap and clears its best time.",small);
  if(GUI.Button(new Rect(470,685,180,40),"Restore defaults"))draft=new DrivingSettings();
  if(GUI.Button(new Rect(735,685,180,40),"Cancel"))CloseSettings(false);
  if(GameUi.PrimaryButton(new Rect(935,685,190,40),"Apply & close"))CloseSettings(true);
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
