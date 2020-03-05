Shader "Hidden/SimpleLineRender"
{
	CGINCLUDE
	#include "UnityCG.cginc"
	
	struct ConnectionData{
	    float3 start; 
	    float3 end; 
	    uint connect;
	};
	
	StructuredBuffer<ConnectionData> _ConnectionBuffer;
	float4x4 _InvViewMatrix;
	
	struct v2g {
	    float3 pos0: TEXCOORD0;
	    float3 pos1: TEXCOORD1;
	    float4 color: COLOR;
	};
	
	struct g2f {
	    float4 pos: POSITION;
	    float4 color: COLOR;
	};
	
	// --------------------------------------------------------------------
	// Vertex Shader
	// --------------------------------------------------------------------
	v2g vert(uint id: SV_VertexID){
	    v2g output = (v2g)0;
	    output.pos0 = _ConnectionBuffer[id].start; 
	    output.pos1 = _ConnectionBuffer[id].end;
	    output.color = float4(0.8, 0.8, 0.8, 0.8);
	    return output;
	}
	
	// --------------------------------------------------------------------
	// Geometry Shader
	// --------------------------------------------------------------------
	[maxvertexcount(2)]
	void geom(point v2g In[1], inout LineStream<g2f> linestream){
	    g2f o = (g2f)0; 
        o.pos = UnityObjectToClipPos(float4(In[0].pos0, 1.0));
        o.color = In[0].color; 
        linestream.Append(o); 
        o.pos = UnityObjectToClipPos(float4(In[0].pos1, 1.0));
        o.color = In[0].color; 
        linestream.Append(o); 
        linestream.RestartStrip();
	}
	
	fixed4 frag(g2f i) : SV_Target
	{
		return i.color;
	}
	
	ENDCG
	SubShader
	{
		Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 100

		ZWrite Off
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma target   5.0
			#pragma vertex   vert
			#pragma geometry geom
			#pragma fragment frag
			ENDCG
		}
	}
}