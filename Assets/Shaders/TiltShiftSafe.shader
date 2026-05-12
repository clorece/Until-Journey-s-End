Shader "PostProcess/TiltShiftSafe"
{
    Properties
    {
        _BlurStrength("Blur Strength", Range(0, 10)) = 3
        _FocusCenter("Focus Center (World Dist)", Float) = 10
        _FocusRange("Focus Range (Meters)", Float) = 2
        _FocusSmoothness("Focus Smoothness (Meters)", Float) = 5
        [Toggle] _DebugMode("Debug Focus Mask", Float) = 0
        [Toggle] _ShowRawDepth("Show Raw Depth", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "TiltShift"
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _BlurStrength;
                float _FocusCenter;
                float _FocusRange;
                float _FocusSmoothness;
                float _DebugMode;
                float _ShowRawDepth;
            CBUFFER_END

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.texcoord;
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                
                // Sample Depth and convert to Linear Eye Depth
                float rawDepth = SampleSceneDepth(uv);
                float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                // RAW DEPTH DEBUG: Visualize distance from camera
                // Closer objects = Darker, Farther objects = Lighter
                if (_ShowRawDepth > 0.5)
                {
                    float d = linearDepth * 0.01; // Scaled so 100m = white
                    return float4(d, d, d, 1.0);
                }

                // Calculate mask (0 = sharp, 1 = blurred)
                float dist = abs(linearDepth - _FocusCenter);
                float mask = smoothstep(_FocusRange, _FocusRange + _FocusSmoothness, dist);
                
                // If it's the skybox, force blur
                if (rawDepth <= 0.0) mask = 1.0;

                // DEBUG MODE: Visualize the mask
                // Black = Sharp Area, White = Blurred Area
                if (_DebugMode > 0.5)
                {
                    return float4(mask, mask, mask, 1.0);
                }

                if (mask <= 0.0)
                    return color;

                // Simple 9-tap blur
                float2 texelSize = 1.0 / _ScreenParams.xy;
                float4 blurColor = 0;
                float currentBlur = mask * _BlurStrength;
                
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        float2 offset = float2(x, y) * currentBlur * texelSize;
                        blurColor += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset);
                    }
                }
                
                return blurColor / 9.0;
            }
            ENDHLSL
        }
    }
}
