Shader "Gotland/Pine" {
 Properties {_MainTex("Pine",2D)="white" {} }
 SubShader { Tags {"DisableBatching"="True" "Queue"="AlphaTest" "RenderType"="TransparentCutout"} Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;UNITY_FOG_COORDS(1)};
 v2f vert(appdata v){v2f o;float3 center=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;float width=length(unity_ObjectToWorld._m00_m10_m20);float height=length(unity_ObjectToWorld._m01_m11_m21);float3 right=normalize(float3(UNITY_MATRIX_V[0][0],0,UNITY_MATRIX_V[0][2]));float3 world=center+right*v.vertex.x*width+float3(0,v.vertex.y*height,0);o.vertex=mul(UNITY_MATRIX_VP,float4(world,1));o.uv=v.uv;UNITY_TRANSFER_FOG(o,o.vertex);return o;}
 fixed4 frag(v2f i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv);clip(c.a-.45);UNITY_APPLY_FOG(i.fogCoord,c);return c;}
 ENDCG }
 }
}
