// Material único da cena da descida (M6, E1): textura com filtro "point", corte por alfa (sprites sem ordenação: o
// buffer de profundidade resolve quem está na frente), rolagem da textura pelo quanto o anão já correu e neblina
// escura pela distância. Sem luz: as cores vêm prontas da textura, como no pixel art.
Shader "MinersIdle/DescentUnlit"
{
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
        _BaseColor ("Cor", Color) = (1, 1, 1, 1)
        _ScrollAxis ("Rolagem (uv por metro corrido)", Vector) = (0, 0, 0, 0)
        _Cutoff ("Corte do alfa", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "DescentUnlit"
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _ScrollAxis;
                half _Cutoff;
            CBUFFER_END

            // Globais, escritos pelo DescentStage a cada quadro.
            float _DescentScroll;
            half4 _DescentFogColor;
            float _DescentFogStart;
            float _DescentFogEnd;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float depth : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap) + _ScrollAxis.xy * _DescentScroll;
                output.depth = -position.positionVS.z;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(color.a - _Cutoff);
                half fog = saturate((input.depth - _DescentFogStart) / max(0.001, _DescentFogEnd - _DescentFogStart));
                // A mistura é feita em sRGB, como no simulador: em espaço linear o escurecimento parece lento ao olho e o
                // fundo do corredor vira um retângulo preto.
                #if UNITY_COLORSPACE_GAMMA
                return half4(lerp(color.rgb, _DescentFogColor.rgb, fog), 1);
                #else
                half3 mixed = lerp(LinearToSRGB(color.rgb), LinearToSRGB(_DescentFogColor.rgb), fog);
                return half4(SRGBToLinear(mixed), 1);
                #endif
            }
            ENDHLSL
        }
    }
}
