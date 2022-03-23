Shader "Custom/Particle unlit blended"
{
    Properties {
        _ZWrite ("ZWrite", Float) = 1
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "Queue"="Geometry"
        }

        Pass
        {
            Cull Off
            ZWrite[_ZWrite]
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ZWrite

            #include "UnityCG.cginc"

            struct appdata
            {
                float3 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }

        Pass
            {
                Name "ShadowCaster"
                Tags { "LightMode" = "ShadowCaster" }

                BlendOp Add
                Blend One Zero
                Cull Off

                CGPROGRAM
                #pragma target 2.5

                #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
                #pragma shader_feature_local_fragment _ _COLOROVERLAY_ON _COLORCOLOR_ON _COLORADDSUBDIFF_ON
                #pragma multi_compile_shadowcaster
                #pragma multi_compile_instancing
                #pragma instancing_options procedural:vertInstancingSetup

                #pragma vertex vertParticleShadowCaster
                #pragma fragment fragParticleShadowCaster

                #include "UnityStandardParticleShadow.cginc"
                ENDCG
            }
    }
}
