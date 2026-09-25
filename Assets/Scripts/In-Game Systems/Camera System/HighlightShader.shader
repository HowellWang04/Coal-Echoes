Shader "Custom/HighlightOverlay"
{
    Properties
    {
        _HighlightColor ("Highlight Color", Color) = (1, 0.8, 0, 0.1)
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 1.5
        _BandWidth ("Band Width", Range(0.01, 0.5)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" "RenderPipeline"="HDRenderPipeline" }

        Pass
        {
            Name "HighlightOverlay"
            Tags { "LightMode"="ForwardOnly" }
            Cull Back
            ZWrite Off 
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _HighlightColor;
                float _FresnelPower;
                float _BandWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2; 
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(worldPos);
                output.positionWS = GetAbsolutePositionWS(worldPos);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float fresnel = 1.0 - saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                fresnel = pow(fresnel, _FresnelPower);

                float sweep = frac(_Time.y * 0.5);
                float band = input.positionWS.x + input.positionWS.y;
                band = frac(band * 0.3 + sweep);
                band = smoothstep(0.5 - _BandWidth, 0.5, band) - smoothstep(0.5, 0.5 + _BandWidth, band);

                float alpha = _HighlightColor.a + fresnel * (1.0 - _HighlightColor.a);
                alpha += band * 0.4; 
                return half4(_HighlightColor.rgb + band * 0.3, saturate(alpha));
            }
            ENDHLSL
        }
    }
}