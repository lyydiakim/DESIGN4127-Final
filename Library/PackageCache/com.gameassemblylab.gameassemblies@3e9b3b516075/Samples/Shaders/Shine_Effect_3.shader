Shader "Custom/TextureBased Sprite Shine With Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // Shine properties
        [Header(Shine Effect)]
        _ShineColor ("Shine Color", Color) = (1, 1, 1, 1)
        _ShineWidth ("Shine Width", Range(0.01, 3)) = 0.1
        _ShineScale ("Shine Scale", Range(0.1, 10)) = 1
        _ShineSpeed ("Shine Speed", Range(0.1, 10)) = 1
        _ShineIntensity ("Shine Intensity", Range(0, 1)) = 0.7
        _ShineInterval ("Shine Interval", Range(0.1, 10)) = 2
        
        // Outline properties
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.01
        
        // Required for sprite sheets
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        
        // Outline Pass
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment OutlineFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float4 _MainTex_TexelSize; // Add the missing texture size declaration
            
            fixed4 OutlineFrag(v2f IN) : SV_Target
            {
                fixed4 texColor = SampleSpriteTexture(IN.texcoord) * IN.color;
                
                // Sample texture at neighboring pixels for outline
                float2 texelSize = float2(1.0 / _MainTex_TexelSize.z, 1.0 / _MainTex_TexelSize.w);
                float2 offsetX = float2(_OutlineWidth * texelSize.x, 0);
                float2 offsetY = float2(0, _OutlineWidth * texelSize.y);
                
                // Sample in all 8 directions
                fixed leftAlpha = SampleSpriteTexture(IN.texcoord - offsetX).a;
                fixed rightAlpha = SampleSpriteTexture(IN.texcoord + offsetX).a;
                fixed topAlpha = SampleSpriteTexture(IN.texcoord + offsetY).a;
                fixed bottomAlpha = SampleSpriteTexture(IN.texcoord - offsetY).a;
                fixed topLeftAlpha = SampleSpriteTexture(IN.texcoord + offsetY - offsetX).a;
                fixed topRightAlpha = SampleSpriteTexture(IN.texcoord + offsetY + offsetX).a;
                fixed bottomLeftAlpha = SampleSpriteTexture(IN.texcoord - offsetY - offsetX).a;
                fixed bottomRightAlpha = SampleSpriteTexture(IN.texcoord - offsetY + offsetX).a;
                
                // Get maximum alpha from all samples
                fixed maxAlpha = max(max(max(leftAlpha, rightAlpha), max(topAlpha, bottomAlpha)), 
                                   max(max(topLeftAlpha, topRightAlpha), max(bottomLeftAlpha, bottomRightAlpha)));
                
                // Create outline effect (only at the edges)
                fixed outline = max(0, maxAlpha - texColor.a);
                
                // Create the outline color
                fixed4 outlineColor = _OutlineColor * outline;
                
                // Only draw outline
                return fixed4(outlineColor.rgb, outline);
            }
            ENDCG
        }
        
        // Main texture pass with shine effect
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment ShineFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            
            fixed4 _ShineColor;
            float _ShineWidth;
            float _ShineScale;
            float _ShineSpeed;
            float _ShineIntensity;
            float _ShineInterval;
            
            fixed4 ShineFrag(v2f IN) : SV_Target
            {
                // Sample the sprite texture using Unity's sprite sampler
                fixed4 texColor = SampleSpriteTexture(IN.texcoord) * IN.color;
                
                // Calculate time-based movement with intervals
                float totalTime = _Time.y * _ShineSpeed;
                float intervalProgress = fmod(totalTime, _ShineInterval);
                float normalizedProgress = intervalProgress / _ShineInterval;
                
                // Make the effect only happen during a portion of the interval
                float effectDuration = 0.2; // 20% of the interval
                float effectProgress = normalizedProgress / effectDuration;
                
                // Only show effect during the first part of the interval
                float isEffectVisible = step(normalizedProgress, effectDuration);
                
                // Convert to -1 to 1 range for positioning
                float movement = (effectProgress * 2.0) - 1.0;
                
                // Scale adjusts the width of the bands
                // Smaller scale = more bands (hatch appearance)
                float scaledCoord = (IN.texcoord.x + IN.texcoord.y) * _ShineScale;
                
                // Calculate the position for the shine effect (45-degree angle)
                float shinePos = scaledCoord - movement;
                
                // Make the effect wrap by taking the fractional part
                shinePos = frac(shinePos);
                
                // Create a smooth shine effect with controllable width
                float shine = smoothstep(0.0, _ShineWidth, shinePos) * smoothstep(_ShineWidth, 0.0, shinePos);
                
                // Apply the shine effect to the sprite only during the effect period
                // Note: We're adding the shine on top of the texture color
                fixed3 shineRGB = _ShineColor.rgb * shine * _ShineIntensity * isEffectVisible * texColor.a;
                fixed4 finalColor = fixed4(texColor.rgb + shineRGB, texColor.a);
                
                // Apply premultiplied alpha blending
                finalColor.rgb *= finalColor.a;
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}