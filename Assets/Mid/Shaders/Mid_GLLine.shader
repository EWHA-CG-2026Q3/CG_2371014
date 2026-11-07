// S06_OpenGL_Shader를 복제해 별자리용으로 고침
// GL.Color로 넘긴 정점 색을 그대로 출력하고, 더하기 블렌딩(Blend One One)으로 밤하늘 위에서 빛나게 함
// 색이 0에 가까울수록 보이지 않으므로 GL.Color의 밝기만으로 별이 켜지고 꺼지는 것을 표현할 수 있음
Shader "CG/Mid_GLLine"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend One One
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.color = v.color;
                return o;
            }

            half4 frag (v2f i) : SV_Target { return half4(i.color.rgb, 1); }
            ENDHLSL
        }
    }
}
