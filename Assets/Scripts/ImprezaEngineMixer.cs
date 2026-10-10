using System;

// The audio thread owns all playback/filter state. Main-thread controls are
// copied once per buffer; no Unity calls, allocations or locks occur here.
public sealed class ImprezaEngineMixer
{
 public volatile int SampleRate;
 readonly float[][] loops;
 readonly double[] positions=new double[3];
 readonly float[] overlapCorrelation;
 static readonly float[] referenceRpm={2530,3060,4500};
 float rpm=900,load,speed,squeal,volume,previousLoad,lift;
 float engineLow,engineBody,airLow,airBody,dcInput,dcOutput;
 float tyreFast,tyreSlow,tyreWander;
 double tyrePhase,tyreOvertone;
 uint random=1234567;

 public ImprezaEngineMixer(float[][] recordings,int outputRate){
  loops=recordings;SampleRate=outputRate;
  // Measure shared, phase-aligned engine content once, off the audio thread.
  // Crossfade gain then stays even without compressing the exhaust waveform.
  var profiles=new double[3][];var energy=new double[3];
  for(int n=0;n<3;n++){
   var samples=loops[n];
   profiles[n]=new double[1024];
   foreach(float sample in samples)energy[n]+=sample*sample;
   energy[n]/=samples.Length;
   for(int phase=0;phase<1024;phase++){
    double position=phase/1024.0*samples.Length;int index=(int)position;
    int next=index+1==samples.Length?0:index+1;
    profiles[n][phase]=samples[index]+(samples[next]-samples[index])*(position-index);
   }
  }
  overlapCorrelation=new float[2];
  for(int n=0;n<2;n++){
   double shared=0;for(int phase=0;phase<1024;phase++)shared+=profiles[n][phase]*profiles[n+1][phase];
   overlapCorrelation[n]=(float)(shared/1024/Math.Sqrt(energy[n]*energy[n+1]));
  }
 }

 static float Clamp(float value,float min,float max)=>Math.Max(min,Math.Min(max,value));
 static float Follow(float value,float target,float coefficient)=>value+(target-value)*coefficient;

 public void Render(float[] data,int channels,float targetRpm,float targetLoad,float targetSpeed,float targetSqueal,bool muted){
  int rate=SampleRate;
  float rpmSmoothing=1-(float)Math.Exp(-1.0/(rate*.045));
  float loadSmoothing=1-(float)Math.Exp(-1.0/(rate*.065));
  float airSmoothing=1-(float)Math.Exp(-2*Math.PI*650/rate);
  float dcPole=(float)Math.Exp(-2*Math.PI*18/rate);
  float liftDecay=(float)Math.Exp(-1.0/(rate*.10));
  float squealAttack=1-(float)Math.Exp(-1.0/(rate*.035));
  float squealRelease=1-(float)Math.Exp(-1.0/(rate*.16));
  float tyreFastFilter=1-(float)Math.Exp(-2*Math.PI*3200/rate);
  float tyreSlowFilter=1-(float)Math.Exp(-2*Math.PI*900/rate);
  float tyreWanderFilter=1-(float)Math.Exp(-2*Math.PI*12/rate);
  targetRpm=Clamp(targetRpm,700,8000);targetLoad=Clamp(targetLoad,0,1);
  targetSpeed=Math.Max(0,targetSpeed);targetSqueal=Clamp(targetSqueal,0,1);
  if(previousLoad>.65f&&targetLoad<.15f)lift=.035f*Clamp((rpm-2500)/2500,0,1);
  previousLoad=targetLoad;
  // Throttle opens the intake note; coasting retains the exhaust rumble.
  float tone=1-(float)Math.Exp(-2*Math.PI*(850+targetLoad*750)/rate);
  for(int i=0;i<data.Length;i+=channels){
   rpm=Follow(rpm,targetRpm,rpmSmoothing);
   load=Follow(load,targetLoad,loadSmoothing);
   speed=Follow(speed,targetSpeed,loadSmoothing);
   squeal=Follow(squeal,targetSqueal,targetSqueal>squeal?squealAttack:squealRelease);
   volume=muted?Math.Max(0,volume-1f/(rate*.012f)):Math.Min(1,volume+1f/(rate*.025f));

   int lower=rpm<referenceRpm[1]?0:1;
   float blend=Clamp((rpm-referenceRpm[lower])/(referenceRpm[lower+1]-referenceRpm[lower]),0,1);
   // Account for the correlated exhaust pulses when crossfading. All loops
   // keep running, so changing range never restarts a recording.
   float lowWeight=(float)Math.Sqrt(1-blend),highWeight=(float)Math.Sqrt(blend);
   float blendGain=1/(float)Math.Sqrt(1+2*lowWeight*highWeight*overlapCorrelation[lower]);
   float engine=0;
   for(int n=0;n<3;n++){
    var samples=loops[n];double position=positions[n];int index=(int)position;
    int next=index+1==samples.Length?0:index+1;
    float sample=samples[index]+(samples[next]-samples[index])*(float)(position-index);
    if(n==lower)engine+=sample*lowWeight;
    else if(n==lower+1)engine+=sample*highWeight;
    // Each recording contains one isolated engine cycle. Exact cycle playback
    // keeps the recordings phase-locked over a whole driving session.
    position+=(double)samples.Length*rpm/(120.0*rate);
    if(position>=samples.Length)position-=samples.Length;
    positions[n]=position;
   }
   engineLow=Follow(engineLow,engine*blendGain,tone);
   engineBody=Follow(engineBody,engineLow,tone);

   random^=random<<13;random^=random>>17;random^=random<<5;
   float white=random/(float)uint.MaxValue*2-1;
   airLow=Follow(airLow,white,airSmoothing);airBody=Follow(airBody,airLow,airSmoothing);
   lift*=liftDecay;
   float rolling=Clamp(speed/65,0,1);
   float air=airBody*(rolling*.045f+lift);
   // A rough, wavering squeal above the exhaust, driven by loaded tyre slip.
   tyreFast=Follow(tyreFast,white,tyreFastFilter);tyreSlow=Follow(tyreSlow,white,tyreSlowFilter);
   tyreWander=Follow(tyreWander,white,tyreWanderFilter);
   float tyrePitch=1330+180*squeal+70*rolling+tyreWander*700;
   tyrePhase+=2*Math.PI*tyrePitch/rate;tyreOvertone+=2*Math.PI*(tyrePitch*1.43+17)/rate;
   if(tyrePhase>=2*Math.PI)tyrePhase-=2*Math.PI;
   if(tyreOvertone>=2*Math.PI)tyreOvertone-=2*Math.PI;
   float tyreTone=(float)(Math.Sin(tyrePhase)+.3*Math.Sin(tyreOvertone))*.20f;
   float tyres=(tyreTone+(tyreFast-tyreSlow)*.65f)*squeal*.30f*Clamp((speed-3)/12,0,1);
   float gain=(.52f+.30f*load)*(.8f+.2f*Clamp((rpm-900)/4000,0,1));
   float value=engineBody*gain+air+tyres;
   // Remove subsonic energy from pitching down to idle, with ample headroom.
   float filtered=value-dcInput+dcPole*dcOutput;dcInput=value;dcOutput=filtered;
   value=(float)Math.Tanh(filtered)*volume;
   for(int channel=0;channel<channels;channel++)data[i+channel]=value;
  }
 }
}
