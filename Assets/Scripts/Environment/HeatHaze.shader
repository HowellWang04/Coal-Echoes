Shader "Coal Echoes/Heat Haze"
{
    Properties
    {
        _Intensity ("Intensity (pixels)", Range(0, 10)) = 0.8
        _Speed ("Flow Speed", Range(0, 2)) = 0.35
        _NoiseScale ("Noise Scale", Float) = 1.2
        _StartDistance ("Start Distance (m)", Float) = 15
        _FadeDistance ("Distance Fade (m)", Float) = 45
        _Height ("Height Above Ground (m)", Float) = 4
        _EdgeFade ("Area Edge Fade (m)", Float) = 25
        _Area ("Area Center XZ / Size XZ", Vector) = (0, 0, 360, 350)
        [NoScaleOffset] _GroundHeight ("Ground Height", 2D) = "black" {}
    }

    HLSLINCLUDE
    #pragma vertex Vert
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    TEXTURE2D(_GroundHeight);
    SAMPLER(sampler_GroundHeight);

    CBUFFER_START(UnityPerMaterial)
        float4 _Area;
        float _Intensity, _Speed, _NoiseScale;
        float _StartDistance, _FadeDistance, _Height, _EdgeFade;
    CBUFFER_END

    float Hash(float2 p)
    {
        p = frac(p * float2(123.34, 456.21));
        p += dot(p, p + 45.32);
        return frac(p.x * p.y);
    }

    float Noise(float2 p)
    {
        float2 cell = floor(p);
        float2 f = frac(p);
        f = f * f * (3.0 - 2.0 * f);
        return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), f.x), f.y);
    }

    float4 CopyColor(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return float4(CustomPassLoadCameraColor(input.positionCS.xy, 0), 1);
    }

    float4 HeatDistortion(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.positionCS.xy * _ScreenSize.zw;
        float depth = LoadCameraDepth(input.positionCS.xy);
        float4 original = CustomPassLoadCustomColor(input.positionCS.xy);
        if (depth == UNITY_RAW_FAR_CLIP_VALUE || _Intensity <= 0)
            return original;

        PositionInputs position = GetPositionInput(input.positionCS.xy, _ScreenSize.zw,
            depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float3 world = GetAbsolutePositionWS(position.positionWS);
        float2 local = world.xz - _Area.xy;
        float2 edge = _Area.zw * 0.5 - abs(local);
        float mask = smoothstep(0, max(_EdgeFade, 0.01), min(edge.x, edge.y));
        float ground = SAMPLE_TEXTURE2D_LOD(_GroundHeight, sampler_GroundHeight,
            local / _Area.zw + 0.5, 0).r;
        float height = world.y - ground;
        mask *= smoothstep(-2, 0, height) * (1 - smoothstep(0, max(_Height, 0.01), height));
        mask *= smoothstep(_StartDistance, _StartDistance + max(_FadeDistance, 0.01),
            distance(world, _WorldSpaceCameraPos));
        if (mask <= 0)
            return original;

        // World anchored noise keeps the ripples attached to the ground as the camera moves.
        float time = _TimeParameters.x * _Speed;
        float2 p = (local + world.y * float2(0.37, 0.61)) * _NoiseScale;
        float2 wave = float2(Noise(p + float2(time, time * 0.7)),
            Noise(p * 1.37 + float2(-time * 0.6, time) + 19.3)) * 2 - 1;
        float2 shiftedUV = clamp(uv + wave * (_Intensity * mask) * _ScreenSize.zw,
            _ScreenSize.zw * 0.5, 1 - _ScreenSize.zw * 0.5);

        // Do not pull a nearby object into the distorted background at depth edges.
        float shiftedDepth = LoadCameraDepth(uint2(shiftedUV * _ScreenSize.xy));
        float shiftedDistance = LinearEyeDepth(shiftedDepth, _ZBufferParams);
        if (shiftedDistance < position.linearDepth - max(0.5, position.linearDepth * 0.02))
            return original;
        return CustomPassSampleCustomColor(shiftedUV);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "Copy Color"
            HLSLPROGRAM
            #pragma fragment CopyColor
            ENDHLSL
        }
        Pass
        {
            Name "Heat Distortion"
            HLSLPROGRAM
            #pragma fragment HeatDistortion
            ENDHLSL
        }
    }
    Fallback Off
}
