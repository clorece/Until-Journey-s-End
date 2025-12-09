// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
// https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/Sprites-Diffuse.shader

// Based on the tutorial series: Unity 2.5D Tutorials by Allen Devs 
// https://www.youtube.com/watch?v=flu2PNRUAso&list=PLDPG4I84qtXoVdVxS4E_O6txSCE_f_cLh

// might not even need this since urp does a good job applying material shaders to sprite anyways
// could use this for projecting shadows when a player sprite holds a torch or etc

Shader "Custom/SpriteShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // Controls at what alpha level the shadow disappears (0.0 to 1.0)
        _Cutoff ("Shadow Alpha Cutoff", Range(0,1)) = 0.5
        
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting On
        ZWrite Off
        Blend One OneMinusSrcAlpha

        CGPROGRAM
        // 'addshadow' = Generate a shadow pass based on geometry
        // 'alphatest:_Cutoff' = Use the _Cutoff property to shape that shadow
        #pragma surface surf Lambert vertex:vert nofog nolightmap nodynlightmap keepalpha noinstancing addshadow alphatest:_Cutoff
        #pragma multi_compile_local _ PIXELSNAP_ON
        #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
        #include "UnitySprites.cginc"

        struct Input
        {
            float2 uv_MainTex;
            fixed4 color;
        };

        void vert (inout appdata_full v, out Input o)
        {
            v.vertex = UnityFlipSprite(v.vertex, _Flip);

            #if defined(PIXELSNAP_ON)
            v.vertex = UnityPixelSnap (v.vertex);
            #endif

            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color * _Color * _RendererColor;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = SampleSpriteTexture (IN.uv_MainTex) * IN.color;
            o.Albedo = c.rgb * c.a;
            o.Alpha = c.a;
        }
        ENDCG
    }

    Fallback "Transparent/Cutout/VertexLit"
}