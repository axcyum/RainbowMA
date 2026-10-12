#include "RainbowRotation.hlsl"
#if defined(LIL_APP_NORMAL)
    #define RAINBOW_ROTATE_NORMAL input.normalOS = RainbowRotateNormal(input.normalOS);
#else
    #define RAINBOW_ROTATE_NORMAL
#endif
#if defined(LIL_APP_TANGENT)
    #define RAINBOW_ROTATE_TANGENT input.tangentOS.xyz = RainbowRotateDirection(input.tangentOS.xyz);
#else
    #define RAINBOW_ROTATE_TANGENT
#endif
#define LIL_CUSTOM_VERTEX_OS { positionOS.xyz = RainbowRotatePosition(positionOS.xyz); RAINBOW_ROTATE_NORMAL RAINBOW_ROTATE_TANGENT }
float4 _RainbowRowY;
float _RainbowMinY, _RainbowHeight, _RainbowEmission, _RainbowOutlineEmission;
float _RainbowSpeed, _RainbowOutlinePhase;
float3 RainbowSpectrum(float t)
{
    float h = frac(t - _Time.y * _RainbowSpeed);
    return saturate(abs(frac(h + float3(0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0) - 1.0);
}
#if defined(LIL_OUTLINE)
  #if defined(UNITY_PASS_FORWARDADD)
    #define BEFORE_OUTPUT { fd.col.rgb = 0; }
  #else
    #define BEFORE_OUTPUT { float4 p = mul(unity_WorldToObject, float4(fd.positionWS,1)); float t = 1 - (dot(_RainbowRowY,p)-_RainbowMinY)/max(_RainbowHeight,0.001); fd.col.rgb = RainbowSpectrum(t + _RainbowOutlinePhase) * _RainbowOutlineEmission; }
  #endif
#elif !defined(UNITY_PASS_FORWARDADD)
    #define BEFORE_OUTPUT { float4 p = mul(unity_WorldToObject, float4(fd.positionWS,1)); float t = 1 - (dot(_RainbowRowY,p)-_RainbowMinY)/max(_RainbowHeight,0.001); float3 rainbow = RainbowSpectrum(t); fd.col.rgb = fd.col.rgb * rainbow + rainbow * _RainbowEmission; }
#endif



