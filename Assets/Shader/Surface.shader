Shader "Hidden/MediaPipe/FaceMesh/Surface"
{
    CGINCLUDE

    #include "UnityCG.cginc"

    StructuredBuffer<float4> _Vertices;

    void Vertex(uint vid : SV_VertexID,
                float2 uv : TEXCOORD0,
                out float4 outVertex : SV_Position,
                out float2 outUV : TEXCOORD0)
    {
        outVertex = UnityObjectToClipPos(_Vertices[vid]);
        outUV = uv;
    }

    float4 Fragment(float4 vertex : SV_Position,
                    float2 uv : TEXCOORD0) : SV_Target
    {
        const float repeat = 20;
        const float width = 2;

        float2 g = abs(0.5 - frac(uv * repeat));
        g = 1 - saturate(g / (fwidth(uv * repeat) * width));
        float a = max(g.x, g.y);

        return float4(a, a, a, 1);
    }

    ENDCG

    SubShader
    {
        Tags { "Queue" = "Overlay" }
        Cull Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            ENDCG
        }
    }
}
