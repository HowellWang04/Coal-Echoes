// Mesh decal pass adapted from the installed HDRP 17.4 shader. Package includes retain lighting and surface blending.
Shader "Project Coal/Dirt Road Spline Decal"
{
    Properties
    {
        [MainColor] _BaseColor("_BaseColor", Color) = (1,1,1,1)
        [MainTexture] _BaseColorMap("BaseColorMap", 2D) = "white" {}
        _NormalMap("NormalMap", 2D) = "bump" {}     // Tangent space normal map
        _MaskMap("MaskMap", 2D) = "white" {}
        _DecalBlend("_DecalBlend", Range(0.0, 1.0)) = 0.5
        _NormalBlendSrc("_NormalBlendSrc", Float) = 0.0
        _MaskBlendSrc("_MaskBlendSrc", Float) = 1.0
        [Enum(Depth Bias, 0, View Bias, 1)] _DecalMeshBiasType("_DecalMeshBiasType", Int) = 0
        _DecalMeshDepthBias("_DecalMeshDepthBias", Float) = 0.0
        _DecalMeshViewBias("_DecalMeshViewBias", Float) = 0.0
        _DrawOrder("_DrawOrder", Int) = 0
        [HDR] _EmissiveColor("EmissiveColor", Color) = (0, 0, 0)
        // Used only to serialize the LDR and HDR emissive color in the material UI,
        // in the shader only the _EmissiveColor should be used
        [HideInInspector] _EmissiveColorLDR("EmissiveColor LDR", Color) = (0, 0, 0)
        [HDR][HideInInspector] _EmissiveColorHDR("EmissiveColor HDR", Color) = (0, 0, 0)
        _EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
        _EmissiveIntensityUnit("Emissive Mode", Int) = 0
        [ToggleUI] _UseEmissiveIntensity("Use Emissive Intensity", Int) = 0
        _EmissiveIntensity("Emissive Intensity", Float) = 1
        _EmissiveExposureWeight("Emissive Pre Exposure", Range(0.0, 1.0)) = 1.0

        // Remapping
        _MetallicRemapMin("_MetallicRemapMin", Range(0.0, 1.0)) = 0.0
        _MetallicRemapMax("_MetallicRemapMax", Range(0.0, 1.0)) = 1.0
        _SmoothnessRemapMin("SmoothnessRemapMin", Float) = 0.0
        _SmoothnessRemapMax("SmoothnessRemapMax", Float) = 1.0
        _AORemapMin("AORemapMin", Float) = 0.0
        _AORemapMax("AORemapMax", Float) = 1.0

        // scaling
        _DecalMaskMapBlueScale("_DecalMaskMapBlueScale", Range(0.0, 1.0)) = 1.0

        // Alternative when no mask map is provided
        _Smoothness("_Smoothness",  Range(0.0, 1.0)) = 0.5
        _Metallic("_Metallic",  Range(0.0, 1.0)) = 0.0
        _AO("_AO",  Range(0.0, 1.0)) = 1.0

        [ToggleUI]_AffectAlbedo("Boolean", Float) = 1
        [ToggleUI]_AffectNormal("Boolean", Float) = 1
        [ToggleUI]_AffectAO("Boolean", Float) = 0
        [ToggleUI]_AffectMetal("Boolean", Float) = 1
        [ToggleUI]_AffectSmoothness("Boolean", Float) = 1
        [ToggleUI]_AffectEmission("Boolean", Float) = 0

        // Stencil state
        [HideInInspector] _DecalStencilRef("_DecalStencilRef", Int) = 16
        [HideInInspector] _DecalStencilWriteMask("_DecalStencilWriteMask", Int) = 16

        // Decal color masks
        [HideInInspector]_DecalColorMask0("_DecalColorMask0", Int) = 0
        [HideInInspector]_DecalColorMask1("_DecalColorMask1", Int) = 0
        [HideInInspector]_DecalColorMask2("_DecalColorMask2", Int) = 0
        [HideInInspector]_DecalColorMask3("_DecalColorMask3", Int) = 0

        // TODO: Remove when name garbage is solve (see IsHDRenderPipelineDecal)
        // This marker allow to identify that a Material is a HDRP/Decal
        [HideInInspector]_Unity_Identify_HDRP_Decal("_Unity_Identify_HDRP_Decal", Float) = 1.0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
    //#pragma enable_d3d11_debug_symbols

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------
    #pragma shader_feature_local_fragment _COLORMAP
    #pragma shader_feature_local_fragment _MASKMAP
    #pragma shader_feature_local _NORMALMAP
    #pragma shader_feature_local_fragment _EMISSIVEMAP

    #pragma shader_feature_local_fragment _MATERIAL_AFFECTS_ALBEDO
    #pragma shader_feature_local_fragment _MATERIAL_AFFECTS_NORMAL
    #pragma shader_feature_local_fragment _MATERIAL_AFFECTS_MASKMAP

    #pragma multi_compile_instancing

    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------

    // Enable the support of global mip bias in the shader.
    // Only has effect if the global mip bias is enabled in shader config and DRS is enabled.
    #define SUPPORT_GLOBAL_MIP_BIAS

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    // All our shaders use same name for entry point
    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        Pass // 2
        {
            Name "DBufferMesh"
            Tags{"LightMode" = "DBufferMesh"}

            Stencil
            {
                WriteMask [_DecalStencilWriteMask]
                Ref [_DecalStencilRef]
                Comp Always
                Pass Replace
            }

            ZWrite Off
            ZTest LEqual

            // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
            Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 3 Zero OneMinusSrcColor

            ColorMask [_DecalColorMask0]
            ColorMask [_DecalColorMask1] 1
            ColorMask [_DecalColorMask2] 2
            ColorMask [_DecalColorMask3] 3

            HLSLPROGRAM

            #pragma multi_compile_fragment DECALS_3RT DECALS_4RT
            #pragma multi_compile_fragment _ DECAL_SURFACE_GRADIENT
            #pragma multi_compile _ DOTS_INSTANCING_ON
            // enable dithering LOD crossfade
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #define SHADERPASS SHADERPASS_DBUFFER_MESH
            #define ATTRIBUTES_NEED_COLOR
            #define VARYINGS_NEED_COLOR
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalProperties.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
            // The spline supplies a smooth endpoint fade through vertex alpha.
            #define _DecalBlend (_DecalBlend * input.color.a)
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalData.hlsl"
            #undef _DecalBlend
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

            ENDHLSL
        }

    }
}
