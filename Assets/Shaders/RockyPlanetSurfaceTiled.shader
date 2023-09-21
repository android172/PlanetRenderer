Shader "RockyPlanetSurfaceTiled"
{
	Properties
	{
		_MainTex ("_MainTex (RGBA)", 2D) = "white" {}
		_Color ("_Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		CGPROGRAM
		
		#pragma surface surf Standard fullforwardshadows addshadow vertex:vert
		#pragma target 5.0

		#include "UnityCG.cginc" // for UnityObjectToWorldNormal

		#ifdef SHADER_API_D3D11
		// LOD buffer
		StructuredBuffer<uint> lod_layout;
		StructuredBuffer<float3> position_buffer;
		StructuredBuffer<float3> normal_buffer;
		StructuredBuffer<float2> uv_buffer;
		#endif

		// Data passed to vertex function
		struct appdata {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 texcoord : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			uint vertexID : SV_VertexID;
			uint instanceID : SV_InstanceID;
		};

		// Data passed to surface shader
		struct Input {
			uint vertexID;
		};

		// Properties
		sampler2D _MainTex;
		float4 _Color;
		
		uniform float4x4 _ObjectToWorld;
		uniform uint num_vertices_per_tile;
		
		// Vertex function
		void vert (inout appdata v, out Input data) {

			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_OUTPUT(Input, data);

			#ifdef SHADER_API_D3D11
			// vertex buffers have holes. but lod_layout maps them
			uint num_holes = 0;
			for(uint i = 0; i <= v.instanceID; i++) {
				if(lod_layout[i] == 0) {
					num_holes++;
				}
			}
			uint tile_index = v.instanceID + num_holes;
			uint vertex_index = tile_index * num_vertices_per_tile + v.vertexID;

			// float4 worldPos = mul(_ObjectToWorld, float4(position_buffer[vertex_index], 1.0));
			// v.vertex.xyz = mul(UNITY_MATRIX_VP, worldPos).xyz;
			v.vertex.xyz = position_buffer[vertex_index];
			v.normal = normal_buffer[vertex_index];
			#endif

			// Doesn't actually work
			data.vertexID = v.vertexID;
		}

		// Surface shader
		void surf (Input IN, inout SurfaceOutputStandard o) {
			#ifdef SHADER_API_D3D11
			//float3 texture_color = tex2D(_MainTex, uv_buffer[IN.vertexID]).rgb;
			o.Albedo = _Color; // no texture color for now
			#endif
		}

		ENDCG
	}
}
