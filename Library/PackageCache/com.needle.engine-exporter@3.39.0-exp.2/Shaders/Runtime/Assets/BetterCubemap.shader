// Cubemap Shader with blur support by Needle.

Shader "Skybox/Better Cubemap (Needle)" {
Properties {
    _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
    [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
    _Rotation ("Rotation", Range(0, 360)) = 0
    [NoScaleOffset] _Tex ("Cubemap   (HDR)", Cube) = "grey" {}
    
    // Additions for blurred backgrounds.
    
    [Toggle] _Lod("Enable Blur", Float) = 1
    _BackgroundBlurriness("Blurriness", Range(0, 1)) = 0.0
    _BackgroundIntensity("Exposure", Range(0, 2)) = 1.0
    _BakeBlurriness("Cubemap Blur", Range(0, 1)) = 0.0
    
    [HideInInspector] [KeywordEnum(BuiltIn, Extra)] _CUBEMAP_USAGE("Enable Extra", Float) = 0
    
    // Is a global property now
    // [HideInInspector] [NoScaleOffset] _Needle_SkyboxPreconvolutedTex ("Blurred Cubemap", Cube) = "grey" {}
    [HideInInspector] _AutoBakeOnChanges("Auto Bake On Changes", Int) = 1
    
    // Extra keyword that's only set in the editor.
    // Computationally heavy blur code is only run in the Editor.
    [HideInInspector] [Toggle] _Needle_Editor("", Float) = 1
}

SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off

    Pass {

        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0

        #pragma shader_feature _LOD_ON
        #pragma multi_compile _CUBEMAP_USAGE_BUILTIN _CUBEMAP_USAGE_EXTRA _CUBEMAP_USAGE_ORIGINAL
        #pragma multi_compile __ _NEEDLE_EDITOR_ON
        #include "UnityCG.cginc"

        samplerCUBE _Tex;
        UNITY_DECLARE_TEXCUBE(_Needle_SkyboxPreconvolutedTex);
        half4 _Tex_TexelSize;
        half4 _Tex_HDR;
        half4 _Tint;
        half _Exposure;
        half _BackgroundIntensity;
        float _Rotation;
        half _BackgroundBlurriness;
        half _BakeBlurriness;

        #if defined(_CUBEMAP_USAGE_EXTRA)
        #define CUBEMAP _Needle_SkyboxPreconvolutedTex
        #else
        #define CUBEMAP unity_SpecCube0
        #endif

        float3 RotateAroundYInDegrees (float3 vertex, float degrees)
        {
            float alpha = degrees * UNITY_PI / 180.0;
            float sina, cosa;
            sincos(alpha, sina, cosa);
            float2x2 m = float2x2(cosa, -sina, sina, cosa);
            return float3(mul(m, vertex.xz), vertex.y).xzy;
        }

        struct appdata_t {
            float4 vertex : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
            float3 wp : TEXCOORD1;
            float3 texcoordUnrotated : TEXCOORD2;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        v2f vert (appdata_t v)
        {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            float3 rotated = RotateAroundYInDegrees(v.vertex.xyz, _Rotation);
            o.vertex = UnityObjectToClipPos(rotated);
            o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.texcoord = v.vertex.xyz;
            o.texcoordUnrotated = rotated;
            return o;
        }
        
        // constant for gaussian blur 5x5 - should be 5 values, symmetric
        static const float kGaussianMultipliers[5] = { 0.06136, 0.24477, 0.38774, 0.24477, 0.06136 };
        static const float mipAdditions[5] = { 1.0, 0.0, -1.0, 0.0, 1.0 };
            
        fixed4 frag (v2f i) : SV_Target
        {
            // Check if we're baking right now.
            // Since there are no reliable keywords, we need to infer from as much data as possible.
            // - camera position is (0,0,0) during baking
            // - fov is 90 degrees during baking
            // - near and far plane are 0.5 and 1000 during baking
            // - image is square during baking
            
            bool worldPosZero = dot(_WorldSpaceCameraPos, _WorldSpaceCameraPos) == 0;
            float fov = 2.0 * atan(1.0 / unity_CameraProjection[1][1]) / 3.1415926535897932384626433832795;
            bool fovIs90 = abs(fov - 0.5) < 0.00001;
            bool imageIsSquare = abs(unity_CameraProjection[0][0] - unity_CameraProjection[1][1]) < 0.00001;

            bool isBaking = worldPosZero && fovIs90 && imageIsSquare;
            
            half3 c = half3(0, 0, 0); 
#if defined(_LOD_ON)
            if (!isBaking)
            {
                if (_BackgroundBlurriness > 0.0 || _BakeBlurriness > 0.0)
                {
                    // See https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/CGIncludes/UnityImageBasedLighting.cginc#L522
                    // for some code related to roughness-based mip level selection.

                    // Fast path for the background: we already have a preconvoluted cubemap in unity_SpecCube0
                    half perceptualRoughness = _BackgroundBlurriness;
                    perceptualRoughness = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness);
                    half mip = perceptualRoughness * 6; // UNITY_SPECCUBE_LOD_STEPS is defined as 6 in UnityStandardConfig.cginc
                    
                    half4 skyData = UNITY_SAMPLE_TEXCUBE_LOD(CUBEMAP, i.texcoordUnrotated, mip);
                    
                    half3 skyColor = skyData.rgb;
                    c = skyColor * _BackgroundIntensity;
                }
                else
                {
                    half4 tex = texCUBE(_Tex, i.texcoord);
                    c = DecodeHDR (tex, _Tex_HDR);
                    c *= _BackgroundIntensity;
                }
            }
#if _NEEDLE_EDITOR_ON
            else if (_BakeBlurriness > 0.0)
            {
                // Slow path for blurring during baking: we need to do the blurring ourselves.

                // do 25 taps for mip level and sum it up to get nicer blur
                half3 c0 = half3(0, 0, 0);
                half perceptualRoughness = _BakeBlurriness;
                perceptualRoughness = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness);
                half mip = perceptualRoughness * 6; // UNITY_SPECCUBE_LOD_STEPS is defined as 6 in UnityStandardConfig.cginc
                
                half2 offset = _Tex_TexelSize.xy * mip * 4;
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        half mipAddition = mipAdditions[x + 2] * mipAdditions[y + 2];
                        half4 tex = texCUBElod(_Tex, float4(i.texcoord + half3(offset * float2(x, y), 0), mip * 1.5 + mipAddition * 1.5));
                        c0 += DecodeHDR(tex, _Tex_HDR).rgb * kGaussianMultipliers[x + 2] * kGaussianMultipliers[y + 2];
                    }
                }
                c0 /= 1.0;
                c = c0;
            }
#endif
            else
            {
                half4 tex = texCUBE(_Tex, i.texcoord);
                c = DecodeHDR (tex, _Tex_HDR);
            }
#else
            half4 tex = texCUBE(_Tex, i.texcoord);
            c = DecodeHDR (tex, _Tex_HDR);
#endif
            
            c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
            c *= _Exposure;
            return half4(c, 1);
        }
        ENDCG
    }
}


Fallback "Skybox/Cubemap"
CustomEditor "Needle.BetterCubemapEditor"

}