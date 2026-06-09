Shader "Custom/RippleFullScreen"
{
    Properties
    {
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Strength ("Strength", Float) = 0.02
        _Frequency ("Frequency", Float) = 20.0
        _Speed ("Speed", Float) = 3.0
        _Time2 ("Time", Float) = 0.0
        _Radius ("Radius", Float) = 0.0
        _RingWidth ("Ring Width", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "RipplePass"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _Center;
            float _Strength;
            float _Frequency;
            float _Speed;
            float _Time2;
            float _Radius;
            float _RingWidth;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                float2 delta = uv - _Center.xy;
                float dist = length(delta);

                // Anneau gaussien qui se propage
                float ring = exp(-pow(dist - _Radius, 2.0) / (_RingWidth * _RingWidth));

                // Ondulation sur l'anneau
                float wave = sin(dist * _Frequency - _Time2) * ring * _Strength;

                float2 dir = dist > 0.001 ? normalize(delta) : float2(0, 0);
                uv += dir * wave;

                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }
            ENDHLSL
        }
    }
}