using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EngineAudioChecks
{
 [MenuItem("Gotland Ring/Check and preview engine audio")]
 public static void Run(){
  Directory.CreateDirectory("Build/AudioPreview");
  foreach(int rate in new[]{44100,48000,96000}){
   var mixer=BoxerAudio.CreateMixer(rate);
   var block=new float[512*2];float peak=0,step=0,last=0;double energy=0;
   using(var output=new BinaryWriter(File.Create($"Build/AudioPreview/impreza-drive-{rate}.wav"))){
    const int seconds=24;int frames=(seconds*rate/512)*512;
    Header(output,frames,rate);
    for(int frame=0;frame<frames;frame+=512){
     float t=frame/(float)rate,rpm,load,speed;
     if(t<3){rpm=900;load=0;speed=0;}
     else if(t<10){rpm=Mathf.Lerp(1200,6200,(t-3)/7);load=.9f;speed=(t-3)*5;}
     else if(t<14){rpm=Mathf.Lerp(4300,6300,(t-10)/4);load=.95f;speed=35+(t-10)*5;}
     else if(t<18){rpm=Mathf.Lerp(4500,2500,(t-14)/4);load=0;speed=55-(t-14)*6;}
     else {rpm=3000;load=.7f;speed=30;}
     bool muted=t>=19&&t<21;
     mixer.Render(block,2,rpm,load,speed,t>=14&&t<18?.65f:0,muted);
     for(int i=0;i<block.Length;i+=2){
      float sample=block[i];
      if(float.IsNaN(sample)||float.IsInfinity(sample)||block[i+1]!=sample)throw new Exception("Invalid engine audio or channel mismatch");
      if(t>19.1f&&t<20.9f&&sample!=0)throw new Exception("Mute did not silence the engine mixer");
      peak=Mathf.Max(peak,Mathf.Abs(sample));step=Mathf.Max(step,Mathf.Abs(sample-last));last=sample;
      energy+=sample*sample;output.Write((short)Mathf.RoundToInt(sample*32767));
     }
    }
    double rms=Math.Sqrt(energy/frames);
    if(peak>=.95f||rms<.025||step>.08f)throw new Exception($"Engine audio headroom/continuity failed: peak={peak} rms={rms} step={step}");
    Debug.Log($"ENGINE_AUDIO_TEST rate={rate} peak={peak:F4} rms={rms:F4} maxStep={step:F4} pass=True");
   }
  }
  // Long steady renders expose repeating seams and provide reproducible
  // spectral evidence that the exhaust order follows the requested RPM.
  foreach(int rpm in new[]{900,1800,2200,2530,2700,2800,2900,3060,3300,3800,4500,6500}){
   var mixer=BoxerAudio.CreateMixer(48000);var samples=new float[48000*16];
   // Crossfading must stay phase-aligned after several minutes of driving,
   // despite rounding each prepared loop to a whole number of WAV frames.
   if(rpm==2800){var block=new float[48000];for(int second=0;second<300;second++)mixer.Render(block,1,rpm,.75f,30,0,false);}
   mixer.Render(samples,1,rpm,.75f,30,0,false);
   using(var output=new BinaryWriter(File.Create($"Build/AudioPreview/impreza-{rpm}rpm.wav"))){
    Header(output,samples.Length,48000);
    foreach(float sample in samples)output.Write((short)Mathf.RoundToInt(sample*32767));
   }
   // Ignore individual combustion pulses; measure the slow swell heard at
   // steady speed over several loop periods, especially between RPM layers.
   const int window=7200,hop=480,warmup=48000;
   var levels=new System.Collections.Generic.List<double>();double energy=0;
   for(int i=0;i<samples.Length;i++){
    energy+=samples[i]*samples[i];if(i>=window)energy-=samples[i-window]*samples[i-window];
    if(i>=warmup+window&&i%hop==0)levels.Add(energy/window);
   }
   levels.Sort();double swellDb=10*Math.Log10(levels[levels.Count*9/10]/levels[levels.Count/10]);
   if(swellDb>1.2)throw new Exception($"Slow engine-volume pulsing at {rpm} RPM: {swellDb:F2} dB");
   Debug.Log($"ENGINE_PULSE_TEST rpm={rpm} slowEnvelopeSpread={swellDb:F3}dB pass=True");
  }
  // A live output-device rate change must preserve finite, continuous audio.
  var changed=BoxerAudio.CreateMixer(44100);var buffer=new float[4096];
  changed.Render(buffer,1,4500,1,50,.5f,false);float previous=buffer[buffer.Length-1];
  changed.SampleRate=48000;changed.Render(buffer,1,4500,1,50,.5f,false);
  if(Mathf.Abs(buffer[0]-previous)>.08f)throw new Exception("Discontinuity when changing audio sample rate");
  CheckTyreSqueal();
  Debug.Log("ENGINE_AUDIO_TEST passed: recorded loops, slow-pulse checks, RPM sweep, gear change, coast, mute/unmute, sample-rate change; previews in Build/AudioPreview");
 }

 static void CheckTyreSqueal(){
  foreach(int rate in new[]{44100,48000,96000}){
   var mixer=BoxerAudio.CreateMixer(rate);var straight=BoxerAudio.CreateMixer(rate);
   var block=new float[512*2];var baseline=new float[block.Length];
   double difference=0,tail=0;int audibleFrames=0,tailFrames=0;float peak=0,step=0,last=0;
   int frames=rate*12/512*512;
   using(var output=new BinaryWriter(File.Create($"Build/AudioPreview/impreza-cornering-{rate}.wav"))){
    Header(output,frames,rate);
    for(int frame=0;frame<frames;frame+=512){
     float t=frame/(float)rate;
     float demand=t<2?0:t<3?t-2:t<6?.8f:t<7?.8f*(7-t):t<9?0:1;
     float speed=t<9?30:0;bool muted=t>=8&&t<9;
     mixer.Render(block,2,3500,.6f,speed,demand,muted);
     straight.Render(baseline,2,3500,.6f,speed,0,muted);
     for(int i=0;i<block.Length;i+=2){
      float value=block[i],delta=value-baseline[i];
      if(!float.IsFinite(value)||value!=block[i+1])throw new Exception("Invalid tyre audio or stereo mismatch");
      if(t>8.1f&&t<8.9f&&value!=0)throw new Exception("Tyre audio ignored mute");
      if(t>3.5f&&t<5.5f){difference+=delta*delta;audibleFrames++;}
      if(t>10){tail+=delta*delta;tailFrames++;}
      peak=Mathf.Max(peak,Mathf.Abs(value));step=Mathf.Max(step,Mathf.Abs(value-last));last=value;
      output.Write((short)Mathf.RoundToInt(value*32767));
     }
    }
   }
   double audibleRms=Math.Sqrt(difference/audibleFrames),stationaryRms=Math.Sqrt(tail/tailFrames);
   if(audibleRms<.025||stationaryRms>.00001||peak>=.95f||step>.10f)
    throw new Exception($"Tyre audio gain/stop/headroom failed: audible={audibleRms} stopped={stationaryRms} peak={peak} step={step}");
   Debug.Log($"TYRE_AUDIO_TEST rate={rate} squealRms={audibleRms:F4} stationaryRms={stationaryRms:F6} peak={peak:F4} maxStep={step:F4} pass=True");
  }
 }

 static void Header(BinaryWriter output,int frames,int rate){
  output.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));output.Write(36+frames*2);
  output.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));output.Write(16);
  output.Write((short)1);output.Write((short)1);output.Write(rate);output.Write(rate*2);
  output.Write((short)2);output.Write((short)16);output.Write(System.Text.Encoding.ASCII.GetBytes("data"));output.Write(frames*2);
 }
}
