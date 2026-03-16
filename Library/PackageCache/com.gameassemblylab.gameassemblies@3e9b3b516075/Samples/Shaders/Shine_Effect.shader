Shader "Custom/2D Sprite Shine Effect"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _ShineColor ("Shine Color", Color) = (1, 1, 1, 1)
        _ShineWidth ("Shine Width", Range(0.01, 1)) = 0.1
        _ShineSpeed ("Shine Speed", Range(0.1, 10)) = 1
        _ShineIntensity ("Shine Intensity", Range(0, 1)) = 0.7
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
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 screenPos : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _ShineColor;
            float _ShineWidth;
            float _ShineSpeed;
            float _ShineIntensity;
            
            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color = IN.color;
                OUT.screenPos = OUT.vertex.xy;
                return OUT;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                // Sample the texture
                fixed4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // Calculate the shine effect
                // Create a moving value that cycles from 0 to 2
                float movement = frac(_Time.y * _ShineSpeed);
                movement = movement * 2.0 - 1.0; // Convert to -1 to 1 range
                
                // Calculate the position for the shine effect (45-degree angle)
                float shinePos = (IN.texcoord.x + IN.texcoord.y) - movement;
                
                // Create a smooth shine effect
                float shine = smoothstep(0.0, _ShineWidth, shinePos) * smoothstep(_ShineWidth, 0.0, shinePos);
                
                // Apply the shine effect to the sprite
                fixed4 finalColor = color + _ShineColor * shine * _ShineIntensity * color.a;
                finalColor.a = color.a; // Preserve original alpha
                
                // Apply pre-multiplication for proper transparency
                finalColor.rgb *= finalColor.a;
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
