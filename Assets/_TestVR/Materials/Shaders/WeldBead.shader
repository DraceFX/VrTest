Shader "Custom/WeldHeatVR"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.3,0.3,0.3,1)
        _HotColor ("Hot Color", Color) = (1,0.5,0,1)
        _Heat ("Heat", Range(0,1)) = 1
        _EmissionStrength ("Emission", Range(0,10)) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _BaseColor;
            fixed4 _HotColor;

            float _Heat;
            float _EmissionStrength;

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                fixed3 col =
                    lerp(
                        _BaseColor.rgb,
                        _HotColor.rgb,
                        _Heat
                    );

                col +=
                    _HotColor.rgb *
                    _Heat *
                    _EmissionStrength;

                return fixed4(col, 1);
            }

            ENDCG
        }
    }
}