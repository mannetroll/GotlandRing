Shader "Gotland/Quarry limestone"
{
 Properties { _MainTex ("Stone detail", 2D) = "white" {} _Color ("Limestone", Color) = (.76,.75,.70,1) }
 SubShader {
  Tags { "RenderType"="Opaque" }
  LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  sampler2D _MainTex;
  fixed4 _Color;
  struct Input { float3 worldPos; float3 worldNormal; };
  void surf (Input IN, inout SurfaceOutputStandard o) {
   float3 weights=pow(abs(IN.worldNormal),4);weights/=weights.x+weights.y+weights.z;
   float3 p=IN.worldPos/9;
   fixed3 grain=tex2D(_MainTex,p.yz).rgb*weights.x+tex2D(_MainTex,p.xz).rgb*weights.y+tex2D(_MainTex,p.xy).rgb*weights.z;
   float detail=dot(grain,float3(.2126,.7152,.0722));
   o.Albedo=_Color.rgb*(.78+detail*.32);o.Metallic=0;o.Smoothness=.02;o.Occlusion=1;o.Alpha=1;
  }
  ENDCG
 }
}
