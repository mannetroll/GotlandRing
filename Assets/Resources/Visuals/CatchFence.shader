Shader "Gotland/Catch fence wire"
{
 Properties { _Color ("Wire colour", Color) = (.38,.43,.44,1) }
 SubShader {
  Tags { "RenderType"="Opaque" }
  Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   fixed4 _Color;
   struct v2f { float4 position : SV_POSITION; UNITY_FOG_COORDS(0) };
   v2f vert(appdata_base v) { v2f o; o.position=UnityObjectToClipPos(v.vertex); UNITY_TRANSFER_FOG(o,o.position); return o; }
   fixed4 frag(v2f i) : SV_Target { fixed4 color=_Color; UNITY_APPLY_FOG(i.fogCoord,color); return color; }
   #pragma multi_compile_fog
   ENDCG
  }
 }
}
