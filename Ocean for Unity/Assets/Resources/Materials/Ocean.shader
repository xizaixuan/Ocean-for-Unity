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
			#pragma target 5.0

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
				float2 uv : TEXCOORD0;
			};

			sampler2D ReflectTex;
			sampler2D RefractTex;
			sampler2D DisplacementTex;

			float4x4 Interpolation;

			float2 DisplacementTex_Offset;
			float Displacement;

			static const float DMAP_SIZE = 128.0f;
			static const float DMAP_DX = 1.0f / DMAP_SIZE;

			float DoDispMapping(float2 uv)
			{
				float2 texelpos = DMAP_SIZE * uv;
				float2 lerps = frac(texelpos);

				float dmap[4];
				dmap[0] = tex2Dlod(DisplacementTex, float4(uv, 0.0f, 0.0f)).r;
				dmap[1] = tex2Dlod(DisplacementTex, float4(uv, 0.0f, 0.0f) + float4(DMAP_DX, 0.0f, 0.0f, 0.0f)).r;
				dmap[2] = tex2Dlod(DisplacementTex, float4(uv, 0.0f, 0.0f) + float4(0.0f, DMAP_DX, 0.0f, 0.0f)).r;
				dmap[3] = tex2Dlod(DisplacementTex, float4(uv, 0.0f, 0.0f) + float4(DMAP_DX, DMAP_DX, 0.0f, 0.0f)).r;

				float h = lerp(lerp(dmap[0], dmap[1], lerps.x), lerp(dmap[2], dmap[3], lerps.x), lerps.y);
				return h;
			}

			v2f vert (appdata v)
			{
				v2f o;

				o.uv = v.uv;
				float4 p = lerp(lerp(Interpolation[0], Interpolation[1], v.uv.x), lerp(Interpolation[3], Interpolation[2], v.uv.x), v.uv.y);
				float4 vertex = p / p.w;
				vertex.y = 0;
				float4 screenPos = UnityObjectToClipPos(vertex);
				o.screenUV = ComputeScreenPos(screenPos);

				float h = DoDispMapping(v.uv + DisplacementTex_Offset);
				vertex.y += h*Displacement - Displacement;

				o.vertex = UnityObjectToClipPos(vertex);
				
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.screenUV.xy / i.screenUV.w;
				float4 reflectColor = tex2D(ReflectTex, float2(1 - uv.x, uv.y));
				float4 refractColor = tex2D(RefractTex, float2(0 + uv.x, uv.y));

				float3 blerpColor = reflectColor*0.75 + refractColor*0.25;

				fixed4 finalColor = float4(blerpColor.xyz, 1.0);
				return finalColor;
			}
			ENDCG
		}
	}
}
