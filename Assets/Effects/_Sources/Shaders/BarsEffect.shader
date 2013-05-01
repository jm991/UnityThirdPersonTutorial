Shader "Hidden/Bars Effect" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BarTex ("Base (RGB)", 2D) = "grayscaleRamp" {}
		_Coverage ("Screen coverage", float) = 1
	}

	SubShader 
	{
		Pass 
		{
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
					
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest 
			#include "UnityCG.cginc"
			
			uniform sampler2D _MainTex;
			uniform sampler2D _BarTex;
			float _Coverage;
			
			fixed4 frag (v2f_img i) : COLOR
			{
				fixed4 original = tex2D(_MainTex, i.uv);
				
				// Blend with bars
				float2 offset = float2(0, _Coverage);
				float2 flippedOffset = float2(0, _Coverage + 1);
				
				float2 flippedUVs = float2(i.uv.x, -1 * i.uv.y);
				
				float2 barUVs = (i.uv + offset);				
				float2 flippedBarUVs = (flippedUVs + flippedOffset);
				
                return original * tex2D(_BarTex, barUVs) * tex2D(_BarTex, flippedBarUVs);
			}
			
			ENDCG
		}
	}
	
	Fallback off

}