Shader "NeonRush/CyberRoadReadable"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.055, 0.062, 0.075, 1)
        [HDR] _EmissionColor ("Emission Color", Color) = (0.044, 0.2475, 0.4125, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "CyberRoadReadable"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 color = _BaseColor.rgb + _EmissionColor.rgb * _EmissionColor.a;
                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
