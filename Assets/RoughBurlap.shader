
Shader "Custom/RoughBurlap"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _ThreadScale ("Thread Scale", Range(1, 200)) = 50
        _Roughness ("Roughness", Range(0,1)) = 0.6
        _NormalStrength ("Normal Strength", Range(0,2)) = 1.0
        _AO ("Ambient Occlusion", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _ThreadScale;
                float _Roughness;
                float _NormalStrength;
                float _AO;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv * _ThreadScale;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            float weavePattern(float2 uv)
            {
                float warp = smoothstep(0.45, 0.55, abs(frac(uv.x) - 0.5));
                float weft = smoothstep(0.45, 0.55, abs(frac(uv.y) - 0.5));
                float thread = saturate((warp + weft) * 0.6);
                return thread;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float thread = weavePattern(i.uv);
                float n = frac(sin(dot(i.uv, float2(12.9898,78.233))) * 43758.5453);
                float rough = lerp(_Roughness, 1.0, n * 0.2);
                float3 color = _Color.rgb * (0.8 + 0.2 * thread);
                float3 normal = normalize(i.normalWS + (thread - 0.5) * _NormalStrength.xxx);
                float3 lightDir = normalize(float3(0.3, 0.6, 0.4));
                float ndotl = saturate(dot(normal, lightDir));
                float3 litColor = color * (0.3 + 0.7 * ndotl);
                litColor = lerp(litColor * _AO, litColor, thread);
                return float4(litColor, 1);
            }
            ENDHLSL
        }
    }
}
