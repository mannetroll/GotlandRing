Shader "Gotland/AutomotiveGlass" {
 Properties { _Color("Tint",Color)=(.25,.35,.4,.10) }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata { float4 vertex:POSITION;float3 normal:NORMAL;};
 struct v2f { float4 vertex:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;};
 float4 _Color;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.view=WorldSpaceViewDir(v.vertex);return o;}
 fixed4 frag(v2f i):SV_Target {float3 n=normalize(i.normal),v=normalize(i.view);float f=pow(1-saturate(dot(n,v)),4);half4 c=UNITY_SAMPLE_TEXCUBE(unity_SpecCube0,reflect(-v,n));float3 r=DecodeHDR(c,unity_SpecCube0_HDR);return float4(lerp(_Color.rgb,r,.7),.28+.35*f);}
 ENDCG }
 }
}
