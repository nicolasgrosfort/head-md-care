Shader "Custom/RippleFullScreen"
{
    Properties
    {
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Strength ("Strength", Float) = 0.02
        _Frequency ("Frequency", Float) = 12.0
        _Time2 ("Time", Float) = 0.0
        _WaveFront ("Wave Front", Float) = 0.0
        _WaveWidth ("Wave Width", Float) = 0.15
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "ForceFieldPass"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _Center;
            float _Strength;
            float _Frequency;
            float _Time2;
            float _WaveFront;
            float _WaveWidth;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                // Distance depuis le centre du clic
                float2 delta = uv - _Center.xy;
                float dist = length(delta);

                float globalFalloff = 1.0 - smoothstep(0.0, 0.5f, dist);

                float envelope = exp(-pow(dist - _WaveFront, 2.0) / (_WaveWidth * _WaveWidth));
                float wave = sin(dist * _Frequency - _Time2) * envelope * globalFalloff * _Strength;

                // Direction : pousse vers l'extérieur depuis le centre
                float2 dir = dist > 0.001 ? normalize(delta) : float2(0, 1);

                // Compression/étirement sur l'axe Y pour simuler la profondeur Z
                float depthFactor = 1.0 + wave * 0.2; 
                uv += dir * wave;
                uv.y = _Center.y + (uv.y - _Center.y) * depthFactor;

                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }
            ENDHLSL
        }
    }
}