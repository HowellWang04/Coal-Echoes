// Terrain layer sampling that hides texture repetition, called from SG_TerrainAntiTiling.
// Hex tiling: every hex cell shows the layer texture with its own random rotation and offset, and each pixel
// blends the three nearest cells (Mikkelsen 2022, "Practical Real-Time Hex-Tiling", JCGT 11(3)).
// Distance blend: far away each layer is also sampled at a larger tile size, so mountains lose the small repeat.
// Layer textures and per-layer settings come from HDRP's TerrainLit_Splatmap.hlsl, included by every TerrainLit graph pass.

#ifndef COAL_ECHOES_TERRAIN_ANTI_TILING
#define COAL_ECHOES_TERRAIN_ANTI_TILING

// HDRP 17.4 TerrainLit graph template calls ApplyDecalToSurfaceData with 3 arguments when decals use additive normal
// blending (DECAL_SURFACE_GRADIENT) but the graph does not define SURFACE_GRADIENT. That overload does not exist, so the
// GBuffer pass fails to compile and the terrain disappears. This supplies it: convert the normal to a surface gradient,
// let HDRP apply the decal, then convert back.
#if HAVE_DECALS && defined(DECAL_SURFACE_GRADIENT) && !defined(SURFACE_GRADIENT)
void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, float3 vtxNormal, inout SurfaceData surfaceData)
{
    float3 surfaceGradient = SurfaceGradientFromPerturbedNormal(vtxNormal, surfaceData.normalWS);
    ApplyDecalToSurfaceData(decalSurfaceData, vtxNormal, surfaceData, surfaceGradient);
    surfaceData.normalWS = SurfaceGradientResolveNormal(vtxNormal, surfaceGradient);
}
#endif

struct AntiTilingSettings
{
    float hexCellSize;
    float hexRotation;
    float hexSharpness;
    float farScale;
};

struct LayerSample
{
    float4 albedo;
    float3 normal;
    float4 mask;
};

float3 HexHash(float2 p)
{
    float3 p3 = frac(p.xyx * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yxz + 33.33);
    return frac((p3.xxy + p3.yzz) * p3.zyx);
}

// Same unpacking as SampleNormalGrad in TerrainLit_Splatmap.hlsl
float3 UnpackLayerNormal(float4 packedNormal, float scale)
{
#ifdef SURFACE_GRADIENT
    #ifdef UNITY_NO_DXT5nm
        return float3(UnpackDerivativeNormalRGB(packedNormal, scale), 0);
    #else
        return float3(UnpackDerivativeNormalRGorAG(packedNormal, scale), 0);
    #endif
#else
    #ifdef UNITY_NO_DXT5nm
        return UnpackNormalRGB(packedNormal, scale);
    #else
        return UnpackNormalMapRGorAG(packedNormal, scale);
    #endif
#endif
}

// tiles is how many times the texture repeats across one terrain tile (_SplatN_ST.xy)
LayerSample SampleLayerHex(TEXTURE2D_PARAM(splatMap, splatSampler), TEXTURE2D_PARAM(normalMap, normalSampler), TEXTURE2D_PARAM(maskMap, maskSampler),
    float normalScale, float2 uv, float2 dUVdx, float2 dUVdy, float2 tiles, float2 tileOffset, AntiTilingSettings settings)
{
    // Skewed triangle grid with whole-number coefficients, so the hex pattern repeats exactly once per terrain tile
    // and neighbouring tiles meet without a seam. Cells are equilateral when m = n * sqrt(3).
    float m = max(1.0, round(tiles.x * 2.0 * sqrt(3.0) / settings.hexCellSize));
    float n = max(1.0, round(tiles.y * 2.0 / settings.hexCellSize));
    float2 skewed = float2(m * uv.x - n * uv.y, 2.0 * n * uv.y);
    float2 baseId = floor(skewed);
    float2 f = frac(skewed);
    float fz = 1.0 - f.x - f.y;
    float s = step(0.0, -fz);
    float s2 = 2.0 * s - 1.0;
    float3 bary = float3(-fz * s2, s - f.y * s2, s - f.x * s2);
    float2 cellId[3] = { baseId + float2(s, s), baseId + float2(s, 1.0 - s), baseId + float2(1.0 - s, s) };

    float2 st = uv * tiles + tileOffset;
    float2 stdx = dUVdx * tiles;
    float2 stdy = dUVdy * tiles;

    float2 cellUV[3];
    float2x2 cellRot[3];
    float4 albedo[3];
    float3 weight;
    [unroll] for (int i = 0; i < 3; i++)
    {
        // Wrap the id onto one terrain tile so a cell gets the same random values on both sides of a border
        float2 id = cellId[i];
        id += float2(n, -2.0 * n) * floor(id.y / (2.0 * n));
        id.x -= m * floor(id.x / m);
        float3 h = HexHash(id);

        float sn, cs;
        sincos((h.z * 2.0 - 1.0) * PI * settings.hexRotation, sn, cs);
        cellRot[i] = float2x2(cs, -sn, sn, cs);

        float2 centre = float2((cellId[i].x + 0.5 * cellId[i].y) / m, cellId[i].y / (2.0 * n)) * tiles + tileOffset;
        cellUV[i] = mul(cellRot[i], st - centre) + centre + h.xy;
        albedo[i] = SAMPLE_TEXTURE2D_GRAD(splatMap, splatSampler, cellUV[i], mul(cellRot[i], stdx), mul(cellRot[i], stdy));

        // Bright features cover darker ones at cell borders, so the blend keeps contrast instead of turning flat
        float luminance = dot(albedo[i].rgb, float3(0.299, 0.587, 0.114));
        weight[i] = pow(saturate(bary[i]), settings.hexSharpness) * lerp(1.0, luminance, 0.6);
    }
    weight /= weight.x + weight.y + weight.z;

    LayerSample result;
    result.albedo = albedo[0] * weight.x + albedo[1] * weight.y + albedo[2] * weight.z;
    result.normal = 0;
    result.mask = 0;
    [unroll] for (int j = 0; j < 3; j++)
    {
        float2 dx = mul(cellRot[j], stdx);
        float2 dy = mul(cellRot[j], stdy);
#ifdef _NORMALMAP
        float3 normal = UnpackLayerNormal(SAMPLE_TEXTURE2D_GRAD(normalMap, normalSampler, cellUV[j], dx, dy), normalScale);
        // The texture was rotated, so rotate its normal back into terrain space
        normal.xy = mul(normal.xy, cellRot[j]);
        result.normal += normal * weight[j];
#endif
#ifdef _MASKMAP
        result.mask += SAMPLE_TEXTURE2D_GRAD(maskMap, maskSampler, cellUV[j], dx, dy) * weight[j];
#endif
    }
    return result;
}

LayerSample SampleLayer(TEXTURE2D_PARAM(splatMap, splatSampler), TEXTURE2D_PARAM(normalMap, normalSampler), TEXTURE2D_PARAM(maskMap, maskSampler),
    float normalScale, float2 uv, float2 dUVdx, float2 dUVdy, float2 tiles, float2 tileOffset, float farBlend, AntiTilingSettings settings)
{
    LayerSample nearSample, farSample;
    ZERO_INITIALIZE(LayerSample, nearSample);
    ZERO_INITIALIZE(LayerSample, farSample);

    UNITY_BRANCH if (farBlend < 1.0)
    {
        nearSample = SampleLayerHex(TEXTURE2D_ARGS(splatMap, splatSampler), TEXTURE2D_ARGS(normalMap, normalSampler), TEXTURE2D_ARGS(maskMap, maskSampler),
            normalScale, uv, dUVdx, dUVdy, tiles, tileOffset, settings);
    }
    UNITY_BRANCH if (farBlend > 0.0)
    {
        // Whole repeats per terrain tile keep the large pattern continuous across tile borders
        float2 farTiles = max(1.0, round(tiles / settings.farScale));
        farSample = SampleLayerHex(TEXTURE2D_ARGS(splatMap, splatSampler), TEXTURE2D_ARGS(normalMap, normalSampler), TEXTURE2D_ARGS(maskMap, maskSampler),
            normalScale, uv, dUVdx, dUVdy, farTiles, tileOffset, settings);
    }

    LayerSample result;
    result.albedo = lerp(nearSample.albedo, farSample.albedo, farBlend);
    result.normal = lerp(nearSample.normal, farSample.normal, farBlend);
    result.mask = lerp(nearSample.mask, farSample.mask, farBlend);
    return result;
}

#ifdef _MASKMAP
    #define APPLY_LAYER_MASK_MAP(i, layer, layerMask) layerMask = lerp(layerMask, layer.mask * _MaskMapRemapScale##i + _MaskMapRemapOffset##i, _LayerHasMask##i)
#else
    #define APPLY_LAYER_MASK_MAP(i, layer, layerMask)
#endif

// Mirrors TerrainSplatBlend in TerrainLit_Splatmap.hlsl, with the layer sampling swapped out
#define ANTI_TILING_LAYER(i, layerWeight)                                                                                           \
    UNITY_BRANCH if (layerWeight > 0.0)                                                                                             \
    {                                                                                                                               \
        LayerSample layer = SampleLayer(TEXTURE2D_ARGS(_Splat##i, sampler_Splat0), TEXTURE2D_ARGS(_Normal##i, sampler_Splat0),      \
            TEXTURE2D_ARGS(_Mask##i, sampler_Splat0), _NormalScale##i, UV, dUVdx, dUVdy, _Splat##i##_ST.xy, _Splat##i##_ST.zw,      \
            farBlend, settings);                                                                                                    \
        float4 layerMask = float4(_Metallic##i, _MaskMapRemapOffset##i.y + _MaskMapRemapScale##i.y, 0.0, layer.albedo.a * _Smoothness##i); \
        APPLY_LAYER_MASK_MAP(i, layer, layerMask);                                                                                  \
        albedo += layer.albedo.rgb * _DiffuseRemapScale##i.rgb * layerWeight;                                                       \
        normal += layer.normal * layerWeight;                                                                                       \
        masks += layerMask.xyw * layerWeight;                                                                                       \
    }

void TerrainAntiTiling_float(float2 UV, float3 PositionWS, float HexCellSize, float HexRotation, float HexSharpness,
    float FarScale, float FarStart, float FarEnd, float FarStrength,
    out float3 BaseColor, out float3 NormalTS, out float Metallic, out float Smoothness, out float Occlusion)
{
#ifdef SHADERGRAPH_PREVIEW
    BaseColor = float3(0.5, 0.5, 0.5);
    NormalTS = float3(0.0, 0.0, 1.0);
    Metallic = 0.0;
    Smoothness = 0.0;
    Occlusion = 1.0;
#else
    AntiTilingSettings settings;
    settings.hexCellSize = max(HexCellSize, 0.05);
    settings.hexRotation = HexRotation;
    settings.hexSharpness = HexSharpness;
    settings.farScale = max(FarScale, 1.0);

    #ifdef _TERRAIN_BASEMAP_GEN
        // The distant base map is baked without a camera, so it always gets the far look
        float farBlend = FarStrength;
    #else
        // Position is camera relative in HDRP, so its length is the view distance
        float farBlend = smoothstep(FarStart, FarEnd, length(PositionWS)) * FarStrength;
    #endif

    float2 dUVdx = ddx(UV);
    float2 dUVdy = ddy(UV);

    float2 controlUV0 = (UV * (_Control0_TexelSize.zw - 1.0) + 0.5) * _Control0_TexelSize.xy;
    float4 weights0 = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, controlUV0);
    #ifdef _TERRAIN_8_LAYERS
        float2 controlUV1 = (UV * (_Control1_TexelSize.zw - 1.0) + 0.5) * _Control1_TexelSize.xy;
        float4 weights1 = SAMPLE_TEXTURE2D(_Control1, sampler_Control0, controlUV1);
    #endif

    float3 albedo = 0;
    float3 normal = 0;
    float3 masks = 0;
    ANTI_TILING_LAYER(0, weights0.x);
    ANTI_TILING_LAYER(1, weights0.y);
    ANTI_TILING_LAYER(2, weights0.z);
    ANTI_TILING_LAYER(3, weights0.w);
    #ifdef _TERRAIN_8_LAYERS
        ANTI_TILING_LAYER(4, weights1.x);
        ANTI_TILING_LAYER(5, weights1.y);
        ANTI_TILING_LAYER(6, weights1.z);
        ANTI_TILING_LAYER(7, weights1.w);
    #endif

    BaseColor = albedo;
    NormalTS = normal;
    Metallic = masks.x;
    Occlusion = masks.y;
    Smoothness = masks.z;
#endif
}

#undef ANTI_TILING_LAYER
#undef APPLY_LAYER_MASK_MAP

#endif
