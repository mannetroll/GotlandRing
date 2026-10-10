using System;
using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class BoxerAudio : MonoBehaviour
{
 public volatile float Rpm=900,Load,Speed;
 public volatile bool Muted;
 public volatile float Asphalt,Kerb,LooseGround;
 readonly SurfaceAudioMixer surfaceMixer=new SurfaceAudioMixer();
 ImprezaEngineMixer mixer;
 AudioClip carrier;

 void Awake(){
  int rate=AudioSettings.outputSampleRate;
  mixer=CreateMixer(rate);
  var source=GetComponent<AudioSource>();
  carrier=AudioClip.Create("2022 Impreza recording mixer",rate,1,rate,false);
  source.clip=carrier;source.loop=true;source.spatialBlend=0;source.volume=1;
  source.Play();
  AudioSettings.OnAudioConfigurationChanged+=AudioConfigurationChanged;
 }

 public static ImprezaEngineMixer CreateMixer(int outputRate){
  var names=new[]{"ImprezaLow","ImprezaMid","ImprezaHigh"};
  var loops=new float[names.Length][];
  for(int i=0;i<names.Length;i++){
   var clip=Resources.Load<AudioClip>("Audio/"+names[i]);
   if(!clip||clip.channels!=1||clip.frequency!=44100)throw new InvalidOperationException("Expected mono 44.1 kHz engine recording: "+names[i]);
   loops[i]=new float[clip.samples];
   if(!clip.GetData(loops[i],0))throw new InvalidOperationException("Cannot read engine recording: "+names[i]);
  }
  return new ImprezaEngineMixer(loops,outputRate);
 }

 void AudioConfigurationChanged(bool deviceWasChanged){mixer.SampleRate=AudioSettings.outputSampleRate;}
 void OnAudioFilterRead(float[] data,int channels){mixer.Render(data,channels,Rpm,Load,Speed,Muted);surfaceMixer.Render(data,channels,mixer.SampleRate,Speed,Asphalt,Kerb,LooseGround,Muted);}
 void OnDestroy(){AudioSettings.OnAudioConfigurationChanged-=AudioConfigurationChanged;Destroy(carrier);}
}
