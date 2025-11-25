Shader "Custom/CRTDistortion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
        _DistortionStrength ("Distortion Strength", Range(0, 0.5)) = 0.1
        
        _ScanLineDensity ("Scanline Density", Range(0, 1000)) = 400
        _ScanLineStrength ("Scanline Strength", Range(0, 1)) = 0.5
        _PhosphorStrength ("Phosphor Strength", Range(0, 1)) = 0.1
        _ScreenResolution ("Screen Resolution", Vector) = (1920, 1080, 0, 0)
        
        _ChromaticAberrationStrength ("Chromatic Aberration Strength", Range(0, 50)) = 10.0
        _BloomStrength ("Bloom Strength", Range(0, 1)) = 0.1
        
        _NoiseStrength ("Noise Strength", Range(0, 0.2)) = 0.05
        _FlickerStrength ("Flicker Strength", Range(0, 0.5)) = 0.08
        
        _VsyncGlitchStrength ("09. VSync Glitch Strength", Range(0, 100)) = 20.0
        _VsyncGlitchSpeed ("10. VSync Glitch Speed", Range(0, 5)) = 1.0
        _VsyncGlitchBarHeight ("11. VSync Glitch Bar Height", Range(0.01, 0.5)) = 0.1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            Name "CRTDistortion"

            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragGeometricDistortion

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            float _DistortionStrength;
            float _ChromaticAberrationStrength;

            half4 FragGeometricDistortion(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // 歪み
                float2 centeredUV = uv - 0.5;
                float dist = length(centeredUV);
                float distSquared = dist * dist;
                float distortionFactor = 1.0 - distSquared * _DistortionStrength;
                float2 distortedUV = centeredUV * distortionFactor + 0.5;

                // 色収差
                float2 direction = (dist > 1e-5) ? normalize(centeredUV) : float2(0.0, 0.0);
                float2 texelSize = _BlitTexture_TexelSize.xy;
                float2 offset = direction * (_ChromaticAberrationStrength * dist) * texelSize;

                float2 uvR = distortedUV + offset;
                float2 uvG = distortedUV;
                float2 uvB = distortedUV - offset;

                half R = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvR).r;
                half G = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvG).g;
                half B = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvB).b;

                return half4(R, G, B, 1.0);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ScanlineAndPhosphor"
            
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragScanlineAndPhosphor

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
            
            float _ScanlineDensity;
            float _ScanlineStrength;
            float _PhosphorStrength;
            float4 _ScreenResolution;
            float _BloomStrength;
            float _NoiseStrength;
            float _FlickerStrength;
            float _VsyncGlitchStrength;
            float _VsyncGlitchSpeed;
            float _VsyncGlitchBarHeight;

            float2 GetVsyncGlitchUV(float2 uv)
            {
                float glitchY = fmod(_Time.y * _VsyncGlitchSpeed, 1.0);
                float inGlitchBar = step(glitchY, uv.y) * (1.0 - step(glitchY + _VsyncGlitchBarHeight, uv.y));

                if (inGlitchBar > 0.0)
                {
                    float glitchNoise = Hash(uv.y * 100.0);
                    float glitchAmount = (glitchNoise * 2.0 - 1.0) * _VsyncGlitchStrength * _BlitTexture_TexelSize.x;

                    uv.x += glitchAmount;
                }

                return uv;
            }

            half4 FragScanlineAndPhosphor(Varyings IN) : SV_Target
            {
                float2 uv = GetVsyncGlitchUV(IN.texcoord);
                float2 texelSize = _BlitTexture_TexelSize.xy;

                // ブラー
                half4 blurredColor = 0;
                float offsets[3] = { -1.0, 0.0, 1.0 };
                [unroll] for (int x = 0 ; x < 3 ; x++)
                {
                    [unroll] for (int y = 0 ; y < 3 ; y++)
                    {
                        float2 offsetUV = uv + float2(offsets[x], offsets[y]) * texelSize;
                        blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, offsetUV);
                    }
                }
                
                blurredColor /= 9.0;

                half4 originalColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half4 baseColor = lerp(originalColor, blurredColor, _BloomStrength);
                float3 finalColor = baseColor.rgb;

                // 走査線
                float scanlineFactor = sin(uv.y * _ScanlineDensity * PI);
                scanlineFactor = saturate(scanlineFactor * scanlineFactor * 4);
                float scanlineDarkness = lerp(1.0, 1.0 - _ScanlineStrength, scanlineFactor);
                finalColor *= scanlineDarkness;

                // Phosphor
                float screenPosX = uv.x * _ScreenResolution.x;
                float pixelXMod3 = fmod(screenPosX, 3.0);
                float3 phosphorPattern = (pixelXMod3 < 1.0) ? float3(1,0,0) :
                                         (pixelXMod3 < 2.0) ? float3(0,1,0) :
                                                              float3(0,0,1);
                float3 maskedColor = finalColor * phosphorPattern;
                finalColor = lerp(finalColor, maskedColor, _PhosphorStrength);

                // ノイズとちらつき
                const float BLOCK_H_PX = 8.0;    // 行ブロック高さ（px）
                const float UPDATE_FPS = 60.0;   // 見かけの更新速度

                float2 pixel = uv * _ScreenResolution.xy;
                float rowIdx = floor(pixel.y / BLOCK_H_PX);

                // 量子化された2時刻間を補間（smootherstep 風に滑らか）
                float t = _Time.y * UPDATE_FPS;
                float t0 = floor(t);
                float tf = smoothstep(0.0, 1.0, frac(t));

                // 行ノイズ（横方向一定）を時間で補間
                float n0 = Hash(float2(rowIdx, t0));
                float n1 = Hash(float2(rowIdx, t0 + 1.0));
                float bandNoise = lerp(n0, n1, tf);
                float noiseAmount = (bandNoise * 2.0 - 1.0) * _NoiseStrength;

                // フリッカーも同様に補間
                float f0 = Hash(t0 * 2.0);
                float f1 = Hash((t0 + 1.0) * 2.0);
                float flicker = lerp(f0, f1, tf);
                float flickerAmount = (flicker * 2.0 - 1.0) * _FlickerStrength;

                finalColor += noiseAmount;
                finalColor += flickerAmount;
                finalColor = saturate(finalColor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
