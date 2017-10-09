Shader "Unlit/NewUnlitShader"
{
	Properties
	{
		//_MainTex ("Texture", 2D) = "white" {}
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
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 screenUV : TEXCOORD1;
			};

			sampler2D ReflectTex;

			float4x4 Interpolation;

			v2f vert (appdata v)
			{
				v2f o;

				float4 p = lerp(lerp(Interpolation[0], Interpolation[1], v.uv.x), lerp(Interpolation[3], Interpolation[2], v.uv.x), v.uv.y);
				p = p / p.w;
				p.y = 0.0f;

				o.vertex = UnityObjectToClipPos(p);
				float4 screenPos = UnityObjectToClipPos(p);
				o.screenUV = ComputeScreenPos(screenPos);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.screenUV.xy / i.screenUV.w;
				uv.x = 1 - uv.x;
				float4 reflectColor = tex2D(ReflectTex, uv.xy);

				float4 finalColor = float4(reflectColor.xyz, 1.0);
				return finalColor;
			}
			ENDCG
		}
	}
}
