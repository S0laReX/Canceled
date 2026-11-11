Shader "FUNADOS/Toon Starter URP"
{
    Properties
    {
        _BaseMap("Color texture", 2D) = "white" {}
        _BaseColor("Base color", Color) = (0.95,0.33,0.65,1)
        _ShadeColor("Shade tint", Color) = (0.35,0.24,0.48,1)
        _LightDirection("Art light direction", Vector) = (0.4,0.8,0.3,0)
        _RimColor("Rim color", Color) = (0.5,0.8,1,1)
        _RimStrength("Rim strength", Range(0,0.5)) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadeColor;
                float4 _LightDirection;
                half4 _RimColor;
                half _RimStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; float2 uv:TEXCOORD2; half fog:TEXCOORD3; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                half3 n = normalize(i.normalWS);
                half lighting = dot(n, SafeNormalize(_LightDirection.xyz)) * 0.5h + 0.5h;
                half band = lighting < 0.35h ? 0.0h : (lighting < 0.7h ? 0.55h : 1.0h);
                half3 textureColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                half3 color = textureColor * _BaseColor.rgb * lerp(_ShadeColor.rgb, half3(1,1,1), band);
                half rim = pow(1.0h - saturate(dot(n, GetWorldSpaceNormalizeViewDir(i.positionWS))), 3.0h);
                color += rim * _RimStrength * _RimColor.rgb;
                return half4(MixFog(color, i.fog), 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
