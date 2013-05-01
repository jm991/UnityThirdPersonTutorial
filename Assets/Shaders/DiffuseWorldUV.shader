Shader "Debug/DiffuseWorldUV" 
{
	Properties 
	{
		_Wall ("Wall", 2D) = "white" {}
		_Floor ("Floor", 2D) = "white" {}
		_Frequency ("Frequency", float) = 1
	}
	SubShader 
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _Wall;
		sampler2D _Floor;
		float _Frequency;
		
		struct Input 
		{
			float2 uv_Wall;
			float3 worldPos;
			float3 worldNormal;
		};

		void surf (Input IN, inout SurfaceOutput o) 
		{
			// Use abs() for bottom facing polygons
			float2 UV;
			
  			if (IN.worldNormal.y > 0.5 || IN.worldNormal.y < -0.5) 
  			{
  				UV = IN.worldPos.xz; // top
			}
  			else if (abs(IN.worldNormal.x) > 0.5) 
  			{
  				UV = IN.worldPos.yz; // side
			}
  			else 
  			{
  				UV = IN.worldPos.xy; // front
			}
		
			half4 c = float4(1, 1, 1, 1); 
			half4 wall = tex2D(_Wall,  UV * _Frequency);
			half4 floor = tex2D(_Floor, UV * _Frequency);
			float s = step(IN.worldNormal.y, 0.5);
			c = (wall * (s)) + (floor * 0.5 * (1 - s));
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	} 
	
	FallBack "Diffuse"
}