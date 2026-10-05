using System;
using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class BoxerAudio : MonoBehaviour
{
 public volatile float Rpm=900,Load,Speed,Slip; public volatile bool Muted;
 AudioSource recording;
 double phase,turboPhase;float smoothedRpm=900,lastLoad,release,noise;uint random=1234567;int rate;
 void Awake(){rate=AudioSettings.outputSampleRate;var source=GetComponent<AudioSource>();source.clip=AudioClip.Create("Procedural boxer carrier",rate,1,rate,false);source.loop=true;source.spatialBlend=0;source.volume=.55f;source.Play();var child=new GameObject("Engine texture from IMG_0286");child.transform.SetParent(transform,false);recording=child.AddComponent<AudioSource>();recording.clip=Resources.Load<AudioClip>("TrackEngine");recording.loop=true;recording.volume=.14f;recording.spatialBlend=0;recording.Play();}
 void Update(){if(recording){recording.pitch=Mathf.Clamp(Rpm/3200f,.45f,2);recording.volume=Muted?0:.08f+Load*.12f;}}
 void OnAudioFilterRead(float[] data,int channels){float target=Rpm,load=Load,speed=Speed,slip=Slip;bool mute=Muted;if(lastLoad>.5f&&load<.1f)release=.22f;lastLoad=load;
  for(int i=0;i<data.Length;i+=channels){smoothedRpm+=(target-smoothedRpm)*.00012f;phase+=smoothedRpm/120.0/rate;if(phase>=1)phase-=1;double pulse=0;foreach(double offset in offsets){double d=phase-offset;if(d<0)d+=1;pulse+=Math.Exp(-d*65)*Math.Sin(d*130);}
   random^=random<<13;random^=random>>17;random^=random<<5;float white=(random/(float)uint.MaxValue)*2-1;noise+=.08f*(white-noise);turboPhase+=(1600+load*smoothedRpm*.4)/rate;turboPhase%=1;release=Math.Max(0,release-1f/rate);
   float value=(float)(pulse*.42+Math.Sin(phase*Math.PI*4)*.09)*( .35f+load*.6f)+noise*(.03f+speed*.0015f+slip*.09f)+(float)Math.Sin(turboPhase*2*Math.PI)*load*.012f+white*release*.3f;
   value=mute?0:(float)Math.Tanh(value);for(int ch=0;ch<channels;ch++)data[i+ch]=value;
  }
 }
 static readonly double[] offsets={0,.18,.5,.68};
}
