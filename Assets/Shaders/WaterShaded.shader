Shader "Custom/LavaShader" {
    Properties{
    _Color("Color", Color) = (1,1,1,1)
    _Color2("SubColor", Color) = (1,1,1,1)
    _MainTex("Texture", 2D) = "white" {}
    _SubTex("Texture", 2D) = "white" {}
    _ScrollXSpeed("X", Range(0,10)) = 2
    _ScrollYSpeed("Y", Range(0,10)) = 3

    }
        SubShader{
        Tags{ "RenderType" = "Opaque" }
        CGPROGRAM
#pragma surface surf Lambert
    struct Input {
        float2 uv_MainTex;
        float2 uv_SubTex;
        float3 viewDir;
    };
    fixed _ScrollXSpeed;
    fixed _ScrollYSpeed;
    sampler2D _MainTex;
    sampler2D _SubTex;
    fixed4 _Color;
    fixed4 _Color2;

    void surf(Input IN, inout SurfaceOutput o) {
        fixed2 scrolledUV = IN.uv_MainTex;

        fixed xScrollValue = _ScrollXSpeed * _Time;
        fixed yScrollValue = _ScrollYSpeed * _Time;

        scrolledUV += fixed2(xScrollValue, yScrollValue);
        half4 c = tex2D(_MainTex, scrolledUV);

        o.Albedo = tex2D(_SubTex, IN.uv_SubTex).rgb * _Color2;
        o.Albedo += c.rbg * _Color;
    }
    ENDCG
    }
        Fallback "Diffuse"
}