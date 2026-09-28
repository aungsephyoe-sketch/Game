// Cel-shaded world shader for terrain, foliage and scenery built as combined meshes: the same two-band
// lighting as the characters, colour taken from the vertices (so thousands of tufts, trees and rocks share one
// material), GPU wind sway weighted by vertex alpha, and an optional glow driven by vertex alpha (lava heat).
Shader "Hashira/ToonWorld"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Tint", Color) = (0.55,0.5,0.72,1)
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _Emission ("Emission", Color) = (0,0,0,0)
        _AlphaEmit ("Glow From Vertex Alpha", Color) = (0,0,0,0)
        _Wind ("Wind", Float) = 0
        _WindSpeed ("Wind Speed", Float) = 1.6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            fixed4 _Color;
            fixed4 _ShadowColor;
            fixed4 _RimColor;
            fixed4 _Emission;
            fixed4 _AlphaEmit;
            half _RimPower;
            float _Wind;
            float _WindSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                SHADOW_COORDS(2)
                UNITY_FOG_COORDS(3)
                float4 color : TEXCOORD4;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float4 wp = mul(unity_ObjectToWorld, v.vertex);
                float sway = v.color.a * _Wind;
                float t = _Time.y * _WindSpeed + wp.x * 0.31 + wp.z * 0.23;
                wp.x += (sin(t) + 0.4 * sin(t * 2.3 + 1.1)) * sway;
                wp.z += cos(t * 0.8 + 0.7) * sway * 0.6;
                o.pos = mul(UNITY_MATRIX_VP, wp);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = wp.xyz;
                o.color = v.color;
                TRANSFER_SHADOW(o)
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float3 v = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float ndl = dot(n, l);
                float shadow = SHADOW_ATTENUATION(i);
                // Cartoon world: two flat tones with a crisp edge, bright ambient, saturated colour.
                float hl = (ndl * 0.5 + 0.5) * lerp(0.6, 1.0, step(0.5, shadow));
                float lit = smoothstep(0.46, 0.5, hl);
                fixed3 baseCol = i.color.rgb * _Color.rgb;
                fixed3 ambient = ShadeSH9(float4(n, 1.0));
                fixed3 light = min(_LightColor0.rgb, 1.0);
                fixed3 col = lerp(baseCol * lerp(_ShadowColor.rgb, fixed3(1, 1, 1), 0.25) * 0.8, baseCol * lerp(fixed3(1, 1, 1), light, 0.6) * 1.04, lit);
                col += baseCol * ambient * 0.34;
                float rim = pow(1.0 - saturate(dot(n, v)), _RimPower) * saturate(ndl + 0.4);
                col += _RimColor.rgb * smoothstep(0.45, 0.5, rim) * 0.1;
                float luma = dot(col, float3(0.299, 0.587, 0.114));
                col = saturate(lerp(luma.xxx, col, 1.25));
                col = min(col, 0.98);
                col += _Emission.rgb + _AlphaEmit.rgb * i.color.a;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return fixed4(col, 1.0);
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/VertexLit"
}
