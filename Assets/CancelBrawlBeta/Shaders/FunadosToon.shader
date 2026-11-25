// FUNADOS toon material (GDD 2.0 §11): the one toon shader and the one outline solution for the fighters.
// Lighting: main light quantised into soft bands (shadowed side tinted, never black), ambient from the probes and a thin
// rim light. Outline: inverted hull extruded in clip space, so it keeps the same on-screen width at any zoom.
// Shadow, depth and depth-normal passes come from URP Lit, so fighters still cast and receive shadows.
Shader "FUNADOS/Toon"
{
    Properties
    {
        [MainTexture] _BaseMap ("Textura", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _ShadeColor ("Tinte de sombra", Color) = (0.7, 0.66, 0.86, 1)
        _Bands ("Bandas de luz", Range(2, 5)) = 3
        _RimColor ("Luz de borde (alfa = fuerza)", Color) = (1, 1, 1, 0.3)
        _OutlineColor ("Contorno", Color) = (0.05, 0.035, 0.09, 1)
        _OutlineWidth ("Grosor del contorno (fracción de pantalla)", Range(0, 0.02)) = 0.0025
        // Read by the URP Lit passes reused below.
        [HideInInspector] _Cutoff ("Alpha cutoff", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadeColor;
            half _Bands;
            half4 _RimColor;
            half4 _OutlineColor;
            float _OutlineWidth;
            half _Cutoff;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        ENDHLSL

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 normal = normalize(input.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half lit = saturate(dot(normal, light.direction)) * light.shadowAttenuation;
                // Soft-edged bands: flat inside each band, a short blend at its border.
                half bands = max(2, _Bands);
                half scaled = lit * bands;
                half band = (floor(scaled) + smoothstep(0.42, 0.58, frac(scaled))) / bands;
                half3 diffuse = lerp(_ShadeColor.rgb, half3(1, 1, 1), band) * light.color;
                half3 ambient = SampleSH(normal) * 0.45;
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half rim = smoothstep(0.68, 0.76, 1 - saturate(dot(normal, view))) * _RimColor.a * (0.35 + 0.65 * band);
                return half4(albedo.rgb * (diffuse + ambient) + rim * _RimColor.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            // Pushed slightly back so the hull never shows through the body where the mesh has split normals.
            Offset 4, 4
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float4 position = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP, TransformObjectToWorldNormal(input.normalOS));
                float2 offset = normalCS.xy;
                float len = length(offset);
                offset = len > 1e-5 ? offset / len : float2(0, 0);
                // Clip-space units are a share of the screen: correct for aspect so the line is even on both axes.
                offset.x *= _ScreenParams.y / _ScreenParams.x;
                position.xy += offset * _OutlineWidth * 2 * position.w;
                output.positionCS = position;
                return output;
            }

            half4 frag(Varyings input) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
        UsePass "Universal Render Pipeline/Lit/DEPTHONLY"
        UsePass "Universal Render Pipeline/Lit/DEPTHNORMALS"
    }
    FallBack "Universal Render Pipeline/Lit"
}
