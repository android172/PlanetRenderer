Shader "RockyPlanetSurface"
{
	Properties
	{
		_DiffuseMaps("", 2DArray)   = "" {}
		_NormalMaps("", 2DArray)    = "" {}
		_OcclusionMaps("", 2DArray) = "" {}
		_MacroVariation("", 2D) 	= "white" {}
		_NoiseMap("", 2D)		 	= "white" {}
	}
	SubShader
	{
		CGPROGRAM
		
		// Data passed to vertex function
		struct appdata {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 tangent : TANGENT;
			float4 texcoord : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			float4 color : COLOR;
			uint vertexID : SV_VertexID;
		};
		
		// Data passed to surface shader
		#define MAX_BIOME_COUNT 16
		#define BIOMES_USED 11
		struct Input {
			float3 local_coord;
			float3 local_normal;
			float4 biome_factors_1;
			float4 biome_factors_2;
			float4 biome_factors_3;
			float4 biome_factors_4;
		};
		
#ifdef SHADER_API_D3D11	
		
		#pragma surface surf Standard fullforwardshadows addshadow vertex:vert
		#pragma target 5.0
		
		#pragma debug
		
		#pragma shader_feature _NORMALMAP
		#pragma shader_feature _OCCLUSIONMAP
		
		#include "UnityCG.cginc" // for UnityObjectToWorldNormal
		#include "Includes/Biomes.cginc"
		
		// Vertex information
		// Same one is used with compute shader & C# script
		StructuredBuffer<float3> position_buffer;
		StructuredBuffer<float3> normal_buffer;
		StructuredBuffer<float2> uv_buffer;
		StructuredBuffer<uint> biome_buffer;

		// Material settings
		StructuredBuffer<float> material_settings;

		// Texture array samplers
		UNITY_DECLARE_TEX2DARRAY(_DiffuseMaps);
		UNITY_DECLARE_TEX2DARRAY(_NormalMaps);
		UNITY_DECLARE_TEX2DARRAY(_OcclusionMaps);

		// Texture sampler
		sampler2D _MacroVariation;
		sampler2D _NoiseMap;

		// Float arrays
		float  _MapScale	 	  [MAX_BIOME_COUNT];
		float  _NormalStrength	  [MAX_BIOME_COUNT];
		float  _OcclusionStrength [MAX_BIOME_COUNT];
		float  _Metallic		  [MAX_BIOME_COUNT];
		float  _Glossiness		  [MAX_BIOME_COUNT];
		float4 _Color			  [MAX_BIOME_COUNT];
		
		// void   get_surface_material_info(out SurfaceMaterialInfo smi[BIOMES_USED]);
		void set_biome_input(uint biome, inout Input data);
		void parse_biome_factors(Input data, out float biome_factors[MAX_BIOME_COUNT]);
		void apply_triplanar(
		/**/ float3 coords,
		/**/ float3 normal,
		/**/ uint 	biome,
		/**/ float 	biome_factor,
		/**/ inout SurfaceOutputStandard o
		);
		half4 sample_macro_variation(float2 uv);
		
		// Vertex function
		void vert(inout appdata v, out Input data) {
			UNITY_INITIALIZE_OUTPUT(Input, data);
			
			v.vertex.xyz = position_buffer[v.vertexID];
			v.normal = normal_buffer[v.vertexID];		
			
			data.local_coord = v.vertex.xyz;
			data.local_normal = v.normal;
			
			uint biome = get_biome(biome_buffer, v.vertexID);
			set_biome_input(biome, data);
		}
		
		// Surface shader
		void surf(Input IN, inout SurfaceOutputStandard o) {
			// Reset output
			o.Albedo     = (float4) 0.f;
			o.Alpha      = (float)  0.f;
			o.Normal     = (float3) 0.f;
			o.Occlusion  = (float)  0.f;
			o.Metallic 	 = (float)  0.f;
			o.Smoothness = (float)  0.f;

			// Get biome info
			float biome_factors[MAX_BIOME_COUNT];
			parse_biome_factors(IN, biome_factors);

			// Render biome materials for each biome
			for (uint i = 0; i < MAX_BIOME_COUNT; i++){
				float biome_f = biome_factors[i];
				if (biome_f == 0.f) continue;
				else if (i < BIOMES_USED) {
					apply_triplanar(IN.local_coord, IN.local_normal, i, biome_f, o);
				}
			}
		}

		void set_biome_input(uint biome, inout Input data) {
			switch (biome) {
				case BIOME_FLATLANDS:    data.biome_factors_1.x = 1.0f; break;
				case BIOME_MOUNTAINS:    data.biome_factors_1.y = 1.0f; break;
				case BIOME_U_MOUNTAINS:  data.biome_factors_1.z = 1.0f; break;
				case BIOME_UNDERWATER:   data.biome_factors_1.w = 1.0f; break;
				case BIOME_DEEP_WATERS:  data.biome_factors_2.x = 1.0f; break;
				case BIOME_BEACH:        data.biome_factors_2.y = 1.0f; break;
				case BIOME_CRATER:       data.biome_factors_2.z = 1.0f; break;
				case BIOME_CRATER_RIDGE: data.biome_factors_2.w = 1.0f; break;
				case BIOME_ISLAND:       data.biome_factors_3.x = 1.0f; break;
				case BIOME_PEAKS:        data.biome_factors_3.y = 1.0f; break;
				case BIOME_VALLEY:       data.biome_factors_3.z = 1.0f; break;
			}
		}

		void parse_biome_factors(Input data, out float biome_factors[MAX_BIOME_COUNT]) {
			biome_factors[0]  =  data.biome_factors_1.x;
			biome_factors[1]  =  data.biome_factors_1.y;
			biome_factors[2]  =  data.biome_factors_1.z;
			biome_factors[3]  =  data.biome_factors_1.w;
			biome_factors[4]  =  data.biome_factors_2.x;
			biome_factors[5]  =  data.biome_factors_2.y;
			biome_factors[6]  =  data.biome_factors_2.z;
			biome_factors[7]  =  data.biome_factors_2.w;
			biome_factors[8]  =  data.biome_factors_3.x;
			biome_factors[9]  =  data.biome_factors_3.y;
			biome_factors[10] =  data.biome_factors_3.z;
			biome_factors[11] =  data.biome_factors_3.w;
			biome_factors[12] =  data.biome_factors_4.x;
			biome_factors[13] =  data.biome_factors_4.y;
			biome_factors[14] =  data.biome_factors_4.z;
			biome_factors[15] =  data.biome_factors_4.w;
		}
		
		SamplerState g_samPoint
		{
			Filter = MIN_MAG_MIP_POINT;
			AddressU = Wrap;
			AddressV = Wrap;
		};

		void apply_triplanar(
		/**/ float3 coords,
		/**/ float3 normal,
		/**/ uint 	biome,
		/**/ float 	biome_factor,
		/**/ inout SurfaceOutputStandard o
		) {
			// Blending factor of triplanar mapping
			float3 bf = normalize(abs(normal));
			bf /= dot(bf, (float3)1);
			
			// Triplanar mapping
			float2 tx = coords.yz * _MapScale[biome];
			float2 ty = coords.zx * _MapScale[biome];
			float2 tz = coords.xy * _MapScale[biome];

			// Sample macro variations
			half4 mvx = sample_macro_variation(tx / 10.f);
			half4 mvy = sample_macro_variation(ty / 10.f);
			half4 mvz = sample_macro_variation(tz / 10.f);

			// Sample diffuse map
			half4 sample_x = UNITY_SAMPLE_TEX2DARRAY(_DiffuseMaps, float3(tx, biome));
			half4 sample_y = UNITY_SAMPLE_TEX2DARRAY(_DiffuseMaps, float3(ty, biome));
			half4 sample_z = UNITY_SAMPLE_TEX2DARRAY(_DiffuseMaps, float3(tz, biome));

			// Base color
			half4 cx = sample_x * mvx * bf.x;
			half4 cy = sample_y * mvy * bf.y;
			half4 cz = sample_z * mvz * bf.z;
			half4 base_color = (cx + cy + cz) * _Color[biome];
			o.Albedo += base_color.rgb * biome_factor;
			o.Alpha += base_color.a * biome_factor;

			// Normal map
			// Swizzle world normals into tangent space and apply Whiteout blend
			half3 t_normal_x = UnpackScaleNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(tx, biome)), _NormalStrength[biome]);
			half3 t_normal_y = UnpackScaleNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(ty, biome)), _NormalStrength[biome]);
			half3 t_normal_z = UnpackScaleNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(tz, biome)), _NormalStrength[biome]);
			t_normal_x = half3(t_normal_x.xy + normal.zy, abs(t_normal_x.z) * normal.x);
			t_normal_y = half3(t_normal_y.xy + normal.xz, abs(t_normal_y.z) * normal.y);
			t_normal_z = half3(t_normal_z.xy + normal.xy, abs(t_normal_z.z) * normal.z);
			
			o.Normal += normalize(
			/**/ t_normal_x.zyx * bf.x +
			/**/ t_normal_y.xzy * bf.y +
			/**/ t_normal_z.xyz * bf.z
			) * biome_factor;

			// Occlusion map
			half ox = UNITY_SAMPLE_TEX2DARRAY(_OcclusionMaps, float3(tx, biome)).g * bf.x;
			half oy = UNITY_SAMPLE_TEX2DARRAY(_OcclusionMaps, float3(ty, biome)).g * bf.y;
			half oz = UNITY_SAMPLE_TEX2DARRAY(_OcclusionMaps, float3(tz, biome)).g * bf.z;
			o.Occlusion += lerp((half4)1, ox + oy + oz, _OcclusionStrength[biome]) * biome_factor;

			// Misc parameters
			o.Metallic   += _Metallic[biome] * biome_factor;
			o.Smoothness += _Glossiness[biome] * biome_factor;
		}

		half4 sample_macro_variation(float2 uv) {
			// Scale UV's
			float2 uv_scale_1 = uv * 0.2134f;
			float2 uv_scale_2 = uv * 0.05341f;
			float2 uv_scale_3 = uv * 0.002f;

			// Sample macro variation texture
			half sample_scale_1 = tex2D(_MacroVariation, uv_scale_1).r;
			half sample_scale_2 = tex2D(_MacroVariation, uv_scale_2).r;
			half sample_scale_3 = tex2D(_MacroVariation, uv_scale_3).r;
			half sample = sample_scale_1 * sample_scale_2 * sample_scale_3;

			// Reduce contrast
			half3 sample_reduced = lerp((half3) 0.5f, (half3)1.f, sample);
			return half4(sample_reduced, 1.f);
		}
		
#else
		void vert (inout appdata v, out Input data) {}
		void surf (Input IN, inout SurfaceOutputStandard o) { o.Albedo = float4(1,0,0.5,1); }
#endif
		ENDCG
	}
	
	FallBack "Diffuse"
	CustomEditor "RockyPlanetSurfaceInspector"
}
