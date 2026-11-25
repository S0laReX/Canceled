// FUNADOS background grade (GDD 2.0 §9): copies a texture with its saturation scaled toward its luminance.
// Used only through Graphics.Blit by BrawlBackgroundGrade to make in-memory copies of the stage textures; it lives in
// Resources so the build includes it without touching the project's shader list.
Shader "Hidden/FUNADOS/Desaturate"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Saturation ("Saturación conservada", Range(0, 1)) = 1
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Saturation;

            float4 Frag(v2f_img input) : SV_Target
            {
                float4 colour = tex2D(_MainTex, input.uv);
                float luminance = dot(colour.rgb, float3(0.2126, 0.7152, 0.0722));
                return float4(lerp(luminance.xxx, colour.rgb, _Saturation), colour.a);
            }
            ENDCG
        }
    }
}
