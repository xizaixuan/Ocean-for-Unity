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
				float2 uv : TEXCOORD0;
				float4 screenUV : TEXCOORD1;
				float3 toEyeT   : TEXCOORD2;
				float3 lightDirT: TEXCOORD3;
				
			};

			sampler2D ReflectTex;
			sampler2D RefractTex;
			sampler2D DisplacementTex;

			SamplerState sampler_MainTex;

			float4x4 Interpolation;

			float2 gWaveDMapOffset0;
			float Displacement;

			sampler2D NormalTex;

			float3 eyePosW;
			float3 lightDirW;

			static const float DMAP_SIZE = 128;
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
				return h*4;
			}


			float4 OceanPos(float2 uv)
			{
				uv.xy = saturate(uv.xy);

				float4 p = lerp(lerp(Interpolation[0], Interpolation[1], uv.x), lerp(Interpolation[3], Interpolation[2], uv.x), uv.y);
				p = p / p.w;

				return p;
			}

			v2f vert (appdata v)
			{
				v2f o;

				float2 vTex0 = v.uv + gWaveDMapOffset0;

				float h = DoDispMapping(vTex0);

				o.uv = v.uv*8 + gWaveDMapOffset0;

				float4 pos = OceanPos(v.uv);
				pos.y = h;

				o.vertex = UnityObjectToClipPos(pos);

				o.screenUV = o.vertex;

				float s0 = tex2Dlod(DisplacementTex, float4(vTex0, 0.0f, 0.0f) + float4(-DMAP_DX, 0.0f, 0.0f, 0.0f)).r;
				float s1 = tex2Dlod(DisplacementTex, float4(vTex0, 0.0f, 0.0f) + float4(DMAP_DX, 0.0f, 0.0f, 0.0f)).r;
				float s2 = tex2Dlod(DisplacementTex, float4(vTex0, 0.0f, 0.0f) + float4(0.0f, -DMAP_DX, 0.0f, 0.0f)).r;
				float s3 = tex2Dlod(DisplacementTex, float4(vTex0, 0.0f, 0.0f) + float4(0.0f, DMAP_DX, 0.0f, 0.0f)).r;
				float3x3 TBN;
				TBN[0] = normalize(float3(1.0f, (s1 - s0) / 0.5, 0.0f));
				TBN[1] = normalize(float3(0.0f, (s3 - s2) / 0.5, -1.0f));
				TBN[2] = normalize(cross(TBN[0], TBN[1]));
				
				float3x3 toTangentSpace = transpose(TBN);
				o.toEyeT = mul(eyePosW - v.vertex.xyz, toTangentSpace);
				o.lightDirT = mul(lightDirW, toTangentSpace);

				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				i.toEyeT = normalize(i.toEyeT);
				i.lightDirT = normalize(i.lightDirT);

				float3 lightVecT = -i.lightDirT;

				float3 normalT = tex2D(NormalTex, i.uv);
				normalT = normalize(2.0f*normalT - 1.0f);

				float3 r = reflect(-lightVecT, normalT);
				float t = pow(max(dot(r, i.toEyeT), 0.0f), 160);
				float s = max(dot(lightVecT, normalT), 0.0f);
				if (s <= 0.0f)
					t = 0.0f;

				float3 spec = float3(0.8, 0.8, 0.8)*t;

				float3 uv = i.screenUV.xyz / i.screenUV.w;
				uv.xy = uv.xy*0.5 + 0.5;
				uv.xy = 1 - uv.xy;

				uv.z = 0.003 / uv.z;

				float3 reflectColor = tex2D(ReflectTex, float2(0 + uv.x, uv.y) - uv.z*normalT.xz).xyz;
				float3 refractColor = tex2D(RefractTex, float2(1 - uv.x, uv.y) - uv.z*normalT.xz).xyz;

				float3 blerpColor = reflectColor*0.65 + refractColor*0.35;
				float3 waterColor = float3(0.5, 0.5, 1)*blerpColor + spec;
				float4 finalColor = float4(blerpColor.xyz, 1.0);
				return finalColor;
			}
			ENDCG
		}
	}
}
