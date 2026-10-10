using System;
using System.IO;
using UnityEngine;

public static class SurfaceAudioChecks
{
 public static void Run(){
  foreach(int rate in new[]{44100,48000,96000}){
   var mixer=new SurfaceAudioMixer();var engine=BoxerAudio.CreateMixer(rate);
   var block=new float[512*2];var engineBlock=new float[block.Length];
   double[] energy=new double[3];int[] counts=new int[3];float peak=0,step=0,last=0;
   int frames=rate*16/512*512;
   using(var output=new BinaryWriter(File.Create($"Build/AudioPreview/impreza-surfaces-{rate}.wav"))){
    Header(output,frames,rate);
    for(int frame=0;frame<frames;frame+=512){
     float t=frame/(float)rate,speed=t<10?30:0;
     int surface=t<3?0:t<6?1:2;bool muted=t>=8&&t<9;
     float level=t>=9?0:1;
     Array.Clear(block,0,block.Length);
     mixer.Render(block,2,rate,speed,surface==0?level:0,surface==1?level:0,surface==2?level:0,muted);
     engine.Render(engineBlock,2,t<10?4500:900,.8f,speed,muted);
     for(int i=0;i<block.Length;i+=2){
      float sound=block[i],combined=sound+engineBlock[i];
      if(!float.IsFinite(combined)||sound!=block[i+1])throw new Exception("Invalid surface sound or stereo mismatch");
      if(t>8.1f&&t<8.9f&&sound!=0)throw new Exception("Surface sound ignored mute");
      if(t>11&&Mathf.Abs(sound)>.000001f)throw new Exception("Disabled surface sound did not fade");
      if(t>surface*3+1&&t<surface*3+2){energy[surface]+=sound*sound;counts[surface]++;}
      peak=Mathf.Max(peak,Mathf.Abs(combined));step=Mathf.Max(step,Mathf.Abs(combined-last));last=combined;
      output.Write((short)Mathf.RoundToInt(combined*32767));
     }
    }
   }
   double road=Math.Sqrt(energy[0]/counts[0]),kerb=Math.Sqrt(energy[1]/counts[1]),loose=Math.Sqrt(energy[2]/counts[2]);
   if(road<.002||kerb<road*2||loose<road*2||peak>=.95f||step>.12f)
    throw new Exception($"Surface sound level/headroom: road={road} kerb={kerb} loose={loose} peak={peak} step={step}");
   // Wheels can stop while the contact still requests a surface.
   var stop=new SurfaceAudioMixer();var samples=new float[rate*2];
   stop.Render(samples,1,rate,35,0,1,0,false);
   Array.Clear(samples,0,samples.Length);stop.Render(samples,1,rate,0,0,1,0,false);
   for(int i=rate;i<samples.Length;i++)if(samples[i]!=0)throw new Exception("Stationary wheel rumble");
   Array.Clear(samples,0,samples.Length);stop.Render(samples,1,rate,35,0,1,0,false);
   Array.Clear(samples,0,samples.Length);stop.Render(samples,1,rate,35,0,0,0,false);
   for(int i=rate;i<samples.Length;i++)if(Mathf.Abs(samples[i])>.000001f)throw new Exception("Airborne wheel rumble");
   Debug.Log($"SURFACE_AUDIO_TEST rate={rate} asphaltRms={road:F4} kerbRms={kerb:F4} looseRms={loose:F4} peak={peak:F4} maxStep={step:F4} pass=True");
  }
  var changed=new SurfaceAudioMixer();var data=new float[4096];
  changed.Render(data,1,44100,40,0,1,0,false);float previous=data[data.Length-1];Array.Clear(data,0,data.Length);
  changed.Render(data,1,48000,40,0,1,0,false);
  if(Mathf.Abs(data[0]-previous)>.03f)throw new Exception("Surface sound sample-rate change discontinuity");
 }
 static void Header(BinaryWriter output,int frames,int rate){
  output.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));output.Write(36+frames*2);
  output.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));output.Write(16);output.Write((short)1);output.Write((short)1);
  output.Write(rate);output.Write(rate*2);output.Write((short)2);output.Write((short)16);
  output.Write(System.Text.Encoding.ASCII.GetBytes("data"));output.Write(frames*2);
 }
}
