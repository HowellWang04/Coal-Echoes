// Halftone grid adapted from Daniel Ilett's Halftone.shadergraph, copyright (c) 2022 Daniel Ilett.
// Paper absorption, faded ink and wear are project-specific additions.
// MIT license and upstream provenance are included with this asset and in ArtSource.
Shader "Hidden/Coal Echoes/Newspaper Halftone"
{
    Properties
    {
        _MainTex ("Camera Image", 2D) = "white" {}
        _PaperTex ("Paper Texture", 2D) = "white" {}
        _PaperUV ("Paper UV (scale, offset)", Vector) = (0.5, 0.5, 0.25, 0.25)
        _PrintRect ("Photo on Paper (offset, size)", Vector) = (0, 0, 1, 1)
        _PaperSize ("Paper Grain Size", Vector) = (1920, 1080, 0, 0)
        _PaperColor ("Paper Color", Color) = (0.865, 0.85, 0.82, 1)
        _InkColor ("Ink Color", Color) = (0.16, 0.16, 0.155, 1)
        _CircleDensity ("Circle Density", Range(10, 240)) = 150
        _Rotation ("Grid Rotation (radians)", Range(0, 3.14159)) = 0.785398
        _LitThreshold ("Lit Threshold", Range(0, 1)) = 0.72
        _FalloffThreshold ("Falloff Threshold", Range(0.1, 2)) = 0.85
        _Softness ("Dot Edge Softness", Range(0.001, 0.3)) = 0.04
        _Contrast ("Image Contrast", Range(0.5, 3)) = 2.2
        _PaperAmount ("Paper Texture Strength", Range(0, 1)) = 0.55
        _DotStrength ("Visible Dot Strength", Range(0, 1)) = 0.12
        _DotRoughness ("Ragged Dot Edges", Range(0, 0.6)) = 0.12
        _DisplayScale ("Photo Width on Screen", Range(0.05, 1)) = 1
        _DotFadeStart ("Dots Appear at Pitch (1920-wide px)", Range(2, 12)) = 4
        _DotFadeEnd ("Dots Full at Pitch (1920-wide px)", Range(2, 16)) = 6
        _InkDensity ("Ink Density", Range(0, 1)) = 0.94
        _InkSpread ("Ink Spread (source pixels)", Range(0, 3)) = 1.15
        _InkWear ("Uneven Ink Coverage", Range(0, 1)) = 0.22
        _EffectStrength ("Print Effect", Range(0, 1)) = 1
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex, _PaperTex;
            float4 _MainTex_TexelSize;
            float4 _PaperColor, _InkColor;
            float4 _PaperUV, _PrintRect, _PaperSize;
            float _CircleDensity, _Rotation, _LitThreshold, _FalloffThreshold;
            float _Softness, _Contrast, _PaperAmount, _EffectStrength;
            float _DotStrength, _InkDensity, _InkSpread, _InkWear;
            float _DotRoughness, _DisplayScale, _DotFadeStart, _DotFadeEnd;

            float Hash(float2 p)
            {
                // Dave Hoskins' hash12. It does not repeat across the photo's grain cells.
                float3 p3 = frac(p.xyx * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                            lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), f.x), f.y);
            }

            float Grain(float2 p)
            {
                // Value noise pinches along its lattice, which reads as a fine square grid.
                // Bending the lattice with a coarser noise keeps the grain but breaks the rows.
                float2 warp = float2(ValueNoise(p * 0.31 + float2(5.2, 1.3)),
                                     ValueNoise(p * 0.31 + float2(63.7, 29.1))) - 0.5;
                return ValueNoise(p + warp * 3);
            }

            float HalftoneDistance(float2 uv)
            {
                // Upstream screen-space branch corrects Y by the image aspect ratio,
                // multiplies by Circle Density, then rotates around (0.5, 0.5).
                uv.y *= abs(_MainTex_TexelSize.x / _MainTex_TexelSize.y);
                float2 p = uv * _CircleDensity - 0.5;
                float s, c;
                sincos(_Rotation, s, c);
                p = mul(float2x2(c, -s, s, c), p) + 0.5;

                // Upstream Voronoi has Angle Offset = 0 and Cell Density = 5.
                // Every seed is consequently at lattice + (0.5, 1). This wrapped
                // distance is the same nearest seed result without a 3x3 search.
                return length(frac(p * 5 + float2(0, -0.5)) - 0.5);
            }

            float4 Frag(v2f_img input) : SV_Target
            {
                float4 source = tex2D(_MainTex, input.uv);
                float2 spread = abs(_MainTex_TexelSize.xy) * _InkSpread;
                float3 softened = source.rgb * 0.5;
                softened += (tex2D(_MainTex, input.uv + float2(spread.x, 0)).rgb
                          + tex2D(_MainTex, input.uv - float2(spread.x, 0)).rgb
                          + tex2D(_MainTex, input.uv + float2(0, spread.y)).rgb
                          + tex2D(_MainTex, input.uv - float2(0, spread.y)).rgb) * 0.125;
                float3 displayColor = softened;
                float3 paperColor = _PaperColor.rgb;
                float3 inkColor = _InkColor.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                    displayColor = LinearToGammaSpace(displayColor);
                    paperColor = LinearToGammaSpace(paperColor);
                    inkColor = LinearToGammaSpace(inkColor);
                #endif
                float brightness = saturate((dot(displayColor, float3(0.2126, 0.7152, 0.0722)) - 0.5) * _Contrast + 0.5);

                // Substitute 2*brightness-1 for upstream diffuse lighting. Its
                // Negate -> Remap(-1,1, Lit-Falloff, Lit) becomes this radius.
                float radius = _LitThreshold - _FalloffThreshold * brightness;

                // The photo and page share paper coordinates, including folds.
                float2 printUV = _PrintRect.xy + input.uv * _PrintRect.zw;
                float3 paperTexture = tex2D(_PaperTex, printUV * _PaperUV.xy + _PaperUV.zw).rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                    paperTexture = LinearToGammaSpace(paperTexture);
                #endif
                float paperTone = dot(paperTexture, float3(0.2126, 0.7152, 0.0722));
                // Grain is fixed to the photo, with no time-dependent random noise.
                float2 paperPosition = printUV * _PaperSize.xy;
                float fineGrain = Grain(paperPosition * 0.7);
                float fibers = Grain(paperPosition * float2(0.42, 2.3));
                float mottle = Grain(paperPosition / 28);

                // Ink spreading into the fibers frays each dot's edge. The noise is about an
                // eighth of a dot, fine enough to roughen edges without merging dots.
                float edgeGrain = Grain(paperPosition * 2);
                float distanceToDot = HalftoneDistance(input.uv) + (edgeGrain - 0.5) * _DotRoughness;
                float softness = max(_Softness, fwidth(distanceToDot));
                float paperMask = smoothstep(radius, radius + softness, distanceToDot);

                // Dots fade by their pitch on screen, counted in 1920-wide pixels so every
                // resolution matches. The menu keeps only grain; dots surface during the zoom.
                float dotPitch = 1920 * _DisplayScale / (_CircleDensity * 5);
                float dotsVisible = smoothstep(_DotFadeStart, _DotFadeEnd, dotPitch);
                float coverage = lerp(1 - brightness, 1 - paperMask, _DotStrength * dotsVisible);

                float missedInk = smoothstep(0.5, 0.9, fineGrain);
                coverage *= _InkDensity * (1 - _InkWear * missedInk);
                coverage = saturate(coverage + _InkWear * ((fibers - 0.5) * 0.2 + (mottle - 0.5) * 0.13));
                float paperVariation = (paperTone - 0.8) * _PaperAmount + (fibers - 0.5) * 0.018;
                float3 printColor = lerp(paperColor + paperVariation, inkColor + paperVariation * 0.5, coverage);
                #ifndef UNITY_COLORSPACE_GAMMA
                    printColor = GammaToLinearSpace(saturate(printColor));
                #endif
                return float4(lerp(source.rgb, printColor, _EffectStrength), source.a);
            }
            ENDHLSL
        }
    }
}
