// Cel shader for the melted, single-surface character bodies (see SdfMesher / CharacterVisual.SmoothBody).
// Same look as Hashira/Toon, but the colour comes from a palette of up to eight slots: every vertex stores its
// distance to the nearest part of each slot, and each pixel takes the closest one, so colour edges follow the
// original parts' outlines crisply (anti-aliased) instead of the triangles. Each slot also has its own painted
// surface texture (none, cloth, hair or skin), drawn in the body's rest pose so it doesn't swim when limbs move.
Shader "Hashira/ToonSkin"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _P0 ("Slot 0", Color) = (1,1,1,1)
        _P1 ("Slot 1", Color) = (1,1,1,1)
        _P2 ("Slot 2", Color) = (1,1,1,1)
        _P3 ("Slot 3", Color) = (1,1,1,1)
        _P4 ("Slot 4", Color) = (1,1,1,1)
        _P5 ("Slot 5", Color) = (1,1,1,1)
        _P6 ("Slot 6", Color) = (1,1,1,1)
        _P7 ("Slot 7", Color) = (1,1,1,1)
        _K0 ("Texture kinds 0-3 (0 none, 1 cloth, 2 hair, 3 skin)", Vector) = (0,0,0,0)
        _K1 ("Texture kinds 4-7", Vector) = (0,0,0,0)
        _ShadowColor ("Shadow Tint", Color) = (0.55,0.5,0.72,1)
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.5
        _OutlineColor ("Outline Color", Color) = (0.04,0.03,0.07,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.03
        _Emission ("Emission", Color) = (0,0,0,0)
        _Flash ("Flash", Range(0,1)) = 0
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
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            fixed4 _Color;
            fixed4 _P0, _P1, _P2, _P3, _P4, _P5, _P6, _P7;
            float4 _K0, _K1;
            fixed4 _ShadowColor;
            fixed4 _RimColor;
            fixed4 _Emission;
            fixed4 _FlashColor;
            half _RimPower;
            half _Flash;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                SHADOW_COORDS(2)
                UNITY_FOG_COORDS(3)
                float3 restPos : TEXCOORD4;
                float4 pal0 : TEXCOORD5;
                float4 pal1 : TEXCOORD6;
            };

            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.restPos = v.texcoord3.xyz;
                o.pal0 = v.texcoord1;
                o.pal1 = v.texcoord2;
                TRANSFER_SHADOW(o)
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            // Keeps the closest and second-closest palette slots.
            void Try(float d, float3 c, float k, inout float bestD, inout float3 bestC, inout float bestK, inout float secD, inout float3 secC)
            {
                if (d < bestD) { secD = bestD; secC = bestC; bestD = d; bestC = c; bestK = k; }
                else if (d < secD) { secD = d; secC = c; }
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float3 v = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float ndl = dot(n, l);
                float shadow = SHADOW_ATTENUATION(i);
                float hl = ndl * 0.5 + 0.5;
                hl *= lerp(0.55, 1.0, step(0.5, shadow));
                float aa = 0.15;
                float toMid = smoothstep(0.42 - aa, 0.42 + aa, hl);
                float toLit = smoothstep(0.7 - aa, 0.7 + aa, hl);

                // Palette: the nearest slot wins; the boundary with the runner-up is softened over about a pixel.
                float bestD = i.pal0.x, secD = 100.0, bestK = _K0.x;
                float3 bestC = _P0.rgb, secC = _P0.rgb;
                Try(i.pal0.y, _P1.rgb, _K0.y, bestD, bestC, bestK, secD, secC);
                Try(i.pal0.z, _P2.rgb, _K0.z, bestD, bestC, bestK, secD, secC);
                Try(i.pal0.w, _P3.rgb, _K0.w, bestD, bestC, bestK, secD, secC);
                Try(i.pal1.x, _P4.rgb, _K1.x, bestD, bestC, bestK, secD, secC);
                Try(i.pal1.y, _P5.rgb, _K1.y, bestD, bestC, bestK, secD, secC);
                Try(i.pal1.z, _P6.rgb, _K1.z, bestD, bestC, bestK, secD, secC);
                Try(i.pal1.w, _P7.rgb, _K1.w, bestD, bestC, bestK, secD, secC);
                float gap = secD - bestD;
                float edge = saturate(gap / max(fwidth(gap) * 1.5, 1e-5));
                fixed3 baseCol = lerp((bestC + secC) * 0.5, bestC, edge) * _Color.rgb;

                // Painted surface texture in the rest pose (all three computed, then picked, so the screen-space
                // derivatives never sit inside per-pixel branches).
                float3 op = i.restPos;
                float fwCloth = saturate(1.0 - fwidth(op.y * 220.0) * 0.6);
                float weave = (sin(op.x * 220.0) * sin(op.y * 220.0) + sin(op.z * 220.0) * sin(op.y * 220.0)) * 0.5;
                float folds = sin(op.y * 38.0 + sin(op.x * 21.0 + op.z * 17.0) * 2.2);
                float txCloth = weave * 0.05 * fwCloth + folds * 0.055;
                float fwHair = saturate(1.0 - fwidth((op.x + op.z) * 260.0) * 0.5);
                float strands = sin((op.x + op.z) * 260.0 + sin(op.y * 40.0) * 3.0);
                float clumps = sin((op.x - op.z) * 55.0 + op.y * 12.0);
                float txHair = strands * 0.07 * fwHair + clumps * 0.04;
                float txSkin = sin(op.x * 90.0 + op.z * 40.0) * sin(op.y * 80.0) * 0.018;
                float tx = bestK < 0.5 ? 0.0 : bestK < 1.5 ? txCloth : bestK < 2.5 ? txHair : txSkin;
                baseCol *= 1.0 + tx;

                fixed3 ambient = ShadeSH9(float4(n, 1.0));
                fixed3 light = min(_LightColor0.rgb, 1.0);
                fixed3 shadowTone = baseCol * lerp(_ShadowColor.rgb, fixed3(1, 1, 1), 0.15) * 0.8;
                fixed3 midTone = baseCol * lerp(fixed3(1, 1, 1), light, 0.5) * 0.97;
                fixed3 litTone = baseCol * lerp(fixed3(1, 1, 1), light, 0.6) * 1.06;
                fixed3 col = lerp(lerp(shadowTone, midTone, toMid), litTone, toLit);
                col += baseCol * ambient * 0.26;
                float sky = saturate(n.y * 0.5 + 0.5);
                col *= lerp(0.86, 1.1, sky);

                float3 h = normalize(l + v);
                float spec = smoothstep(0.9, 0.99, dot(n, h)) * step(0.5, shadow);
                col += spec * 0.07;

                float rim = pow(1.0 - saturate(dot(n, v)), _RimPower) * saturate(ndl + 0.5);
                col += _RimColor.rgb * smoothstep(0.25, 0.65, rim) * 0.12;
                float luma = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(luma.xxx, col, 1.32);
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
    Fallback Off
}
