// Cartoon cel-shaded lit shader (Built-in Render Pipeline): three flat tones with tinted shadows, rim light,
// inverted-hull outline, emission and a "_Flash" overlay used for hit flashes / charge glow.
Shader "Hashira/Toon"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Tint", Color) = (0.55,0.5,0.72,1)
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.5
        _OutlineColor ("Outline Color", Color) = (0.04,0.03,0.07,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.03
        _Emission ("Emission", Color) = (0,0,0,0)
        _Flash ("Flash", Range(0,1)) = 0
        _Crisp ("Crisp (weapons)", Range(0,1)) = 0
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
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
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _ShadowColor;
            fixed4 _RimColor;
            fixed4 _Emission;
            fixed4 _FlashColor;
            half _RimPower;
            half _Crisp;
            half _Flash;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                SHADOW_COORDS(2)
                UNITY_FOG_COORDS(3)
                float2 uv : TEXCOORD4;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
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
                // Cartoon cel shading: three flat tones (shadow, mid, lit) with crisp, anti-aliased edges,
                // a bright hard highlight, a hard rim and strong ambient so colours stay bright and readable.
                float hl = ndl * 0.5 + 0.5;
                hl *= lerp(0.55, 1.0, step(0.5, shadow));
                // Soft, squishy bodies: wide, gentle steps between tones. Weapons (_Crisp = 1) keep hard edges.
                float aa = lerp(0.11, 0.02, _Crisp);
                float toMid = smoothstep(0.42 - aa, 0.42 + aa, hl);
                float toLit = smoothstep(0.7 - aa, 0.7 + aa, hl);

                fixed3 baseCol = tex2D(_MainTex, i.uv).rgb * _Color.rgb;
                fixed3 ambient = ShadeSH9(float4(n, 1.0));
                fixed3 light = min(_LightColor0.rgb, 1.0);
                fixed3 shadowTone = baseCol * lerp(_ShadowColor.rgb, fixed3(1, 1, 1), 0.15) * lerp(0.8, 0.66, _Crisp);
                fixed3 midTone = baseCol * lerp(fixed3(1, 1, 1), light, 0.5) * 0.97;
                fixed3 litTone = baseCol * lerp(fixed3(1, 1, 1), light, 0.6) * 1.06;
                fixed3 col = lerp(lerp(shadowTone, midTone, toMid), litTone, toLit);
                col += baseCol * ambient * 0.26;
                // Toy-figure volume on bodies: a soft sky light from above and a gentle bounce below, so rounded
                // shapes read as smooth, chunky 3D forms (not shine). Weapons keep their flat cel look.
                float sky = saturate(n.y * 0.5 + 0.5);
                col *= lerp(1.0, lerp(0.9, 1.08, sky), 1.0 - _Crisp);

                // Bright cartoon highlight: a small hard spot.
                float3 h = normalize(l + v);
                // Matte on bodies (a faint soft sheen), a hard glint on weapons.
                float spec = smoothstep(lerp(0.9, 0.955, _Crisp), lerp(0.99, 0.965, _Crisp), dot(n, h)) * step(0.5, shadow);
                col += spec * lerp(0.07, 0.22, _Crisp);

                float rim = pow(1.0 - saturate(dot(n, v)), _RimPower) * saturate(ndl + 0.5);
                col += _RimColor.rgb * smoothstep(lerp(0.25, 0.4, _Crisp), lerp(0.65, 0.45, _Crisp), rim) * lerp(0.08, 0.16, _Crisp);
                // Saturated, punchy colour.
                float luma = dot(col, float3(0.299, 0.587, 0.114));
                // Rich, cartoony colour on bodies (soft shading keeps it from looking harsh).
                col = lerp(luma.xxx, col, lerp(1.32, 1.22, _Crisp));
                col = saturate(col);
                col = min(col, 0.98);
                col += _Emission.rgb;
                col = lerp(col, _FlashColor.rgb, saturate(_Flash) * 0.75);

                UNITY_APPLY_FOG(i.fogCoord, col);
                return fixed4(col, 1.0);
            }
            ENDCG
        }

        Pass
        {
            Name "OUTLINE"
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            half _OutlineWidth;
            half _Flash;
            fixed4 _FlashColor;

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(0)
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                float3 n = normalize(v.normal);
                o.pos = UnityObjectToClipPos(v.vertex + float4(n * _OutlineWidth, 0.0));
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = lerp(_OutlineColor, _FlashColor, saturate(_Flash) * 0.5);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
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
