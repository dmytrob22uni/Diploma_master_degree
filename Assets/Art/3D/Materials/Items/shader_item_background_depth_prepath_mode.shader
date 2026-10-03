Shader "Custom/shader_item_background_depth_prepath_mode"
{
    SubShader
    {
        // "Transparent-1" ensures this renders immediately before your transparent Shader Graph
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-1" }

        Pass
        {
            ZWrite On
            ZTest LEqual
            ColorMask 0 // Writes depth, but outputs absolutely no color

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes IN) {
                Varyings OUT;
                // Calculates the 3D position for the depth buffer
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag() : SV_Target {
                return 0;
            }
            ENDHLSL
        }
    }
}
