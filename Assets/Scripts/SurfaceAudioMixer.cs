using System;

// Original procedural rolling sounds. Called by the audio thread after the engine mix.
public sealed class SurfaceAudioMixer
{
 float asphalt,kerb,loose,speed,volume,low,gritLow,gritHigh;
 double phase;
 uint random=927451;
 static float Clamp(float x)=>Math.Max(0,Math.Min(1,x));
 public void Render(float[] data,int channels,int rate,float targetSpeed,float road,float paintedKerb,float ground,bool muted){
  float smooth=1-(float)Math.Exp(-1.0/(rate*.06));
  float lowFilter=1-(float)Math.Exp(-2*Math.PI*180/rate);
  float gritLowFilter=1-(float)Math.Exp(-2*Math.PI*120/rate),gritHighFilter=1-(float)Math.Exp(-2*Math.PI*1800/rate);
  road=Clamp(road);paintedKerb=Clamp(paintedKerb);ground=Clamp(ground);
  for(int i=0;i<data.Length;i+=channels){
   asphalt+=(road-asphalt)*smooth;kerb+=(paintedKerb-kerb)*smooth;loose+=(ground-loose)*smooth;
   speed+=(Math.Max(0,targetSpeed)-speed)*smooth;
   volume=muted?Math.Max(0,volume-1f/(rate*.012f)):Math.Min(1,volume+1f/(rate*.025f));
   random^=random<<13;random^=random>>17;random^=random<<5;
   float white=random/(float)uint.MaxValue*2-1;
   low+=(white-low)*lowFilter;gritLow+=(white-gritLow)*gritLowFilter;gritHigh+=(white-gritHigh)*gritHighFilter;
   phase+=2*Math.PI*(28+speed*1.4)/rate;if(phase>=2*Math.PI)phase-=2*Math.PI;
   float rolling=Clamp((speed-.5f)/25);
   float rumble=(float)(Math.Sin(phase)+.3*Math.Sin(2*phase));
   float value=(low*asphalt*.075f+(rumble*.15f+low*.09f)*kerb+(gritHigh-gritLow)*loose*.18f)*rolling*volume;
   for(int c=0;c<channels;c++)data[i+c]+=value;
  }
 }
}
