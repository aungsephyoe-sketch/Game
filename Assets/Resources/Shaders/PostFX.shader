// Lightweight mobile post stack (Built-in pipeline, used by PostFX.cs via OnRenderImage):
// pass 0 bloom prefilter, 1 downsample, 2 additive upsample, 3 composite (radial blur, bloom, grade, vignette).
Shader "Hashira/PostFX"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _BloomTex;
    half _Threshold;
    half _Knee;
    half _BloomIntensity;
    half _Saturation;
    half _Contrast;
    half _Vignette;
    half _RadialBlur;
    half4 _Tint;

    half3 Sample4(float2 uv, float d)
    {
        float4 o = _MainTex_TexelSize.xyxy * float4(-d, -d, d, d);
        half3 s = tex2D(_MainTex, uv + o.xy).rgb + tex2D(_MainTex, uv + o.zy).rgb
                + tex2D(_MainTex, uv + o.xw).rgb + tex2D(_MainTex, uv + o.zw).rgb;
        return s * 0.25h;
    }

    half3 Prefilter(half3 c)
    {
        half br = max(c.r, max(c.g, c.b));
        half soft = clamp(br - _Threshold + _Knee, 0.0h, 2.0h * _Knee);
        soft = soft * soft / (4.0h * _Knee + 0.0001h);
        half contrib = max(soft, br - _Threshold) / max(br, 0.0001h);
        return c * contrib;
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            half4 frag (v2f_img i) : SV_Target { return half4(Prefilter(Sample4(i.uv, 1.0)), 1.0h); }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            half4 frag (v2f_img i) : SV_Target { return half4(Sample4(i.uv, 1.0), 1.0h); }
            ENDCG
        }

        Pass
        {
            Blend One One
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            half4 frag (v2f_img i) : SV_Target { return half4(Sample4(i.uv, 0.5), 1.0h); }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            half4 frag (v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                half3 c = tex2D(_MainTex, uv).rgb;
                if (_RadialBlur > 0.001h)
                {
                    float2 dir = uv - 0.5;
                    half3 acc = c;
                    [unroll]
                    for (int k = 1; k < 6; k++)
                        acc += tex2D(_MainTex, uv - dir * _RadialBlur * (k / 6.0)).rgb;
                    c = acc / 6.0h;
                }
                c += tex2D(_BloomTex, uv).rgb * _BloomIntensity;
                half l = dot(c, half3(0.299h, 0.587h, 0.114h));
                c = lerp(half3(l, l, l), c, _Saturation);
                c = (c - 0.5h) * _Contrast + 0.5h;
                c *= _Tint.rgb;
                float2 v = uv - 0.5;
                c *= saturate(1.0h - dot(v, v) * _Vignette);
                return half4(max(c, 0.0h), 1.0h);
            }
            ENDCG
        }
    }
    Fallback Off
}
