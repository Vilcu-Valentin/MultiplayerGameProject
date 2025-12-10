Shader "Custom/OutlinePostProcessBlend"
{
    Properties
    {
        _MainTex ("Base Tex", 2D) = "white" {}
        _MaskTex ("Mask", 2D) = "white" {}
        _OutlineColor ("Color", Color) = (1,1,0,1)
        _EdgeStrength ("Strength", Float) = 1.0
        _OutlineWidth ("Width (px)", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "DisableBatching"="True" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float4 _OutlineColor;
            float _EdgeStrength;
            float _OutlineWidth;
            float2 _TexelSize;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f Vert(uint id : SV_VertexID)
            {
                v2f o;
                float2 uv = float2((id << 1) & 2, id & 2);
    #ifdef UNITY_UV_STARTS_AT_TOP
                uv.y = 1 - uv.y;
    #endif
                o.uv = uv;
                o.pos = float4(uv * 2 - 1, 0, 1);
                return o;
            }

            float SampleMask(float2 sampleUV)
{
    // If outside the valid area, treat as empty (background)
    if (sampleUV.x < 0 || sampleUV.x > 1 || sampleUV.y < 0 || sampleUV.y > 1)
        return 0.0;
    return tex2D(_MaskTex, sampleUV).r;
}

            // clamp maximum width to avoid too heavy loops
            static const int MAX_RADIUS = 5;

            float4 Frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
#ifdef UNITY_UV_STARTS_AT_TOP
uv.y = 1 - uv.y;
#endif
float4 baseCol = tex2D(_MainTex, uv);

                // determine integer radius (in texels), clamp to [1, MAX_RADIUS]
                int radius = (int)ceil(_OutlineWidth); // width in texels (pixels)
                radius = clamp(radius, 1, MAX_RADIUS);
                if (radius < 1) radius = 1;

                // morphological gradient: dilate minus erode
                float dilated = 0;
                float eroded = 1;

                for (int oy = -radius; oy <= radius; oy++)
{
    for (int ox = -radius; ox <= radius; ox++)
    {
        float2 offset = float2(ox, oy) * _TexelSize;
        float sample = SampleMask(uv + offset);
        dilated = max(dilated, sample);
        eroded = min(eroded, sample);
    }
}

                float edge = saturate((dilated - eroded) * _EdgeStrength);

                // blend outline over base
                float4 outlineCol = _OutlineColor;
                float4 result = lerp(baseCol, outlineCol, edge * outlineCol.a);

                return result;
            }
            ENDHLSL
        }
    }
}
