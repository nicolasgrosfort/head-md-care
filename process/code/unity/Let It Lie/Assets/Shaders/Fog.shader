Shader "Custom/Fog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.7, 0.75, 0.85, 1.0)
        _FogDensity ("Fog Density", Range(0.0, 1.0)) = 0.05
        _FogStart ("Fog Start Distance", Float) = 5.0
        _FogEnd ("Fog End Distance", Float) = 80.0

        _HeightFogEnabled ("Height Fog Enabled", Float) = 1.0
        _FogHeightMin ("Fog Height Min", Float) = -2.0
        _FogHeightMax ("Fog Height Max", Float) = 4.0
        _HeightFogDensity ("Height Fog Density", Range(0.0, 1.0)) = 0.3

        _ScatteringColor ("Light Scattering Color", Color) = (1.0, 0.9, 0.7, 1.0)
        _ScatteringIntensity ("Scattering Intensity", Range(0.0, 3.0)) = 1.0
        _MieScattering ("Mie Scattering (god rays)", Range(0.0, 0.99)) = 0.7

        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 0.05
        _NoiseSpeed ("Noise Speed", Vector) = (0.01, 0.0, 0.005, 0.0)
        _NoiseIntensity ("Noise Intensity", Range(0.0, 1.0)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "VolumetricFogPass"
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float  _FogDensity;
                float  _FogStart;
                float  _FogEnd;

                float  _HeightFogEnabled;
                float  _FogHeightMin;
                float  _FogHeightMax;
                float  _HeightFogDensity;

                float4 _ScatteringColor;
                float  _ScatteringIntensity;
                float  _MieScattering;

                float4 _NoiseTexture_ST;
                float4 _NoiseSpeed;
                float  _NoiseScale;
                float  _NoiseIntensity;
            CBUFFER_END

            TEXTURE2D(_NoiseTexture);
            SAMPLER(sampler_NoiseTexture);

            float MiePhase(float cosAngle, float g)
            {
                float g2 = g * g;
                float denom = 1.0 + g2 - 2.0 * g * cosAngle;
                return (1.0 - g2) / (4.0 * PI * pow(abs(denom), 1.5));
            }

            // Varyings and Attributes are already defined by Blit.hlsl
            // Vert is also provided by Blit.hlsl as: Varyings Vert(Attributes input)

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                // Reconstruct world position from depth
                float rawDepth = SampleSceneDepth(uv);

                #if defined(UNITY_REVERSED_Z)
                    if (rawDepth < 0.0001) return float4(0, 0, 0, 0);
                #else
                    if (rawDepth > 0.9999) return float4(0, 0, 0, 0);
                #endif

                float3 worldPos = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 camPos   = _WorldSpaceCameraPos;
                float3 viewDir  = worldPos - camPos;
                float  dist     = length(viewDir);
                viewDir = viewDir / dist;

                // Depth-based fog
                float fogRange  = max(_FogEnd - _FogStart, 0.001);
                float depthFog  = saturate((dist - _FogStart) / fogRange);
                depthFog = depthFog * depthFog;

                // Height-based fog
                float heightFog = 0.0;
                if (_HeightFogEnabled > 0.5)
                {
                    float heightRange = max(_FogHeightMax - _FogHeightMin, 0.001);
                    float heightT     = 1.0 - saturate((worldPos.y - _FogHeightMin) / heightRange);
                    heightFog = heightT * _HeightFogDensity * depthFog;
                }

                // Noise
                float2 noiseUV       = worldPos.xz * _NoiseScale + _Time.y * _NoiseSpeed.xz;
                float  noise         = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, noiseUV).r;
                float  noiseDisplace = (noise - 0.5) * _NoiseIntensity;

                // Combined fog density
                float fogDensity = saturate((depthFog + heightFog) * _FogDensity * 10.0 + noiseDisplace * depthFog);

                // Main light scattering (Mie / sun halo)
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float  cosAngle = dot(-viewDir, lightDir);
                float  mie      = MiePhase(cosAngle, _MieScattering);
                mie = saturate(mie * 0.1);
                float3 scattering = _ScatteringColor.rgb * mie * _ScatteringIntensity * mainLight.color;

                // Additional lights — ray march along view ray
                float3 additionalScattering = float3(0, 0, 0);
                uint lightsCount = GetAdditionalLightsCount();
                int RAY_STEPS = 4;
                for (int s = 1; s <= RAY_STEPS; s++)
                {
                    float  t         = (float)s / (float)(RAY_STEPS + 1);
                    float3 samplePos = camPos + viewDir * dist * t;

                    for (uint i = 0u; i < lightsCount; ++i)
                    {
                        Light light   = GetAdditionalLight(i, samplePos);
                        float contrib = light.distanceAttenuation * light.shadowAttenuation;
                        additionalScattering += light.color * contrib * fogDensity;
                    }
                }
                additionalScattering /= float(RAY_STEPS);
                scattering += additionalScattering * _ScatteringIntensity;

                // Final fog color
                float3 finalFogColor = lerp(_FogColor.rgb, _FogColor.rgb + scattering, saturate(mie * 2.0));
                finalFogColor += additionalScattering * 0.5;

                float alpha = saturate(fogDensity);
                return float4(finalFogColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
