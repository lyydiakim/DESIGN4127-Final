Shader "Custom/SimpleSpriteOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 1
    }
    
    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "IgnoreProjector"="True"
        }

        // First pass - draw the outline
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _OutlineColor;
            float _OutlineWidth;
            
            v2f vert(appdata v)
            {
                v2f o;
                
                // Scale the vertices outward
                float3 vertexPos = v.vertex.xyz;
                float2 uvCenter = float2(0.5, 0.5);
                float2 uvOffset = v.uv - uvCenter;
                float2 scaleFactor = 1.0 + (_OutlineWidth * 0.01);
                
                // Scale from the center of UV space
                vertexPos.xy += normalize(uvOffset) * length(uvOffset) * (_OutlineWidth * 0.01);
                
                o.vertex = UnityObjectToClipPos(vertexPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the texture alpha to determine the shape
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Apply outline color, preserving the alpha from the texture
                fixed4 outlineColor = _OutlineColor;
                outlineColor.a *= col.a;
                
                return outlineColor;
            }
            ENDCG
        }
        
        // Second pass - draw the transparent sprite
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the texture but make it fully transparent
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgba = fixed4(0, 0, 0, 0);
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}