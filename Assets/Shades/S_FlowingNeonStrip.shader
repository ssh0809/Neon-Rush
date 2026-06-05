Shader "NeonRush/FlowingNeonStrip"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.02, 0.04, 0.06, 1)
        _EmissionColor ("Emission Color", Color) = (0.1, 0.85, 1, 1)
        _EmissionPower ("Emission Power", Range(0, 8)) = 2.2
        _FlowSpeed ("Flow Speed", Range(-12, 12)) = 3.8
        _PulseDensity ("Pulse Density", Range(1, 32)) = 10
        _PulseLength ("Pulse Length", Range(0.02, 0.6)) = 0.18
        _CoreWidth ("Core Width", Range(0.02, 1)) = 0.55
        _RimPower ("Rim Power", Range(0, 4)) = 0.75
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
            Name "FlowingNeonStrip"
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
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                half3 viewDirWS : TEXCOORD1;
                half2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _EmissionPower;
                half _FlowSpeed;
                half _PulseDensity;
                half _PulseLength;
                half _CoreWidth;
                half _RimPower;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.normalWS = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half flowCoord = frac((input.uv.y - _Time.y * _FlowSpeed) * _PulseDensity);
                half pulseHead = 1.0 - smoothstep(0.0, max(0.001, _PulseLength), flowCoord);
                half pulseTail = 1.0 - smoothstep(_PulseLength, saturate(_PulseLength + 0.34), flowCoord);
                half pulse = saturate(pulseHead * 0.7 + pulseTail * 0.55);

                half core = 1.0 - smoothstep(_CoreWidth, 1.0, abs(input.uv.x - 0.5) * 2.0);
                half rim = pow(1.0 - saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS))), 2.5) * _RimPower;
                half breathing = 0.82 + sin(_Time.y * 5.0) * 0.18;
                half glowMask = saturate(core * (0.45 + pulse * 1.35) + rim) * breathing;

                half3 baseColor = _BaseColor.rgb * (0.35 + core * 0.25);
                half3 emission = _EmissionColor.rgb * glowMask * _EmissionPower;
                return half4(baseColor + emission, 1.0);
            }
            ENDHLSL
        }
    }
}
