Shader "147VR/Cue/Procedural Wood"
{
    // Procedural wood, custom-lit (URP) — avoids SurfaceData field-name drift so it compiles
    // reliably. Grain = fBm noise + lengthwise rings; varnish = Blinn-Phong + clearcoat + fresnel.
    // _UseSections=1 -> cue mode (tip/ferrule/butt/bumper by UV.y). 0 -> plain wood (table frame).
    Properties
    {
        _ShaftColor ("Shaft Wood (light)", Color) = (0.80, 0.68, 0.46, 1)
        _ButtColor  ("Butt Wood (dark)", Color)  = (0.28, 0.12, 0.06, 1)
        _GrainColor ("Grain Dark", Color)        = (0.20, 0.09, 0.04, 1)
        _FerruleColor("Ferrule (ivory)", Color)  = (0.92, 0.90, 0.82, 1)
        _TipColor   ("Tip (leather)", Color)     = (0.30, 0.50, 0.72, 1)
        _BumperColor("Bumper (rubber)", Color)   = (0.05, 0.05, 0.05, 1)
        _RingScale  ("Ring Scale (lengthwise)", Range(2, 80)) = 24
        _GrainScale ("Grain Noise Scale", Range(1, 40)) = 8
        _Distortion ("Grain Distortion", Range(0, 3)) = 0.8
        _Varnish    ("Varnish Smoothness", Range(0, 1)) = 0.85
        _ClearCoat  ("Clear Coat", Range(0, 1)) = 0.6
        _Fresnel    ("Fresnel Tint", Range(0, 1)) = 0.3
        _UseSections ("Use Cue Sections (tip/ferrule/bumper)", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShaftColor; float4 _ButtColor; float4 _GrainColor;
                float4 _FerruleColor; float4 _TipColor; float4 _BumperColor;
                float _RingScale; float _GrainScale; float _Distortion;
                float _Varnish; float _ClearCoat; float _Fresnel; float _UseSections;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float  fogCoord : TEXCOORD4;
            };

            float hash(float2 p){ p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float vnoise(float2 p){ float2 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
                float a = hash(i), b = hash(i + float2(1,0)), c = hash(i + float2(0,1)), d = hash(i + float2(1,1));
                return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y); }
            float fbm(float2 p){ float s = 0, a = 0.5; for (int i = 0; i < 5; i++){ s += a * vnoise(p); p *= 2.02; a *= 0.5; } return s; }

            Varyings vert(Attributes IN)
            {
                Varyings O;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                O.positionCS = vpi.positionCS; O.positionWS = vpi.positionWS;
                O.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                O.viewDirWS = GetWorldSpaceViewDir(vpi.positionWS);
                O.uv = IN.uv;
                O.fogCoord = ComputeFogFactor(vpi.positionCS.xy);
                return O;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float v = IN.uv.y;
                float n = fbm(IN.uv * float2(_GrainScale, _GrainScale * 0.15));
                float rings = 0.5 + 0.5 * sin((v * _RingScale + n * _Distortion) * 6.28318);
                float fine = fbm(IN.uv * float2(_GrainScale * 4.0, _GrainScale * 0.5));
                float grain = saturate(rings * 0.6 + fine * 0.4);

                float3 albedo = lerp(_ShaftColor.rgb, _ButtColor.rgb, smoothstep(0.52, 0.62, v));
                albedo = lerp(albedo, _GrainColor.rgb, grain * 0.45);
                float smooth = _Varnish;

                if (_UseSections > 0.5)
                {
                    if (v < 0.006)      { albedo = _TipColor.rgb;     smooth = 0.25; }
                    else if (v < 0.02)  { albedo = _FerruleColor.rgb; smooth = 0.9;  }
                    else if (v > 0.97)  { albedo = _BumperColor.rgb;  smooth = 0.3;  }
                }

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);
                float shin = exp2(lerp(1.0, 10.0, smooth));
                float specStrength = lerp(0.04, 0.5, smooth);

                float3 color = albedo * SampleSH(N);

                Light ml = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float3 mlc = ml.color * ml.shadowAttenuation;
                color += albedo * mlc * saturate(dot(N, ml.direction));
                float ndh = saturate(dot(N, normalize(ml.direction + V)));
                color += mlc * pow(ndh, shin) * specStrength;
                color += mlc * pow(ndh, shin * 4.0) * _ClearCoat * 0.3;

                #if defined(_ADDITIONAL_LIGHTS)
                uint cnt = GetAdditionalLightsCount();
                for (uint li = 0u; li < cnt; li++)
                {
                    Light L = GetAdditionalLight(li, IN.positionWS);
                    float3 Lc = L.color * L.distanceAttenuation * L.shadowAttenuation;
                    color += albedo * Lc * saturate(dot(N, L.direction));
                    float3 H = normalize(L.direction + V);
                    color += Lc * pow(saturate(dot(N, H)), shin) * specStrength;
                }
                #endif

                float fres = pow(1.0 - saturate(dot(N, V)), 5.0);
                color += fres * _Fresnel * 0.2 * (SampleSH(N) + ml.color);

                color = MixFog(color, IN.fogCoord);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
