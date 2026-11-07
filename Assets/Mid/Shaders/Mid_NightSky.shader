// 밤하늘 Skybox: 바라보는 방향의 높이(y)에 따라 세 색을 섞음
//   위(천정) _TopColor ← 지평선 _HorizonColor → 아래(땅 밑) _BottomColor
// vert: 하늘 구의 정점 방향을 frag로 넘김
// frag: 방향을 정규화한 뒤 y로 색을 보간 (S17의 보간과 같은 생각, 여기서는 위아래 방향으로)
Shader "CG/Mid_NightSky"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.01, 0.015, 0.06, 1)
        _HorizonColor ("Horizon", Color) = (0.09, 0.06, 0.15, 1)
        _BottomColor ("Bottom", Color) = (0.02, 0.02, 0.035, 1)
        _HorizonSharpness ("Horizon Sharpness", Float) = 2.5
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
                half _HorizonSharpness;
            CBUFFER_END

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.dir = v.vertex.xyz;   // Skybox 구의 정점 위치 = 그쪽을 바라보는 방향
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float y = normalize(i.dir).y;   // -1(바로 아래) ~ 0(지평선) ~ 1(바로 위)
                // 지평선에서 멀어질수록 빠르게 위/아래 색으로: 1 − (1 − |y|)^sharpness
                float up = 1.0 - pow(1.0 - saturate(y), _HorizonSharpness);
                float down = 1.0 - pow(1.0 - saturate(-y), _HorizonSharpness * 3.0);
                half3 color = lerp(_HorizonColor.rgb, _TopColor.rgb, up);
                color = lerp(color, _BottomColor.rgb, down);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
