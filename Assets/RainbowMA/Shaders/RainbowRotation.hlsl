#ifndef RAINBOW_ROTATION_INCLUDED
#define RAINBOW_ROTATION_INCLUDED
float _RainbowRotationAngle;
float4 _RainbowObjectToAvatar0, _RainbowObjectToAvatar1, _RainbowObjectToAvatar2;
float4 _RainbowAvatarToObject0, _RainbowAvatarToObject1, _RainbowAvatarToObject2;
float3x3 RainbowObjectToAvatar() { return float3x3(_RainbowObjectToAvatar0.xyz,_RainbowObjectToAvatar1.xyz,_RainbowObjectToAvatar2.xyz); }
float3x3 RainbowAvatarToObject() { return float3x3(_RainbowAvatarToObject0.xyz,_RainbowAvatarToObject1.xyz,_RainbowAvatarToObject2.xyz); }
float3 RainbowRotateY(float3 p)
{
    float s,c; sincos(radians(_RainbowRotationAngle),s,c);
    return float3(c*p.x+s*p.z,p.y,-s*p.x+c*p.z);
}
float3 RainbowRotatePosition(float3 p)
{
    float3 a=mul(RainbowObjectToAvatar(),p)+float3(_RainbowObjectToAvatar0.w,_RainbowObjectToAvatar1.w,_RainbowObjectToAvatar2.w);
    return mul(RainbowAvatarToObject(),RainbowRotateY(a))+float3(_RainbowAvatarToObject0.w,_RainbowAvatarToObject1.w,_RainbowAvatarToObject2.w);
}
float3 RainbowRotateDirection(float3 d) { return mul(RainbowAvatarToObject(),RainbowRotateY(mul(RainbowObjectToAvatar(),d))); }
float3 RainbowRotateNormal(float3 n) { return mul(transpose(RainbowObjectToAvatar()),RainbowRotateY(mul(transpose(RainbowAvatarToObject()),n))); }
#endif
