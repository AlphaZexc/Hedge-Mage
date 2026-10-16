Shader "HedgeMage/HedgeWind"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WindAmplitude ("Wind Amplitude", Float) = 0.025
        _WindFrequency ("Wind Frequency", Float) = 1.8
        _WindSpeed ("Wind Speed", Float) = 1.4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "HedgeWind"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float _WindAmplitude;
                float _WindFrequency;
                float _WindSpeed;
            CBUFFER_END

            float4 _AmbientWindDir;
            float _AmbientWindStrength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                float2 windDir = float2(_AmbientWindDir.x, _AmbientWindDir.y);
                float windLen = length(windDir);
                windDir = windLen > 1e-4 ? windDir / windLen : float2(1.0, 0.0);

                float strength = max(_AmbientWindStrength, 0.0);
                float phase = (worldPos.x * 0.85 + worldPos.y * 1.15) * _WindFrequency
                            + _Time.y * _WindSpeed;
                float amp = _WindAmplitude * strength;

                worldPos.xy += windDir * sin(phase) * amp;
                worldPos.y += cos(phase * 1.37 + worldPos.x * 0.4) * amp * 0.3;

                OUT.positionCS = TransformWorldToHClip(worldPos);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 color = tex * IN.color;
                color.rgb *= color.a;
                clip(color.a - 0.001);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
