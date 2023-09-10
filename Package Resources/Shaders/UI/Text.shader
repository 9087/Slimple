Shader "Slime/UI/Text"
{
    Properties
    {
        _MainTex            ("Texture", 2D) = "white" {}
        _MainTexWidth       ("Texture Width", Float) = 512
        _MainTexHeight      ("Texture Width", Float) = 512
        
        _Padding            ("Padding", Float) = 5

        _StencilComp        ("Stencil Comparison", Float) = 8
        _Stencil            ("Stencil ID", Float) = 0
        _StencilOp          ("Stencil Operation", Float) = 0
        _StencilWriteMask   ("Stencil Write Mask", Float) = 255
        _StencilReadMask    ("Stencil Read Mask", Float) = 255

        _CullMode           ("Cull Mode", Float) = 0
        _ColorMask          ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull [_CullMode]
        ZWrite Off
        Lighting Off
        Fog { Mode Off }
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vertex
            #pragma fragment fragment

            #include "UnityCG.cginc"

            struct Vertex
            {
                float4 position : POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 property : TEXCOORD1;
            };

            struct Pixel
            {
                float4 position : SV_POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 property : TEXCOORD1;
            };

            sampler2D _MainTex;
            float _MainTexWidth;
            float _MainTexHeight;
            float _Padding;

            Pixel vertex (Vertex v)
            {
                Pixel pixel;
                pixel.position = UnityObjectToClipPos(v.position);
                pixel.color = v.color;
                pixel.uv = v.uv / float2(_MainTexWidth, _MainTexHeight);
                pixel.property = v.property;
                return pixel;
            }

            fixed4 fragment (Pixel p) : SV_Target
            {
                const float sdf = tex2D(_MainTex, p.uv).a;
                const float2 pixelSize = float2(ddx(p.uv.y), ddy(p.uv.y)) * _MainTexWidth * .75;
                const float scale = rsqrt(dot(pixelSize, pixelSize)) * _Padding;
                const float fontWeight = p.property.x;
                const float edge = 0.5 - fontWeight;
                const float bias = 0.5 / scale * 0.5;
                return smoothstep(edge - bias, edge + bias, sdf) * p.color;
            }
            ENDCG
        }
    }
}
