Shader "Hidden/AutoBattler/BattlePostProcess"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _VignetteIntensity ("Vignette Intensity", Range(0, 1)) = 0.3
        _ColorGradingStrength ("Color Grading Strength", Range(0, 1)) = 0.2
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        
        Pass
        {
            Name "Battle Post-Processing"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            float _VignetteIntensity;
            float _ColorGradingStrength;
            
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                // Sample base color
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                
                // Apply vignette effect
                float2 center = uv - 0.5;
                float vignette = 1.0 - dot(center, center) * _VignetteIntensity * 2.0;
                vignette = saturate(vignette);
                color.rgb *= vignette;
                
                // Apply subtle color grading for combat intensity
                float intensity = dot(color.rgb, float3(0.299, 0.587, 0.114));
                float3 graded = lerp(color.rgb, float3(intensity * 1.1, intensity * 0.9, intensity * 0.8), _ColorGradingStrength);
                color.rgb = graded;
                
                return color;
            }
            ENDHLSL
        }
    }
}
