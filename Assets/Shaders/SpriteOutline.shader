Shader "Custom/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth("Outline Width", Range(0, 10)) = 1
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif
                return OUT;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                // Outline logic
                fixed upAlpha = tex2D(_MainTex, IN.texcoord + float2(0, _MainTex_TexelSize.y * _OutlineWidth)).a;
                fixed downAlpha = tex2D(_MainTex, IN.texcoord - float2(0, _MainTex_TexelSize.y * _OutlineWidth)).a;
                fixed rightAlpha = tex2D(_MainTex, IN.texcoord + float2(_MainTex_TexelSize.x * _OutlineWidth, 0)).a;
                fixed leftAlpha = tex2D(_MainTex, IN.texcoord - float2(_MainTex_TexelSize.x * _OutlineWidth, 0)).a;

                fixed outline = clamp(upAlpha + downAlpha + rightAlpha + leftAlpha, 0.0, 1.0);
                
                // If it's outside the sprite but inside the outline
                if (c.a == 0 && outline > 0)
                {
                    c = _OutlineColor;
                    c.a = outline; // preserve transparency blending
                }
                else
                {
                    // Force the center of the sprite to be completely transparent.
                    // This ensures the duplicate background sprite is ONLY a hollow outline.
                    c.a = 0;
                }
                
                c.rgb *= c.a; // premultiplied alpha

                return c;
            }
        ENDCG
        }
    }
}
