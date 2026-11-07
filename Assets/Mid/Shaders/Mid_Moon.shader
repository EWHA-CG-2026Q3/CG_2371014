// 달 셰이더: 래스터라이저로 그린 달 표면 텍스처를 읽고, 가장자리로 갈수록 조금 어둡게 (limb darkening)
// 장면의 안개(지수 안개)를 받지 않음 → 먼 하늘에 있어도 묻히지 않고 또렷하게 보임
// vert: 정점 위치를 화면 좌표로, normal과 카메라 방향을 frag로 넘김
// frag: 텍스처 색 × 밝기, 밝기 = 0.65 + 0.35 × (N · V)  (정면은 밝고 가장자리는 어두움)
Shader "CG/Mid_Moon"
{
    Properties
    {
        _MainTex ("Surface", 2D) = "white" {}
        _Color ("Color", Color) = (1, 0.97, 0.88, 1)
        _Brightness ("Brightness", Float) = 1.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Brightness;
            CBUFFER_END

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 viewWS : TEXCOORD2; };

            v2f vert (appdata v)
            {
                v2f o;
                float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
                o.pos = TransformWorldToHClip(positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normalWS = TransformObjectToWorldNormal(v.normal);
                o.viewWS = GetWorldSpaceViewDir(positionWS);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewWS);
                half facing = saturate(dot(n, v));
                half3 surface = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
                half3 color = surface * _Color.rgb * _Brightness * (0.65 + 0.35 * facing);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
