Shader "RainbowMA/White Shaded Wire"
{
    Properties
    {
        _WireWidth ("Line width", Range(0.5,4)) = 1.2
        _RainbowRotationAngle ("Rotation degrees", Float) = 0
        _RainbowObjectToAvatar0 ("Object to avatar X", Vector) = (1,0,0,0)
        _RainbowObjectToAvatar1 ("Object to avatar Y", Vector) = (0,1,0,0)
        _RainbowObjectToAvatar2 ("Object to avatar Z", Vector) = (0,0,1,0)
        _RainbowAvatarToObject0 ("Avatar to object X", Vector) = (1,0,0,0)
        _RainbowAvatarToObject1 ("Avatar to object Y", Vector) = (0,1,0,0)
        _RainbowAvatarToObject2 ("Avatar to object Z", Vector) = (0,0,1,0)
        _RainbowRowY ("Avatar height row", Vector) = (0,1,0,0)
        _RainbowMinY ("Min Y", Float) = 0
        _RainbowHeight ("Height", Float) = 1.8
        _RainbowSpeed ("Cycles per second", Float) = 0.15
        _RainbowWirePhase ("Wire phase offset", Float) = 0.666667
        _RainbowWireEmission ("Wire emission", Float) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Back ZWrite On Blend Off
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "RainbowRotation.hlsl"
            float _WireWidth;
            float4 _RainbowRowY;
            float _RainbowMinY, _RainbowHeight, _RainbowSpeed, _RainbowWirePhase, _RainbowWireEmission;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2g { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float height:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            struct g2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 bary:TEXCOORD1; float height:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            v2g vert(appdata v) { v2g o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); v.vertex.xyz=RainbowRotatePosition(v.vertex.xyz); v.normal=RainbowRotateNormal(v.normal); o.pos=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal); o.height=dot(_RainbowRowY,v.vertex); return o; }
            [maxvertexcount(3)] void geom(triangle v2g v[3], inout TriangleStream<g2f> stream)
            {
                for(int i=0;i<3;i++) { g2f o; UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(v[i],o); o.pos=v[i].pos; o.normal=v[i].normal; o.height=v[i].height; o.bary=float3(i==0,i==1,i==2); stream.Append(o); }
            }
            float4 frag(g2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n=normalize(i.normal);
                float light=saturate(dot(n,normalize(_WorldSpaceLightPos0.xyz)));
                float3 shade=max(0.65,ShadeSH9(float4(n,1)))+light*_LightColor0.rgb*0.35;
                float3 aa=smoothstep(0,fwidth(i.bary)*_WireWidth,i.bary);
                float face=min(aa.x,min(aa.y,aa.z));
                float phase=1-(i.height-_RainbowMinY)/max(_RainbowHeight,.001)-_Time.y*_RainbowSpeed+_RainbowWirePhase;
                float3 rainbow=saturate(abs(frac(phase+float3(0,2.0/3.0,1.0/3.0))*6-3)-1);
                return float4(lerp(rainbow*_RainbowWireEmission,min(shade,1),face),1);
            }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual Cull Back
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "RainbowRotation.hlsl"
            struct shadowOutput { V2F_SHADOW_CASTER; UNITY_VERTEX_OUTPUT_STEREO };
            shadowOutput shadowVert(appdata_base v)
            {
                shadowOutput o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                v.vertex.xyz=RainbowRotatePosition(v.vertex.xyz); v.normal=RainbowRotateNormal(v.normal);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            float4 shadowFrag(shadowOutput i):SV_Target { SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
}
