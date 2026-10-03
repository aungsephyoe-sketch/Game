// Hand-painted anime eye, drawn procedurally on a small curved patch that sits on the face: almond opening,
// gradient iris with streaks and a dark rim, pupil, three highlights, a thick lash line with a flick at the
// outer corner and a fine lower lash. Everything is computed per pixel, so it stays razor sharp on posters.
//
// Blinks and expressions squash the eye's transform vertically (see Blinker / CharacterExpression). This shader
// reads that squash from its own object matrix, un-squashes the patch, and instead closes a lid over the eye,
// so a blink looks like a blink and a happy squint shows the bottom of the iris under a lowered lid.
Shader "Hashira/PaintedEye"
{
    Properties
    {
        _Iris ("Iris", Color) = (0.3,0.55,0.95,1)
        _IrisLow ("Iris Glow", Color) = (0.62,0.78,1,1)
        _Ink ("Lashes", Color) = (0.06,0.04,0.08,1)
        _Aspect ("Eye height / width", Float) = 1.1
        _Side ("Outer corner side (+1 / -1)", Float) = 1
        _Wing ("Lash flick", Float) = 1
        _Tilt ("Outer corner lift", Float) = 0.04
        _MX ("Patch width / eye width", Float) = 1.5
        _MY ("Patch height / eye height", Float) = 1.6
        _Flash ("Flash", Range(0,1)) = 0
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            // Alpha accumulates "over" the face, so portrait render textures stay opaque around the eyes.
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Offset -1, -1
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            fixed4 _Iris;
            fixed4 _IrisLow;
            fixed4 _Ink;
            float _Aspect;
            float _Side;
            float _Wing;
            float _Tilt;
            float _MX;
            float _MY;
            half _Flash;
            fixed4 _FlashColor;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float open : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                // How squashed the eye is right now, relative to its painted shape (1 = wide open).
                float sx = length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x));
                float sy = length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y));
                float open = saturate(sy / max(sx * _Aspect, 1e-6));
                float4 p = v.vertex;
                p.y /= max(open, 0.08);
                o.pos = UnityObjectToClipPos(p);
                o.uv = v.texcoord.xy;
                o.open = open;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            float Cover(float d, float px) { return saturate(0.5 + d / (px * 1.2)); }

            float Blob(float x, float y, float hx, float hy, float rx, float ry, float px)
            {
                float d = length(float2((x - hx) / rx, (y - hy) / ry));
                return Cover((1.0 - d) * rx, px);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float a = 0.5;
                float b = 0.5 * _Aspect;
                float x = (i.uv.x - 0.5) * _MX;
                float y = (i.uv.y - 0.5) * _MY * _Aspect;
                float px = max(max(fwidth(x), fwidth(y)), 1e-5);
                float open = i.open;
                float xo = x * _Side;

                // Lids: the upper one comes down toward the lower one as the eye closes.
                float t = saturate(1.0 - (x / a) * (x / a));
                float lift = _Tilt * xo;
                float yU = b * pow(max(t, 1e-5), 0.55) + lift;
                float yL = -b * 0.8 * pow(max(t, 1e-5), 0.85) + lift;
                float yO = lerp(yL, yU, open);
                float inside = Cover(min(yO - y, y - yL), px) * saturate((yO - yL) / px - 0.5);

                // Iris: glow at the bottom, darker under the lid, fine streaks, a dark rim; then the pupil.
                float cx = 0.0;
                float cy = -0.06 * b;
                float rx = 0.3;
                float ry = 0.88 * b;
                float q = length(float2((x - cx) / rx, (y - cy) / ry));
                float irisM = Cover((1.0 - q) * rx, px);
                float g = saturate((y - (cy - ry)) / (2.0 * ry));
                float3 ic = lerp(_IrisLow.rgb, _Iris.rgb, smoothstep(0.15, 0.75, g));
                ic *= lerp(1.0, 0.45, smoothstep(0.6, 1.0, g));
                float ang = atan2(y - cy, x - cx);
                ic *= 1.0 + 0.08 * sin(ang * 14.0) * smoothstep(0.3, 0.9, q);
                ic *= lerp(1.0, 0.55, smoothstep(0.78, 0.98, q));
                float pq = length(float2((x - cx) / (rx * 0.42), (y - cy + 0.04 * b) / (ry * 0.5)));
                float pupil = Cover((1.0 - pq) * rx * 0.42, px);
                ic = lerp(ic, _Ink.rgb * 0.6 + _Iris.rgb * 0.15, pupil);

                // White of the eye, in the lid's shadow near the top.
                float lidGap = yO - y;
                float3 sclera = float3(0.99, 0.98, 1.0) * lerp(0.8, 1.0, smoothstep(0.0, 0.84 * b, lidGap));
                float3 col = lerp(sclera, ic, irisM);
                col *= lerp(0.7, 1.0, smoothstep(0.0, 0.6 * b, lidGap));

                // Highlights (the same side on both eyes: one light).
                float hl = max(Blob(x, y, cx - 0.11, cy + 0.36 * ry, 0.1, 0.24 * ry, px), Blob(x, y, cx + 0.12, cy - 0.42 * ry, 0.045, 0.1 * ry, px));
                hl = max(hl, Blob(x, y, cx + 0.02, cy + 0.5 * ry, 0.03, 0.05 * ry, px) * 0.9);
                col = lerp(col, float3(1.0, 1.0, 1.0), hl * irisM);
                float alpha = inside;

                // Upper lash line: rides the lid, thicker toward the outer corner, tapering at the inner one.
                float th = b * 0.2 * (0.55 + 0.75 * smoothstep(-a, a, xo)) * smoothstep(-a * 1.05, -a * 0.55, xo);
                float lashU = Cover(min(min(y - (yO - th * 0.3), (yO + th * 0.7) - y), a * 1.02 - abs(x)), px);
                // The flick at the outer corner, riding the lid too.
                float tw = 1.0 - 0.64;
                float yw = -b * 0.8 * pow(tw, 0.85) + (b * pow(tw, 0.55) + b * 0.8 * pow(tw, 0.85)) * open + _Tilt * a * 0.8;
                float2 w0 = float2(a * 0.78, yw);
                float2 dw = float2(a * 0.36, b * 0.42);
                float2 pw = float2(xo, y) - w0;
                float tt = saturate(dot(pw, dw) / dot(dw, dw));
                float wing = Cover(b * 0.1 * (1.0 - tt * 0.8) - length(pw - dw * tt), px) * _Wing;
                // A fine lower lash on the outer half.
                float lowT = b * 0.07 * smoothstep(-0.1, a, xo);
                float lashL = Cover(min(min(y - (yL - lowT * 0.6), (yL + lowT * 0.4) - y), a - abs(x)), px) * smoothstep(-0.1, 0.25, xo);
                float inkA = max(max(lashU, wing), lashL);
                col = lerp(col, _Ink.rgb, inkA);
                alpha = max(alpha, inkA);

                // A touch of the scene's light so the eyes sit in the face (they stay bright and clean).
                float3 n = normalize(i.worldNormal);
                float ndl = dot(n, normalize(_WorldSpaceLightPos0.xyz));
                float3 env = saturate(_LightColor0.rgb * 0.6 + ShadeSH9(float4(n, 1.0)) * 0.8);
                col *= lerp(float3(1.0, 1.0, 1.0), env, 0.3) * lerp(0.9, 1.0, saturate(ndl * 0.5 + 0.5));
                col = lerp(col, _FlashColor.rgb, saturate(_Flash) * 0.75);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
