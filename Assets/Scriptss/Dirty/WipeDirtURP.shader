Shader "Custom/WipeDirtURP"
{
    Properties
    {
        _CleanColor ("Clean Color", Color) = (1, 1, 1, 1)
        _DirtColor ("Dirt Color", Color) = (0.35, 0.3, 0.2, 1)
        _DirtScale ("Dirt Noise Scale", Float) = 8.0
        _DirtAmount ("Dirt Amount", Range(0, 1)) = 0.7
        _MaskTex ("Mask (Runtime - Do Not Assign)", 2D) = "black" {}
        _Smoothness ("Smoothness", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _CleanColor;
                float4 _DirtColor;
                float _DirtScale;
                float _DirtAmount;
                float _Smoothness;
            CBUFFER_END

            // ---- 程序化噪声 ----
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.13);
                p3 += dot(p3, p3.yzx + 3.333);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amp = 0.5;
                for (int j = 0; j < 4; j++)
                {
                    value += amp * noise(p);
                    p *= 2.0;
                    amp *= 0.5;
                }
                return value;
            }

            // ---- 顶点 ----
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            // ---- 片元 ----
            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // 程序化灰尘
                float dirtNoise = fbm(uv * _DirtScale);
                float dirtMask = smoothstep(1.0 - _DirtAmount, 1.0, dirtNoise);

                // 运行时擦拭遮罩 (0=脏 1=干净)
                float wipeMask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv).r;

                // 最终脏度 = 噪声灰尘 × (1 - 擦掉的)
                float finalDirt = dirtMask * (1.0 - wipeMask);

                // 颜色混合：脏的地方显示 DirtColor，干净的地方显示 CleanColor
                half3 color = lerp(_CleanColor.rgb, _DirtColor.rgb, finalDirt);

                // 光照
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 diffuse = mainLight.color * NdotL * mainLight.shadowAttenuation;
                half3 ambient = SampleSH(input.normalWS);
                color *= (diffuse + ambient);

                // 擦干净的地方有高光反射
                half3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                half3 halfDir = normalize(mainLight.direction + viewDir);
                float spec = pow(max(dot(input.normalWS, halfDir), 0), 32.0);
                spec *= _Smoothness * wipeMask * 0.3;
                color += mainLight.color * spec;

                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // 自定义 ShadowCaster，兼容 URP 14（避免 LerpWhiteTo 报错）
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(input.normalOS);
                float bias = max(0.01, 0.01 * (1.0 - saturate(dot(_LightDirection, normWS))));
                posWS += normWS * bias;
                output.positionCS = TransformWorldToHClip(posWS);

                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        // DepthOnly Pass（URP需要）
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
