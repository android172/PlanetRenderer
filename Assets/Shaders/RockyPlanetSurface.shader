Shader "RockyPlanetSurface"
{
	Properties
	{
		_MainTex ("_MainTex (RGBA)", 2D) = "white" {}
		_Color ("_Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Pass
		{
			// indicate that our pass is the "base" pass in forward
            // rendering pipeline. It gets ambient and main directional
            // light data set up; light direction in _WorldSpaceLightPos0
            // and color in _LightColor0
            Tags {"LightMode"="ForwardBase"}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc" // for UnityObjectToWorldNormal
			#include "Lighting.cginc" // for _LightColor0

			// compile shader into multiple variants, with and without shadows
            // (we don't care about any lightmaps yet, so skip these variants)
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #include "AutoLight.cginc" // shadow helper functions and macros

			// Vertex information
			// Same one is used with compute shader & C# script
			StructuredBuffer<float3> position_buffer;
			StructuredBuffer<float3> normal_buffer;
			StructuredBuffer<float2> uv_buffer;

			struct v2f {
                float2 uv : TEXCOORD0;
                UNITY_SHADOW_COORDS(1) // put shadows data into TEXCOORD1
                fixed3 diff : COLOR0;
                fixed3 ambient : COLOR1;
                float4 pos : SV_POSITION;
            };

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			
			v2f vert (uint id : SV_VertexID) {
				v2f output;

				// Compute position
				output.pos = UnityObjectToClipPos(float4(position_buffer[id], 1));

				// Compute UV
				output.uv = uv_buffer[id];

				// Compute normal
				// (dot product between normal and light direction for
                // standard diffuse (Lambert) lighting)
				half3 world_normal = UnityObjectToWorldNormal(normal_buffer[id]);
                half nl = max(0, dot(world_normal, _WorldSpaceLightPos0.xyz));

				// Compute diffuse
                // (factor in the light color)
                output.diff = nl * _LightColor0.rgb;

				// Compute ambient
				// In addition to the diffuse lighting from the main light,
                // add illumination from ambient or light probes
                // ShadeSH9 function from UnityCG.cginc evaluates it,
                // using world space normal
                output.ambient = ShadeSH9(half4(world_normal,1));

				// Compute shadows data
                TRANSFER_SHADOW(output)

				return output;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				// Sample texture
                fixed4 col = _Color; //tex2D(_MainTex, i.uv);
				// Compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
				i._ShadowCoord.w = i._ShadowCoord.z; //  Fixes broken shadow outside unit sphere
                fixed shadow = SHADOW_ATTENUATION(i);
                // fixed shadow = tex2D(_ShadowMapTexture, i._ShadowCoord.xy / i._ShadowCoord.z);
				// Darken light's illumination with shadow, keep ambient intact
                fixed3 lighting = i.diff * shadow + i.ambient;
                // Multiply by lighting
                col.rgb *= lighting;
                return col;
			}
			ENDCG
		}

		// Shadow casting support
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
