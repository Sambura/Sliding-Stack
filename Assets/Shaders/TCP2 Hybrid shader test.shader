Shader "Edited/Hybrid Shader"
{
	Properties
	{
	//# =======================================================
	//# Base
		[MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
		[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
		[TCP2ColorNoAlpha] _HColor ("Highlight Color", Color) = (1,1,1,1)
		[TCP2ColorNoAlpha] _SColor ("Shadow Color", Color) = (0.2,0.2,0.2,1)
	//# ========================================================

	//# Ramp Shading
		[TCP2MaterialKeywordEnumNoPrefix(Default,_,Crisp,TCP2_RAMP_CRISP,Bands,TCP2_RAMP_BANDS,Bands Crisp,TCP2_RAMP_BANDS_CRISP,Texture,TCP2_RAMPTEXT)] _RampType ("Ramp Type", Float) = 0
	//# IF_KEYWORD TCP2_RAMPTEXT
		[TCP2Gradient] _Ramp ("Ramp Texture (RGB)", 2D) = "gray" {}
		_RampScale ("Scale", Float) = 1.0
		_RampOffset ("Offset", Float) = 0.0
	//# ELSE
		[PowerSlider(0.415)] _RampThreshold ("Threshold", Range(0.01,1)) = 0.75
	//# END_IF
	//# IF_KEYWORD !TCP2_RAMPTEXT && !TCP2_RAMP_CRISP
		_RampSmoothing ("Smoothing", Range(0,1)) = 0.1
	//# END_IF
	//# IF_KEYWORD TCP2_RAMP_BANDS || TCP2_RAMP_BANDS_CRISP
		[IntRange] _RampBands ("Bands Count", Range(1,20)) = 4
	//# END_IF
	//# IF_KEYWORD TCP2_RAMP_BANDS
		_RampBandsSmoothing ("Bands Smoothing", Range(0,1)) = 0.1
	//# END_IF
	//# =========================================================

	//# Global Illumination

	//# Indirect Diffuse
		_IndirectIntensity ("Strength", Range(0,1)) = 1
	//# IF_PROPERTY _IndirectIntensity > 0
		[TCP2ToggleNoKeyword] _SingleIndirectColor ("Single Indirect Color", Float) = 0
	//# END_IF


	//# ========================================================

	//# Options
		[ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadowsOff ("Receive Shadows", Float) = 1

		[HideInInspector] _RenderingMode ("rendering mode", Float) = 0.0
		[HideInInspector] _UseMobileMode ("Mobile mode", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
		}

		HLSLINCLUDE

		#include "UnityCG.cginc"
		#include "UnityLightingCommon.cginc"
		#include "UnityStandardUtils.cginc"
		#include "Lighting.cginc"
		#include "AutoLight.cginc"

		//================================================================================================================================
		//================================================================================================================================
		
		// MAIN
		
		//================================================================================================================================
		//================================================================================================================================
		
		// Uniforms
		CBUFFER_START(UnityPerMaterial)
			half _RampSmoothing;
			half _RampThreshold;
			half _RampBands;
			half _RampBandsSmoothing;
			half _RampScale;
			half _RampOffset;

			float4 _BumpMap_ST;
			half _BumpScale;

			float4 _BaseMap_ST;

			half _Cutoff;

			half4 _BaseColor;

			float4 _EmissionMap_ST;
			half _EmissionChannel;
			half4 _EmissionColor;

			half4 _MatCapColor;
			half _MatCapMaskChannel;
			half _MatCapType;

			half4 _SColor;
			half4 _HColor;

			half _RimMin;
			half _RimMax;
			half4 _RimColor;

			half _SpecularRoughness;
			half4 _SpecularColor;
			half _SpecularMapType;
			half _SpecularToonSize;
			half _SpecularToonSmoothness;

			half _ReflectionSmoothness;
			half4 _ReflectionColor;
			half _FresnelMax;
			half _FresnelMin;
			half _ReflectionMapType;

			half _OcclusionStrength;
			half _OcclusionChannel;

			half _IndirectIntensity;
			half _SingleIndirectColor;

			half _OutlineWidth;
			half _OutlineMinWidth;
			half4 _OutlineColor;
			half _OutlineTextureLOD;
			half _DirectIntensityOutline;
			half _IndirectIntensityOutline;
		CBUFFER_END

		// Samplers
		sampler2D _BaseMap;
		sampler2D _Ramp;
		sampler2D _BumpMap;
		sampler2D _EmissionMap;
		sampler2D _OcclusionMap;
		sampler2D _ReflectionTex;
		sampler2D _SpecGlossMap;
		sampler2D _ShadowBaseMap;
		sampler2D _MatCapTex;
		sampler2D _MatCapMask;
		
		//Specular help functions (from UnityStandardBRDF.cginc)
		inline half3 TCP2_SafeNormalize(half3 inVec)
		{
			half dp3 = max(0.001f, dot(inVec, inVec));
			return inVec * rsqrt(dp3);
		}
		
		//GGX
		#define TCP2_PI 3.14159265359
		#define TCP2_INV_PI 0.31830988618f
		#define TCP2_EPSILON 1e-4f

		inline half GGX(half NdotH, half roughness)
		{
			half a2 = roughness * roughness;
			half d = (NdotH * a2 - NdotH) * NdotH + 1.0f;
			return TCP2_INV_PI * a2 / (d * d + TCP2_EPSILON);
		}
		
		float GetOcclusion(sampler2D _OcclusionMap, float2 mainTexcoord, half _OcclusionStrength, half _OcclusionChannel, half4 albedo)
		{
		#if defined(TCP2_MOBILE)
			half occlusion = tex2D(_OcclusionMap, mainTexcoord).a;
		#else
			half occlusion = 1.0;
			if (_OcclusionChannel >= 4)
			{
				occlusion = tex2D(_OcclusionMap, mainTexcoord).a;
			}
			else if (_OcclusionChannel >= 3)
			{
				occlusion = tex2D(_OcclusionMap, mainTexcoord).b;
			}
			else if (_OcclusionChannel >= 2)
			{
				occlusion = tex2D(_OcclusionMap, mainTexcoord).g;
			}
			else if (_OcclusionChannel >= 1)
			{
				occlusion = tex2D(_OcclusionMap, mainTexcoord).r;
			}
			else
			{
				occlusion = albedo.a;
			}
		#endif
			occlusion = lerp(1, occlusion, _OcclusionStrength);
			return occlusion;
		}
		
		half3 CalculateRamp(half ndlWrapped)
		{
			#if defined(TCP2_RAMPTEXT)
				half3 ramp = tex2D(_Ramp, _RampOffset + ((ndlWrapped.xx - 0.5) * _RampScale) + 0.5).rgb;
			#elif defined(TCP2_RAMP_BANDS) || defined(TCP2_RAMP_BANDS_CRISP)
				half bands = _RampBands;
		
				half rampThreshold = _RampThreshold;
				half rampSmooth = _RampSmoothing * 0.5;
				half x = smoothstep(rampThreshold - rampSmooth, rampThreshold + rampSmooth, ndlWrapped);
		
				#if defined(TCP2_RAMP_BANDS_CRISP)
					half bandsSmooth = fwidth(ndlWrapped) * (2.0 + bands);
				#else
					half bandsSmooth = _RampBandsSmoothing * 0.5;
				#endif
				half3 ramp = saturate((smoothstep(0.5 - bandsSmooth, 0.5 + bandsSmooth, frac(x * bands)) + floor(x * bands)) / bands).xxx;
			#else
				#if defined(TCP2_RAMP_CRISP)
					half rampSmooth = fwidth(ndlWrapped) * 0.5;
				#else
					half rampSmooth = _RampSmoothing * 0.5;
				#endif
				half rampThreshold = _RampThreshold;
				half3 ramp = smoothstep(rampThreshold - rampSmooth, rampThreshold + rampSmooth, ndlWrapped).xxx;
			#endif
			return ramp;
		}
		
		half CalculateSpecular(half3 lightDir, half3 viewDir, float3 normal, half specularMap)
		{
			half3 halfDir = TCP2_SafeNormalize(lightDir + viewDir);
			half nh = saturate(dot(normal, halfDir));
		
			#if defined(TCP2_SPECULAR_STYLIZED) || defined(TCP2_SPECULAR_CRISP)
				half specSize = 1 - (_SpecularToonSize * specularMap);
				nh = nh * (1.0 / (1.0 - specSize)) - (specSize / (1.0 - specSize));
		
				#if defined(TCP2_SPECULAR_CRISP)
					float specSmoothness = fwidth(nh);
				#else
					float specSmoothness = _SpecularToonSmoothness;
				#endif
		
				half spec = smoothstep(0, specSmoothness, nh);
			#else
				float specularRoughness = max(0.00001,  _SpecularRoughness) * specularMap;
				half roughness = specularRoughness * specularRoughness;
				half spec = GGX(nh, saturate(roughness));
				spec *= TCP2_PI * 0.05;
				#ifdef UNITY_COLORSPACE_GAMMA
					spec = max(0, sqrt(max(1e-4h, spec)));
					half surfaceReduction = 1.0 - 0.28 * roughness * specularRoughness;
				#else
					half surfaceReduction = 1.0 / (roughness * roughness + 1.0);
				#endif
				spec *= surfaceReduction;
			#endif
		
			return max(0, spec);
		}
		
		// Custom macros to separate shadows from attenuation
		// Based on UNITY_LIGHT_ATTENUATION macros from "AutoLight.cginc"
		
		#ifdef POINT
		#	define TCP2_LIGHT_ATTENUATION(input, worldPos) \
				unityShadowCoord3 lightCoord = mul(unity_WorldToLight, unityShadowCoord4(worldPos, 1)).xyz; \
				half shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
				half attenuation = tex2D(_LightTexture0, dot(lightCoord, lightCoord).rr).r;
		#endif
		
		#ifdef SPOT
		#	define TCP2_LIGHT_ATTENUATION(input, worldPos) \
				DECLARE_LIGHT_COORD(input, worldPos); \
				half shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
				half attenuation = (lightCoord.z > 0) * UnitySpotCookie(lightCoord) * UnitySpotAttenuate(lightCoord.xyz);
		#endif
		
		#ifdef DIRECTIONAL
		#	define TCP2_LIGHT_ATTENUATION(input, worldPos) \
				half shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
				half attenuation = 1;
		#endif
		
		#ifdef POINT_COOKIE
		#	define TCP2_LIGHT_ATTENUATION(input, worldPos) \
				DECLARE_LIGHT_COORD(input, worldPos); \
				half shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
				half attenuation = tex2D(_LightTextureB0, dot(lightCoord, lightCoord).rr).r * texCUBE(_LightTexture0, lightCoord).w;
		#endif
		
		#ifdef DIRECTIONAL_COOKIE
		#	define TCP2_LIGHT_ATTENUATION(input, worldPos) \
				DECLARE_LIGHT_COORD(input, worldPos); \
				half shadow = UNITY_SHADOW_ATTENUATION(input, worldPos); \
				half attenuation = tex2D(_LightTexture0, lightCoord).w;
		#endif
		
		// Vertex input
		struct Attributes
		{
			float4 vertex         : POSITION;
			float3 normal         : NORMAL;
			float4 tangent        : TANGENT;
			float4 texcoord0      : TEXCOORD0;
			#if defined(LIGHTMAP_ON)
				float2 texcoord1  : TEXCOORD1;
			#endif
			#if defined(DYNAMICLIGHTMAP_ON)
				float2 texcoord2 : TEXCOORD2;
			#endif
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};
		
		// Vertex output / Fragment input
		struct Varyings
		{
			float4 pos             : SV_POSITION;
			float3 normal          : NORMAL;
			float4 worldPos        : TEXCOORD0; /* w = fog coords */
			float4 texcoords       : TEXCOORD1; /* xy = main texcoords, zw = raw texcoords */
		#if defined(_NORMALMAP) || (defined(TCP2_MOBILE) && (defined(TCP2_RIM_LIGHTING) || (defined(TCP2_REFLECTIONS) && defined(TCP2_REFLECTIONS_FRESNEL)))) // if normalmap or (mobile + rim or fresnel)
			float4 tangentWS       : TEXCOORD2; /* w = ndv (mobile) */
		#endif
		#if defined(_NORMALMAP)
			float4 bitangentWS     : TEXCOORD3;
		#endif
		#if defined(TCP2_MATCAP) && !defined(_NORMALMAP)
			float4 matcap          : TEXCOORD4;
		#endif
			#if defined(DYNAMICLIGHTMAP_ON) || defined(LIGHTMAP_ON)
				float4 lmap        : TEXCOORD5;
			#endif
			UNITY_LIGHTING_COORDS(6,7)
			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
		};
		
		Varyings Vertex(Attributes input)
		{
			Varyings output = (Varyings)0;
		
			UNITY_SETUP_INSTANCE_ID(input);
			UNITY_TRANSFER_INSTANCE_ID(input, output);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
		
			// Texture Coordinates
			output.texcoords.xy = input.texcoord0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;
			output.texcoords.zw = input.texcoord0.xy;
		
			#ifdef LIGHTMAP_ON
				output.lmap.xy = input.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
				output.lmap.zw = 0;
			#endif
			#ifdef DYNAMICLIGHTMAP_ON
				output.lmap.zw = input.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
			#endif
		
			float3 positionWS = mul(unity_ObjectToWorld, input.vertex).xyz;
			float4 positionCS = UnityWorldToClipPos(positionWS);
			output.pos = positionCS;
		
			half sign = half(input.tangent.w) * half(unity_WorldTransformParams.w);
			float3 normalWS = UnityObjectToWorldNormal(input.normal);
			#if defined(_NORMALMAP)
				float3 tangentWS = UnityObjectToWorldDir(input.tangent.xyz);
				float3 bitangentWS = cross(normalWS, tangentWS) * sign;
			#endif
		
			// This Unity macro expects the vertex input to be named 'v'
			#define v input
			UNITY_TRANSFER_LIGHTING(output, input.texcoord1.xy);
		
			// world position
			output.worldPos = float4(positionWS.xyz, 0);
		
			// Compute fog factor
			UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(output, positionCS);
		
			// normal
			output.normal = normalWS;
		
			// tangent
			#if defined(_NORMALMAP) || (defined(TCP2_MOBILE) && (defined(TCP2_RIM_LIGHTING) || (defined(TCP2_REFLECTIONS) && defined(TCP2_REFLECTIONS_FRESNEL)))) // if mobile + rim or fresnel
				output.tangentWS = float4(0, 0, 0, 0);
			#endif
			#if defined(_NORMALMAP)
				output.tangentWS.xyz = tangentWS;
				output.bitangentWS.xyz = bitangentWS;
			#endif
			#if defined(TCP2_MOBILE) && (defined(TCP2_RIM_LIGHTING) || (defined(TCP2_REFLECTIONS) && defined(TCP2_REFLECTIONS_FRESNEL))) // if mobile + rim or fresnel
				// Calculate ndv in vertex shader
				half3 viewDirWS = TCP2_SafeNormalize(_WorldSpaceCameraPos.xyz - positionWS);
				output.tangentWS.w = 1 - max(0, dot(viewDirWS, normalWS));
			#endif
		
			#if defined(TCP2_MATCAP) && !defined(_NORMALMAP)
				// MatCap
				float3 worldNorm = normalize(unity_WorldToObject[0].xyz * input.normal.x + unity_WorldToObject[1].xyz * input.normal.y + unity_WorldToObject[2].xyz * input.normal.z);
				worldNorm = mul((float3x3)UNITY_MATRIX_V, worldNorm);
				float4 screenPos = ComputeScreenPos(positionCS);
				float3 perspectiveOffset = (screenPos.xyz / screenPos.w) - 0.5;
				worldNorm.xy -= (perspectiveOffset.xy * perspectiveOffset.z) * 0.5;
				output.matcap.xy = worldNorm.xy * 0.5 + 0.5;
			#endif
		
			return output;
		}
		
		// Note: calculations from the main pass are defined with UNITY_PASS_FORWARDBASE
		// However it is left out sometimes because some keywords aren't defined for the
		// Forward Add pass (e.g. TCP2_MATCAP, TCP2_REFLECTIONS, ...)

		half4 Fragment(Varyings input, half vFace : VFACE) : SV_Target
		{
			UNITY_SETUP_INSTANCE_ID(input);
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
		
			// Texture coordinates
			float2 mainTexcoord = input.texcoords.xy;
			float2 rawTexcoord = input.texcoords.zw;
		
			// Vectors
			float3 positionWS = input.worldPos.xyz;
			float3 normalWS = normalize(input.normal);
			normalWS.xyz *= (vFace < 0) ? -1.0 : 1.0;
		
			half3 viewDirWS = TCP2_SafeNormalize(_WorldSpaceCameraPos.xyz - positionWS);
			#if defined(_NORMALMAP)
				half3 tangentWS = input.tangentWS.xyz;
				half3 bitangentWS = input.bitangentWS.xyz;
				half3x3 tangentToWorldMatrix = half3x3(tangentWS.xyz, bitangentWS.xyz, normalWS.xyz);
			#endif
		
			// Lighting
		
			half3 lightDir = normalize(UnityWorldSpaceLightDir(positionWS));
			half3 lightColor = _LightColor0.rgb;
		
			TCP2_LIGHT_ATTENUATION(input, positionWS)
			#if defined(_RECEIVE_SHADOWS_OFF)
				half atten = attenuation;
			#else
				half atten = shadow * attenuation;
			#endif
		
			// Base
		
			half4 albedo = tex2D(_BaseMap, mainTexcoord).rgba;
			albedo.rgb *= _BaseColor.rgb;
			half alpha = albedo.a * _BaseColor.a;
			half3 emission = half3(0,0,0);
		
			// Normal Mapping
			#if defined(_NORMALMAP)
				half4 normalMap = tex2D(_BumpMap, rawTexcoord * _BumpMap_ST.xy + _BumpMap_ST.zw).rgba;
				half3 normalTS = UnpackScaleNormal(normalMap, _BumpScale);
				normalWS = mul(normalTS, tangentToWorldMatrix);
			#endif
		
			// Alpha Testing
			#if defined(_ALPHATEST_ON)
				clip(alpha - _Cutoff);
			#endif
		
			// Emission
			#if defined(_EMISSION)
				emission = _EmissionColor.rgb;
				#if defined(TCP2_MOBILE)
					half4 emissionMap = tex2D(_EmissionMap, rawTexcoord * _EmissionMap_ST.xy + _EmissionMap_ST.zw);
					emission *= emissionMap.rgb;
				#else
					if (_EmissionChannel < 5)
					{
						half4 emissionMap = tex2D(_EmissionMap, rawTexcoord * _EmissionMap_ST.xy + _EmissionMap_ST.zw);
						if (_EmissionChannel >= 4)		emission *= emissionMap.rgb;
						else if (_EmissionChannel >= 3)	emission *= emissionMap.a;
						else if (_EmissionChannel >= 2) emission *= emissionMap.b;
						else if (_EmissionChannel >= 1) emission *= emissionMap.g;
						else							emission *= emissionMap.r;
					}
				#endif
			#endif
		
			// MatCap
			#if defined(TCP2_MATCAP)
				#if defined(_NORMALMAP)
					half3 matcapCoordsNormal = mul((float3x3)UNITY_MATRIX_V, normalWS);
					half3 matcap = tex2D(_MatCapTex, matcapCoordsNormal.xy * 0.5 + 0.5).rgb * _MatCapColor.rgb;
				#else
					half3 matcap = tex2D(_MatCapTex, input.matcap.xy).rgb * _MatCapColor.rgb;
				#endif
				half matcapMask = 1.0;
				#if defined(TCP2_MATCAP_MASK)
					half4 matcapMaskTex = tex2D(_MatCapMask, mainTexcoord);
					#if defined(TCP2_MOBILE)
						matcapMask *= matcapMaskTex.a;
					#else
						if (_MatCapMaskChannel >= 3)
						{
							matcapMask *= matcapMaskTex.a;
						}
						else if (_MatCapMaskChannel >= 2)
						{
							matcapMask *= matcapMaskTex.b;
						}
						else if (_MatCapMaskChannel >= 1)
						{
							matcapMask *= matcapMaskTex.g;
						}
						else
						{
							matcapMask *= matcapMaskTex.r;
						}
					#endif
				#endif
		
				#if defined(TCP2_MOBILE)
					emission += matcap * matcapMask;
				#else
					if (_MatCapType >= 1)
					{
						albedo.rgb = lerp(albedo.rgb, matcap.rgb, matcapMask);
					}
					else
					{
						emission += matcap * matcapMask;
					}
				#endif
			#endif
		
			half ndl = dot(normalWS, lightDir);
			half ndlWrapped = ndl * 0.5 + 0.5;
			ndl = saturate(ndl);
		
			// Calculate ramp
			half3 ramp = CalculateRamp(ndlWrapped);
		
			// Apply attenuation
			ramp *= atten;
			#if defined(TCP2_RIM_LIGHTING)
				#if defined(TCP2_RIM_LIGHTING_LIGHTMASK)
					half3 rimMask = ramp.xxx * lightColor.rgb;
				#else
					half3 rimMask = half3(1, 1, 1);
				#endif
			#endif
		
			// Shadow Albedo
			#if defined(TCP2_SHADOW_TEXTURE)
				half4 shadowAlbedo = tex2D(_ShadowBaseMap, mainTexcoord).rgba;
				albedo = lerp(shadowAlbedo, albedo, ramp.x);
			#endif
		
			// Highlight/shadow colors
			ramp = lerp(half3(0, 0, 0), _HColor.rgb, ramp);
		
			// Output color
			half3 color = albedo.rgb * lightColor.rgb * ramp;
		
			// Occlusion
			#if defined(TCP2_OCCLUSION)
				half occlusion = GetOcclusion(_OcclusionMap, mainTexcoord, _OcclusionStrength, _OcclusionChannel, albedo);
			#else
				half occlusion = 1.0;
			#endif
		
			// Setup lighting environment (Built-In)
			#if defined(UNITY_PASS_FORWARDBASE)
				UnityGI gi;
				UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
				gi.indirect.diffuse = 0;
				gi.indirect.specular = 0;
				gi.light.color = lightColor;
				gi.light.dir = lightDir;
		
				// Call GI (lightmaps/SH/reflections) lighting function
				UnityGIInput giInput;
				UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
				giInput.light = gi.light;
				giInput.worldPos = positionWS;
				giInput.worldViewDir = viewDirWS;
				giInput.atten = atten;
				#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
					giInput.lightmapUV = input.lmap;
				#else
					giInput.lightmapUV = 0.0;
				#endif
				giInput.ambient.rgb = 0.0;
				giInput.probeHDR[0] = unity_SpecCube0_HDR;
				giInput.probeHDR[1] = unity_SpecCube1_HDR;
				#if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
					giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
				#endif
				#ifdef UNITY_SPECCUBE_BOX_PROJECTION
					giInput.boxMax[0] = unity_SpecCube0_BoxMax;
					giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
					giInput.boxMax[1] = unity_SpecCube1_BoxMax;
					giInput.boxMin[1] = unity_SpecCube1_BoxMin;
					giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
				#endif
		
				half3 shNormal = (_SingleIndirectColor > 0) ? viewDirWS : normalWS;
				#if defined(TCP2_REFLECTIONS)
					// GI: indirect diffuse & specular
					half smoothness = _ReflectionSmoothness;
					Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(smoothness, giInput.worldViewDir, normalWS, half3(0,0,0));
					gi = UnityGlobalIllumination(giInput, occlusion, shNormal, g);
				#else
					// GI: indirect diffuse only
					gi = UnityGlobalIllumination(giInput, occlusion, shNormal);
				#endif
		
				gi.light.color = _LightColor0.rgb; // remove attenuation, taken into account separately
			#endif
		
			// Apply ambient/indirect lighting
			#if defined(UNITY_PASS_FORWARDBASE)
			#if !defined(TCP2_MOBILE)
				if (_IndirectIntensity > 0)
			#endif
			{
				half3 indirectDiffuse = gi.indirect.diffuse * albedo.rgb * occlusion * _IndirectIntensity;
				color.rgb += indirectDiffuse;
			}
			#endif
		
			// Calculate N.V
			#if defined(TCP2_RIM_LIGHTING) || (defined(TCP2_REFLECTIONS) && defined(TCP2_REFLECTIONS_FRESNEL))
				#if defined(TCP2_MOBILE)
					half ndv = input.tangentWS.w;
				#else
					half ndv = 1 - max(0, dot(viewDirWS, normalWS));
				#endif
			#endif
		
			// Rim Lighting
			#if defined(TCP2_RIM_LIGHTING)
				#if defined(UNITY_PASS_FORWARDBASE) || defined(TCP2_RIM_LIGHTING_LIGHTMASK)
					half rim = smoothstep(_RimMin, _RimMax, ndv);
					emission.rgb += rimMask.rgb * rim * _RimColor.rgb;
				#endif
			#endif
		
			// Specular
			#if defined(TCP2_SPECULAR)
		
				half specularMap = 1.0;
				#if defined(TCP2_MOBILE)
					specularMap *= tex2D(_SpecGlossMap, mainTexcoord).a;
				#else
					if (_SpecularMapType >= 5)
					{
						specularMap *= tex2D(_SpecGlossMap, mainTexcoord).a;
					}
					else if (_SpecularMapType >= 4)
					{
						specularMap *= tex2D(_SpecGlossMap, mainTexcoord).b;
					}
					else if (_SpecularMapType >= 3)
					{
						specularMap *= tex2D(_SpecGlossMap, mainTexcoord).g;
					}
					else if (_SpecularMapType >= 2)
					{
						specularMap *= tex2D(_SpecGlossMap, mainTexcoord).r;
					}
					else if (_SpecularMapType >= 1)
					{
						specularMap *= albedo.a;
					}
				#endif
		
				half spec = CalculateSpecular(lightDir, viewDirWS, normalWS, specularMap);
				emission.rgb += spec * atten * ndl * lightColor.rgb * _SpecularColor.rgb;
			#endif
		
			// Environment Reflection
			#if defined(TCP2_REFLECTIONS)
				half3 reflections = half3(0, 0, 0);
		
				half reflectionRoughness = _ReflectionSmoothness;
				half reflectionMask = 1.0;
				#if defined(TCP2_MOBILE)
					reflectionRoughness *= tex2D(_ReflectionTex, mainTexcoord).a;
				#else
					if (_ReflectionMapType > 0)
					{
						if (_ReflectionMapType <= 1)
						{
							reflectionRoughness *= albedo.a;
						}
						else if (_ReflectionMapType <= 2)
						{
							reflectionRoughness *= tex2D(_ReflectionTex, mainTexcoord).r;
						}
						else if (_ReflectionMapType <= 3)
						{
							reflectionRoughness *= tex2D(_ReflectionTex, mainTexcoord).g;
						}
						else if (_ReflectionMapType <= 4)
						{
							reflectionRoughness *= tex2D(_ReflectionTex, mainTexcoord).b;
						}
						else if (_ReflectionMapType <= 5)
						{
							reflectionRoughness *= tex2D(_ReflectionTex, mainTexcoord).a;
						}
						else if (_ReflectionMapType <= 6)
						{
							reflectionMask *= albedo.a;
						}
						else if (_ReflectionMapType <= 7)
						{
							reflectionMask *= tex2D(_ReflectionTex, mainTexcoord).r;
						}
						else if (_ReflectionMapType <= 8)
						{
							reflectionMask *= tex2D(_ReflectionTex, mainTexcoord).g;
						}
						else if (_ReflectionMapType <= 9)
						{
							reflectionMask *= tex2D(_ReflectionTex, mainTexcoord).b;
						}
						else if (_ReflectionMapType <= 10)
						{
							reflectionMask *= tex2D(_ReflectionTex, mainTexcoord).a;
						}
					}
				#endif
				reflectionRoughness = 1 - reflectionRoughness;
		
				half3 indirectSpecular = gi.indirect.specular;
				half reflectionRoughness4 = max(pow(reflectionRoughness, 4), 6.103515625e-5);
				float surfaceReductionRefl = 1.0 / (reflectionRoughness4 + 1.0);
				reflections += indirectSpecular * reflectionMask * surfaceReductionRefl * _ReflectionColor.rgb;
		
				#if defined(TCP2_REFLECTIONS_FRESNEL)
					half fresnelTerm = smoothstep(_FresnelMin, _FresnelMax, ndv);
					reflections *= fresnelTerm;
				#endif
		
				emission.rgb += reflections;
			#endif
		
			// Premultiply blending
			#if defined(_ALPHAPREMULTIPLY_ON)
				color.rgb *= alpha;
			#else
				alpha = 1;
			#endif
		
			color += emission;
		
			// Fog
			UNITY_EXTRACT_FOG_FROM_WORLD_POS(input);
			UNITY_APPLY_FOG(_unity_fogCoord, color);
		
			return half4(color, alpha);
		}
		
		ENDHLSL

		Pass
		{
			Name "Main"
			Tags { "LightMode"="ForwardBase" }

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard SRP library
			// All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0

			#pragma vertex Vertex
			#pragma fragment Fragment

			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma multi_compile_fwdbase noshadowmask nodynlightmap nolightmap

			// -------------------------------------
			// Material keywords
			#pragma shader_feature _ _RECEIVE_SHADOWS_OFF

			// -------------------------------------
			// Unity keywords
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DYNAMICLIGHTMAP_ON

			//--------------------------------------
			// Toony Colors Pro 2 keywords
			#pragma shader_feature_local TCP2_MOBILE
			#pragma shader_feature_local _ TCP2_RAMPTEXT TCP2_RAMP_CRISP TCP2_RAMP_BANDS TCP2_RAMP_BANDS_CRISP

			// This is actually using an existing keyword to separate fade/transparent behaviors
			#pragma shader_feature_local _ _ALPHAPREMULTIPLY_ON

			#define UNITY_INSTANCED_SH
			#include "UnityShaderVariables.cginc"
			#include "UnityShaderUtilities.cginc"

			//Shader does not support lightmap thus we always want to fallback to SH.
			#undef UNITY_SHOULD_SAMPLE_SH
			#define UNITY_SHOULD_SAMPLE_SH (!defined(UNITY_PASS_FORWARDADD) && !defined(UNITY_PASS_PREPASSBASE) && !defined(UNITY_PASS_SHADOWCASTER) && !defined(UNITY_PASS_META))
			#include "AutoLight.cginc"

			#pragma multi_compile UNITY_PASS_FORWARDBASE

			ENDHLSL
		}

		Pass
		{
			Name "Main"
			Tags { "LightMode"="ForwardAdd" }

			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard SRP library
			// All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0

			#pragma vertex Vertex
			#pragma fragment Fragment

			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma multi_compile_fwdadd

			// -------------------------------------
			// Material keywords
			#pragma shader_feature _ _RECEIVE_SHADOWS_OFF

			//--------------------------------------
			// Toony Colors Pro 2 keywords
			#pragma shader_feature_local TCP2_MOBILE
			#pragma shader_feature_local _ TCP2_RAMPTEXT TCP2_RAMP_CRISP TCP2_RAMP_BANDS TCP2_RAMP_BANDS_CRISP

			// This is actually using an existing keyword to separate fade/transparent behaviors
			#pragma shader_feature_local _ _ALPHAPREMULTIPLY_ON

			#define UNITY_INSTANCED_SH
			#include "UnityShaderVariables.cginc"
			#include "UnityShaderUtilities.cginc"
			#include "AutoLight.cginc"

			#pragma multi_compile UNITY_PASS_FORWARDADD

			ENDHLSL
		}

		// ShadowCaster & Depth Pass
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			CGPROGRAM
			#pragma vertex vertex_shadow
			#pragma fragment fragment_shadow
			#pragma target 2.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"

			struct Varyings_Shadow
			{
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings_Shadow vertex_shadow (appdata_base v)
			{
				Varyings_Shadow o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 fragment_shadow (Varyings_Shadow i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}

	FallBack "Hidden/InternalErrorShader"
	CustomEditor "ToonyColorsPro.ShaderGenerator.MaterialInspector_Hybrid"
}