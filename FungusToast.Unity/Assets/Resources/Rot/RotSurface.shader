Shader "FungusToast/RotSurface"
{
    Properties
    {
        _Rot1 ("Rot 1", 2D) = "white" {}
        _Rot2 ("Rot 2", 2D) = "white" {}
        _Rot3 ("Rot 3", 2D) = "white" {}
        _Rot4 ("Rot 4", 2D) = "white" {}
        _Rot5 ("Rot 5", 2D) = "white" {}
        _CornerRadius ("Exposed corner radius (cells)", Range(0, 0.45)) = 0.30
        _EdgeInset ("Exposed edge inset (cells)", Range(0, 0.15)) = 0.045
        _EdgeRoughness ("Edge irregularity (cells)", Range(0, 0.1)) = 0.035
        _EdgeFeather ("Edge softness (cells)", Range(0.001, 0.1)) = 0.025
        _TextureWarp ("Organic texture warp (cells)", Range(0, 0.3)) = 0.18
        [HideInInspector] _RotVisualTime ("Presentation time", Float) = 0
        _PulseAmplitude ("Brightness pulse", Range(0, 0.1)) = 0.055
        _PulsePeriod ("Pulse period (seconds)", Float) = 6
        _DriftAmplitude ("Texture drift (cells)", Range(0, 0.1)) = 0.04
        _DriftPeriod ("Drift period (seconds)", Float) = 12
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        // Untagged pass works with built-in rendering and URP's SRPDefaultUnlit path.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _Rot1, _Rot2, _Rot3, _Rot4, _Rot5;
            float _CornerRadius, _EdgeInset, _EdgeRoughness, _EdgeFeather, _TextureWarp;
            float _RotVisualTime;
            float _PulseAmplitude, _PulsePeriod, _DriftAmplitude, _DriftPeriod;
            struct appdata
            {
                float4 vertex : POSITION;
                float2 localUV : TEXCOORD0;
                float2 boardUV : TEXCOORD1;
                float4 edges : TEXCOORD2; // left, right, bottom, top
                float4 diagonals : TEXCOORD3; // missing bottom-left/right, top-left/right
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 localUV : TEXCOORD0;
                float2 boardUV : TEXCOORD1;
                float4 edges : TEXCOORD2;
                float4 diagonals : TEXCOORD3;
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.localUV = v.localUV; o.boardUV = v.boardUV; o.edges = v.edges; o.diagonals = v.diagonals;
                return o;
            }
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + 1), f.x), f.y);
            }
            float BoundaryDistance(float2 uv, float4 e, float4 diagonals)
            {
                float d = min(min(lerp(2, uv.x, e.x), lerp(2, 1 - uv.x, e.y)),
                              min(lerp(2, uv.y, e.z), lerp(2, 1 - uv.y, e.w)));
                // Account for exposed segment endpoints in neighboring cells. This keeps
                // alpha continuous across shared edges beside concave bends and holes.
                if (diagonals.x > 0.5) d = min(d, length(uv));
                if (diagonals.y > 0.5) d = min(d, length(uv - float2(1, 0)));
                if (diagonals.z > 0.5) d = min(d, length(uv - float2(0, 1)));
                if (diagonals.w > 0.5) d = min(d, length(uv - float2(1, 1)));
                float r = _CornerRadius;
                // Only round exposed corners. Shared edges always remain fully connected.
                if (e.x * e.z > 0.5 && uv.x < r && uv.y < r) d = min(d, r - length(uv - float2(r, r)));
                if (e.y * e.z > 0.5 && uv.x > 1-r && uv.y < r) d = min(d, r - length(uv - float2(1-r, r)));
                if (e.x * e.w > 0.5 && uv.x < r && uv.y > 1-r) d = min(d, r - length(uv - float2(r, 1-r)));
                if (e.y * e.w > 0.5 && uv.x > 1-r && uv.y > 1-r) d = min(d, r - length(uv - float2(1-r, 1-r)));
                return d;
            }
            float2 ArtUV(float2 p)
            {
                return 0.18 + 0.64 * (1.0 - abs(frac(p * 0.5) * 2.0 - 1.0));
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Silhouette is stationary: motion cannot imply spreading or expose cell seams.
                float inset = _EdgeInset + _EdgeRoughness * Noise(i.boardUV * 7.0);
                float alpha = smoothstep(0, _EdgeFeather, BoundaryDistance(i.localUV, i.edges, i.diagonals) - inset);
                float driftTime = _RotVisualTime * 6.2831853 / max(_DriftPeriod, 0.1);
                float2 p = i.boardUV * 0.85 + _DriftAmplitude * float2(sin(driftTime), cos(driftTime * 0.83));
                // Mirror-repeat the opaque central art, avoiding transparent rectangular PNG rims.
                p += _TextureWarp * (float2(Noise(i.boardUV * 0.7), Noise(i.boardUV * 0.7 + 41.3)) - 0.5);
                float variant = Noise(i.boardUV * 0.23 + 13.7) * 4.0;
                // Offset/rotate sampling phases so mirrored axes do not form a visible grid.
                float4 a = tex2D(_Rot1, ArtUV(p));
                float4 b = tex2D(_Rot2, ArtUV(float2(-p.y, p.x) + float2(0.37, 0.61)));
                float4 c = tex2D(_Rot3, ArtUV(p + float2(0.73, 0.19)));
                float4 d = tex2D(_Rot4, ArtUV(float2(p.y, -p.x) + float2(0.23, 0.83)));
                float4 e = tex2D(_Rot5, ArtUV(p + float2(0.57, 0.47)));
                float wa = saturate(1-abs(variant)), wb = saturate(1-abs(variant-1));
                float wc = saturate(1-abs(variant-2)), wd = saturate(1-abs(variant-3)), we = saturate(1-abs(variant-4));
                float weight = wa*a.a + wb*b.a + wc*c.a + wd*d.a + we*e.a;
                float3 color = (wa*a.rgb*a.a + wb*b.rgb*b.a + wc*c.rgb*c.a + wd*d.rgb*d.a + we*e.rgb*e.a) / max(weight, 0.001);
                float pulse = 1.0 + _PulseAmplitude * sin(_RotVisualTime * 6.2831853 / max(_PulsePeriod, 0.1) + Noise(i.boardUV * 0.18) * 2.0);
                return fixed4(color * pulse, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
