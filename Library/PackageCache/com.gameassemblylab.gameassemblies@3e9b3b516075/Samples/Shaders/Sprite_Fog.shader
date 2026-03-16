Shader "Game Assemblies/2D Sprite Fog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        
        // Fog parameters
        _FogColor ("Fog Color", Color) = (1,1,1,1)
        _FogIntensity ("Fog Intensity", Range(0, 1)) = 0.5
        _FogYStart ("Fog Y Start", Float) = -5
        _FogYEnd ("Fog Y End", Float) = 5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteFogVert
            #pragma fragment SpriteFogFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            // Properties
            sampler2D _MainTex;
            fixed4 _Color;
            float4 _FogColor;
            float _FogIntensity;
            float _FogYStart;
            float _FogYEnd;
            float4 _MainTex_ST;
            fixed4 _RendererColor;
            float4 _Flip;
            sampler2D _AlphaTex;
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f_fog
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            inline float4 UnityFlipSprite(in float4 pos, in float2 flip)
            {
                return float4(pos.xy * flip, pos.z, pos.w);
            }

            v2f_fog SpriteFogVert(appdata_t IN)
            {
                v2f_fog OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityFlipSprite(IN.vertex, _Flip);
                OUT.vertex = UnityObjectToClipPos(OUT.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;

                // Store world position for fog calculation
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }
            
            // Sampling function from UnitySprites.cginc to avoid dependency
            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);

            #if ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D(_AlphaTex, uv);
                color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
            #endif

                return color;
            }

            fixed4 SpriteFogFrag(v2f_fog IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                
                // Calculate fog factor based on Y position
                float fogFactor = saturate((IN.worldPos.y - _FogYStart) / (_FogYEnd - _FogYStart));
                
                // Invert fog factor (more fog at lower Y positions)
                fogFactor = 1.0 - fogFactor;
                
                // Apply intensity multiplier
                fogFactor *= _FogIntensity;
                
                // Blend with fog color
                c.rgb = lerp(c.rgb, _FogColor.rgb, fogFactor);
                
                c.rgb *= c.a;
                
                return c;
            }
            ENDCG
        }
    }
}