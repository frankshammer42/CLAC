﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/SimpleBoidParticleRender"
{
	CGINCLUDE
	#include "UnityCG.cginc"

	// パーティクルデータの構造体
	struct ParticleBoidData
	{
		float3 position;
		float3 direction;
		float noise_offset;
		float speed;
	};
	// VertexShaderからGeometryShaderに渡すデータの構造体
	struct v2g
	{
		float3 position : TEXCOORD0;
		float4 color    : COLOR;
	};
	// GeometryShaderからFragmentShaderに渡すデータの構造体
	struct g2f
	{
		float4 position : POSITION;
		float2 texcoord : TEXCOORD0;
		float4 color    : COLOR;
	};

	// パーティクルデータ
	StructuredBuffer<ParticleBoidData> _ParticleBuffer;
	// パーティクルのテクスチャ
	sampler2D _MainTex;
	float4    _MainTex_ST;
	// パーティクルサイズ
	float     _ParticleSize;
	//float4 _ParticleColor;
	// 逆ビュー行列
	float4x4  _InvViewMatrix;
	// Quadプレーンの座標
	static const float3 g_positions[4] =
	{
		float3(-1, 1, 0),
		float3( 1, 1, 0),
		float3(-1,-1, 0),
		float3( 1,-1, 0),
	};
	// QuadプレーンのUV座標
	static const float2 g_texcoords[4] =
	{
		float2(0, 0),
		float2(1, 0),
		float2(0, 1),
		float2(1, 1),
	};

	// --------------------------------------------------------------------
	// Vertex Shader
	// --------------------------------------------------------------------
	v2g vert(uint id : SV_VertexID) // SV_VertexID:頂点ごとの識別子
	{
		v2g o = (v2g)0;
		// パーティクルの位置
		o.position = _ParticleBuffer[id].position;
		// パーティクルの速度を色に反映
		//o.color    = float4(0.5 + 0.5 * normalize(_ParticleBuffer[id].velocity), 1.0);
		o.color    = float4(0.8, 0.8, 0.8, 0.8);
		//if (_ParticleBuffer[id].velocity.z > 0){
        //    o.color    = _ParticleColor; 
		//}
		return o;
	}

	// --------------------------------------------------------------------
	// Geometry Shader
	// --------------------------------------------------------------------
	[maxvertexcount(4)]
	void geom(point v2g In[1], inout TriangleStream<g2f> SpriteStream)
	{
		g2f o = (g2f)0;
		[unroll]
		for (int i = 0; i < 4; i++)
		{
			float3 position = g_positions[i] * _ParticleSize;
			//position   = mul(_InvViewMatrix, position) + In[0].position;
			position   = position + In[0].position;
			o.position = UnityObjectToClipPos(float4(position, 1.0));

			o.color    = In[0].color;
			o.texcoord = g_texcoords[i];
			// 頂点追加
			SpriteStream.Append(o);
		}
		// ストリップを閉じる
		SpriteStream.RestartStrip();
	}

	// --------------------------------------------------------------------
	// Fragment Shader
	// --------------------------------------------------------------------
	fixed4 frag(g2f i) : SV_Target
	{
		return tex2D(_MainTex, i.texcoord.xy) * i.color;
	}
	ENDCG
	
	SubShader
	{
		Tags { "RenderType"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 100

		ZWrite Off
		Blend One One
		Cull off

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
