Shader "Gotland/Pine" {
 Properties {_MainTex("Pine",2D)="white" {} }
 SubShader { Tags {"DisableBatching"="True" "Queue"="AlphaTest" "RenderType"="TransparentCutout"} Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 offset:TEXCOORD1;fixed4 color:COLOR;};
 struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;UNITY_FOG_COORDS(1)};
 v2f vert(appdata v){v2f o;float3 anchor=mul(unity_ObjectToWorld,v.vertex).xyz;float3 right=normalize(float3(UNITY_MATRIX_V[0][0],0,UNITY_MATRIX_V[0][2]));float3 world=anchor+right*v.offset.x+float3(0,v.offset.y,0);o.vertex=mul(UNITY_MATRIX_VP,float4(world,1));o.uv=v.uv;o.color=v.color;UNITY_TRANSFER_FOG(o,o.vertex);return o;}
 fixed4 frag(v2f i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv);clip(c.a-.45);c.rgb*=i.color.rgb;UNITY_APPLY_FOG(i.fogCoord,c);return c;}
 ENDCG }
 }
}
