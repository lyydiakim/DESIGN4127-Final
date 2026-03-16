Shader "Custom/WorldSpaceDiagonalStripes"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LineThickness ("Line Thickness", Range(0.01, 0.5)) = 0.1
        _LineSpacing ("Line Spacing", Range(0.1, 5.0)) = 1.0
        _LineColor ("Line Color", Color) = (1, 1, 1, 1)
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 0)
        _Speed ("Animation Speed", Range(-2.0, 2.0)) = 0.5
        _Angle ("Line Angle", Range(0, 360)) = 45
        _WorldScale ("World Scale Factor", Range(0.01, 10.0)) = 1.0
    }
    
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
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
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _LineThickness;
            float _LineSpacing;
            float4 _LineColor;
            float4 _BackgroundColor;
            float _Speed;
            float _Angle;
            float _WorldScale;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                
                // Calculate and pass the world position to fragment shader
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float2 rotatePosition(float2 pos, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(
                    pos.x * c - pos.y * s,
                    pos.x * s + pos.y * c
                );
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the sprite texture for alpha mask only
                fixed4 sprite = tex2D(_MainTex, i.uv);
                
                // Use world position for the pattern
                float2 worldPos = i.worldPos.xy;
                
                // Scale the world position
                worldPos *= _WorldScale;
                
                // Rotate world position based on angle
                float angleInRadians = radians(_Angle);
                float2 rotatedPos = rotatePosition(worldPos, angleInRadians);
                
                // Create diagonal pattern with animation
                float pattern = frac((rotatedPos.x + rotatedPos.y) / _LineSpacing + _Time.y * _Speed);
                
                // Create stripe effect
                float stripe = step(pattern, _LineThickness);
                
                // Final color - directly use the line color or background color
                fixed4 finalColor = lerp(_BackgroundColor, _LineColor, stripe);
                
                // Use sprite alpha as a mask
                finalColor.a *= sprite.a;
                
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}