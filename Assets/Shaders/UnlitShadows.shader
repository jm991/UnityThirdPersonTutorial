Shader "Unlit With Shadows" {
	Properties {
		_TintColor ("Color Tint", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags {"Queue" = "Geometry" "RenderType" = "Opaque"}

		Pass {
			Tags {"LightMode" = "ForwardBase"}
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma fragmentoption ARB_fog_exp2
				#pragma fragmentoption ARB_precision_hint_fastest
				
				#include "UnityCG.cginc"
				#include "AutoLight.cginc"
				
				struct v2f
				{
					float4	pos			: SV_POSITION;
					float2	uv			: TEXCOORD0;
					LIGHTING_COORDS(1,2)
				};

				float4 _MainTex_ST;
				fixed4 _TintColor;

				v2f vert (appdata_tan v)
				{
					v2f o;

					o.pos = mul( UNITY_MATRIX_MVP, v.vertex);
					o.uv = TRANSFORM_TEX (v.texcoord, _MainTex).xy;
					TRANSFER_VERTEX_TO_FRAGMENT(o);
					return o;
				}

				sampler2D _MainTex;

				fixed4 frag(v2f i) : COLOR
				{
					fixed3 tint = _TintColor.rgb;
					fixed atten = LIGHT_ATTENUATION(i);	// Light attenuation + shadows.
					////fixed atten = SHADOW_ATTENUATION(i); // Shadows ONLY.
					return tex2D(_MainTex, i.uv) * atten * tint.r; 
				}
			ENDCG
		}

		Pass {
			Tags {"LightMode" = "ForwardAdd"}
			Blend One One
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdadd_fullshadows
				#pragma fragmentoption ARB_fog_exp2
				#pragma fragmentoption ARB_precision_hint_fastest
				
				#include "UnityCG.cginc"
				#include "AutoLight.cginc"
				
				struct v2f
				{
					float4	pos			: SV_POSITION;
					float2	uv			: TEXCOORD0;
					LIGHTING_COORDS(1,2)
				};

				float4 _MainTex_ST;

				v2f vert (appdata_tan v)
				{
					v2f o;
					
					o.pos = mul( UNITY_MATRIX_MVP, v.vertex);
					o.uv = TRANSFORM_TEX (v.texcoord, _MainTex).xy;
					TRANSFER_VERTEX_TO_FRAGMENT(o);
					return o;
				}

				sampler2D _MainTex;

				fixed4 frag(v2f i) : COLOR
				{
					fixed atten = LIGHT_ATTENUATION(i);	// Light attenuation + shadows.
					//fixed atten = SHADOW_ATTENUATION(i); // Shadows ONLY.
					return tex2D(_MainTex, i.uv) * atten;
				}
			ENDCG
		}
	}
	FallBack "VertexLit"
}