Shader "147VR/Table Surface Marking"
{
    Properties
    {
        [MainTexture] _BaseMap("Felt", 2D) = "white" {}
        [MainColor] _BaseColor("Felt Color", Color) = (1,1,1,1)
        _MarkingMap("Snooker Markings", 2D) = "black" {}
        _MarkingStrength("Marking Strength", Range(0,1)) = 1
        _Smoothness("Smoothness", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MarkingMap); SAMPLER(sampler_MarkingMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _BaseColor; float4 _MarkingMap_ST;
                float _MarkingStrength; float _Smoothness;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float2 uv1:TEXCOORD1; };
            struct Varyings { float4 positionHCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; float2 uv1:TEXCOORD3; };
            Varyings vert(Attributes IN)
            {
                Varyings OUT; VertexPositionInputs pos=GetVertexPositionInputs(IN.positionOS.xyz); VertexNormalInputs nrm=GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS=pos.positionCS; OUT.positionWS=pos.positionWS; OUT.normalWS=nrm.normalWS;
                OUT.uv=TRANSFORM_TEX(IN.uv,_BaseMap); OUT.uv1=TRANSFORM_TEX(IN.uv1,_MarkingMap); return OUT;
            }
            half4 frag(Varyings IN):SV_Target
            {
                SurfaceData s=(SurfaceData)0; half4 b=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,IN.uv)*_BaseColor; half4 m=SAMPLE_TEXTURE2D(_MarkingMap,sampler_MarkingMap,IN.uv1);
                s.albedo=lerp(b.rgb,m.rgb,saturate(m.a*_MarkingStrength)); s.alpha=1; s.metallic=0; s.smoothness=_Smoothness; s.normalTS=half3(0,0,1); s.occlusion=1; s.emission=0; s.specular=half3(0.2,0.2,0.2);
                InputData i=(InputData)0; i.positionWS=IN.positionWS; i.normalWS=NormalizeNormalPerPixel(IN.normalWS); i.viewDirectionWS=GetWorldSpaceNormalizeViewDir(IN.positionWS); i.shadowCoord=TransformWorldToShadowCoord(IN.positionWS); i.bakedGI=SampleSH(i.normalWS);
                return UniversalFragmentPBR(i,s);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
