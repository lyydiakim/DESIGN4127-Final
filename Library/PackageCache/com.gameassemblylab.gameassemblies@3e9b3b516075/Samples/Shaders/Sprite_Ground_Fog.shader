Shader "Game Assemblies/2D Sprite Dual Fog"
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
        
        // World Space Fog parameters (Y-axis in world space)
        [Header(World Space Fog)]
        _FogColor ("World Fog Color", Color) = (1,1,1,0.5)
        _FogIntensity ("World Fog Intensity", Range(0, 1)) = 0.5
        _FogYStart ("World Fog Y Start", Float) = -5
        _FogYEnd ("World Fog Y End", Float) = 5
        
        // Ground Fog parameters (Y-axis in local space)
        [Header(Local Ground Fog)]
        _GroundFogColor ("Ground Fog Color", Color) = (0.5,0.5,0.5,0.5)
        _GroundFogIntensity ("Ground Fog Intensity", Range(0, 1)) = 0.5
        _GroundFogYStart ("Ground Fog Y Start (Normalized)", Range(0, 1)) = 0
        _GroundFogYEnd ("Ground Fog Y End (Normalized)", Range(0, 1)) = 0.5
        [Toggle] _UseGroundFog ("Enable Ground Fog", Float) = 1
        [Toggle] _UseScreenPosition ("Use Screen Position For Local Fog", Float) = 0
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
            #pragma vertex SpriteDualFogVert
            #pragma fragment SpriteDualFogFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            // Properties
            sampler2D _MainTex;
            fixed4 _Color;
            float4 _MainTex_ST;
            fixed4 _RendererColor;
            float4 _Flip;
            sampler2D _AlphaTex;
            
            // World Space Fog Properties
            float4 _FogColor;
            float _FogIntensity;
            float _FogYStart;
            float _FogYEnd;
            
            // Ground Fog Properties
            float4 _GroundFogColor;
            float _GroundFogIntensity;
            float _GroundFogYStart;
            float _GroundFogYEnd;
            float _UseGroundFog;
            float _UseScreenPosition;
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f_dualfog
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float4 localPos : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            inline float4 UnityFlipSprite(in float4 pos, in float2 flip)
            {
                return float4(pos.xy * flip, pos.z, pos.w);
            }

            v2f_dualfog SpriteDualFogVert(appdata_t IN)
            {
                v2f_dualfog OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                // Store local position BEFORE any transformations
                OUT.localPos = IN.vertex;
                
                // Apply sprite flip
                float4 flippedVertex = UnityFlipSprite(IN.vertex, _Flip);
                
                // Store world position for world fog calculation
                OUT.worldPos = mul(unity_ObjectToWorld, flippedVertex);
                
                // Transform to clip space
                OUT.vertex = UnityObjectToClipPos(flippedVertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                
                // Store screen position for alternative local coordinate approach
                OUT.screenPos = ComputeScreenPos(OUT.vertex);

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }
            
            // Sampling function 
            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);

            #if ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D(_AlphaTex, uv);
                color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
            #endif

                return color;
            }

            fixed4 SpriteDualFogFrag(v2f_dualfog IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                
                // Calculate world-space fog factor based on world Y position
                float worldFogFactor = saturate((IN.worldPos.y - _FogYStart) / (_FogYEnd - _FogYStart));
                worldFogFactor = 1.0 - worldFogFactor; // Invert (more fog at lower Y)
                worldFogFactor *= _FogIntensity;
                
                // Apply world fog
                c.rgb = lerp(c.rgb, _FogColor.rgb, worldFogFactor);
                
                // Apply ground fog if enabled
                if (_UseGroundFog > 0.5) {
                    float groundFogFactor;
                    
                    if (_UseScreenPosition > 0.5) {
                        // Use screen-space Y position (view space)
                        // This is normalized to the sprite's screen space height regardless of sprite sheet
                        float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                        float screenYNormalized = screenUV.y;
                        groundFogFactor = smoothstep(_GroundFogYEnd, _GroundFogYStart, screenYNormalized);
                    }
                    else {
                        // Use normalized local Y position from vertex data
                        // For sprites with pivot at center, Y ranges from -0.5 to 0.5
                        // Normalize to 0-1 range (0 is bottom, 1 is top)
                        float localYNormalized = (IN.localPos.y + 0.5);
                        groundFogFactor = smoothstep(_GroundFogYEnd, _GroundFogYStart, localYNormalized);
                    }
                    
                    groundFogFactor *= _GroundFogIntensity;
                    
                    // Apply ground fog on top of world fog
                    c.rgb = lerp(c.rgb, _GroundFogColor.rgb, groundFogFactor);
                }
                
                c.rgb *= c.a;
                
                return c;
            }
            ENDCG
        }
    }
}