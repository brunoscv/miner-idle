// Material único da cena da descida (M6, E1): textura com filtro "point", corte por alfa (sprites sem ordenação: o
// buffer de profundidade resolve quem está na frente), rolagem da textura pelo quanto o anão já correu e neblina
// escura pela distância. Sem luz: as cores vêm prontas da textura, como no pixel art. M1b E11: a sombra de contato no
// encontro do chão com a parede (só nos materiais do chão e da parede), em degraus com pontilhado nos pixels da tela.
Shader "MinersIdle/DescentUnlit"
{
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
        _BaseColor ("Cor", Color) = (1, 1, 1, 1)
        _ScrollAxis ("Rolagem (uv por metro corrido)", Vector) = (0, 0, 0, 0)
        _Cutoff ("Corte do alfa", Range(0, 1)) = 0.5
        _EdgeShade ("Sombra do canto (meia largura; alcance m; força; eixo 0 chão / 1 parede)", Vector) = (0, 1, 0, 0)
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
                float4 _EdgeShade;
            CBUFFER_END

            // Degraus da sombra do canto: poucos, como numa paleta de pixel art.
            #define EDGE_SHADE_BANDS 4.0

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
                float2 local : TEXCOORD2;
            };

            // Bayer 4×4 (0 a 15/16) nos pixels da imagem baixa: o pontilhado fica do tamanho do pixel do jogo.
            static const float BayerMatrix[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            float Bayer4(float2 pixel)
            {
                uint2 p = uint2(pixel) % 4;
                return (BayerMatrix[p.y * 4 + p.x] + 0.5) / 16.0;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                // A rolagem entra antes da escala da textura: assim o chão e a parede andam junto com os objetos da
                // trilha (pedras, esteios). Somada depois, com a textura em escala 0,5, o chão corria o dobro.
                output.uv = (input.uv + _ScrollAxis.xy * _DescentScroll) * _BaseMap_ST.xy + _BaseMap_ST.zw;
                output.depth = -position.positionVS.z;
                output.local = input.positionOS.xy;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(color.a - _Cutoff);
                if (_EdgeShade.x > 0)
                {
                    // Distância até o canto: no chão, até a parede mais perto; na parede, a altura acima do chão.
                    float toCorner = _EdgeShade.w > 0.5 ? input.local.y : _EdgeShade.x - abs(input.local.x);
                    float t = saturate(1 - toCorner / _EdgeShade.y);
                    float level = floor(t * EDGE_SHADE_BANDS + Bayer4(input.positionCS.xy)) / EDGE_SHADE_BANDS;
                    color.rgb *= 1 - _EdgeShade.z * saturate(level);
                }
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
