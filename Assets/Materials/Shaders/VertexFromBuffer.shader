Shader "Unlit/VertexFromBuffer"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR0;
            };

            StructuredBuffer<float3> _VertexBuffer;
            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;

                // ComputeBufferから頂点位置を読み込む
                float3 pos = _VertexBuffer[v.vertexID];

                // クリップ空間へ変換
                o.pos = UnityObjectToClipPos(float4(pos, 1.0));

                o.color = _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
