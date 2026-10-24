Shader "Hidden/Coal Echoes/Dormitory Texture Blend"
{
    Properties
    {
        _MainTex ("Aged", 2D) = "white" {}
        _CleanTex ("Clean", 2D) = "white" {}
        _Progress ("Progress", Range(0, 1)) = 0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            sampler2D _CleanTex;
            float _Progress;

            float4 frag(v2f_img input) : SV_Target
            {
                return lerp(tex2D(_CleanTex, input.uv), tex2D(_MainTex, input.uv), _Progress);
            }
            ENDHLSL
        }
    }
}
