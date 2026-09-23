// El charco de luz de un farol sobre el piso: un circulo aditivo que se apaga como una
// luz puntual colgada a esa altura (Lambert por la caida con la distancia), calculado en
// el shader y sin textura, asi no hace bandas.
//
// Existe porque en el telefono los faroles no alumbraban el piso: Android va en calidad
// Medium, con una sola luz por pixel, que se lleva la luna; las luces puntuales caen a
// luz por vertice, y el piso es un plano con un vertice cada diez metros. En el editor,
// en Ultra, se veian bien. Con esto se ven igual en las dos (ver ConstructorEscenarios).
Shader "ShowBies/CharcoDeLuz"
{
    Properties
    {
        _Color ("Color de la luz", Color) = (1, 0.72, 0.42, 1)
        _Intensidad ("Intensidad", Float) = 1
        _Altura ("Altura de la luz, en radios del charco", Float) = 0.4
        _Caida ("Caida con la distancia", Float) = 6
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Color;
            half _Intensidad;
            half _Altura;
            half _Caida;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy * 2 - 1;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // En radios del charco: el borde es 1. La luz cuelga a _Altura sobre el centro.
                half r2 = dot(i.uv, i.uv);
                half d2 = r2 + _Altura * _Altura;
                half luz = _Altura * rsqrt(d2) / (1 + _Caida * d2);
                // Y se apaga antes del borde, sin que se vea el corte del cuadrado.
                half borde = saturate((1 - r2) * 2);
                luz *= borde * borde * (3 - 2 * borde);

                fixed4 col = fixed4(_Color.rgb * (_Intensidad * luz), 1);
                // Aditivo: la niebla lo lleva a negro, no al color de la niebla.
                UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0, 0, 0, 0));
                return col;
            }
            ENDCG
        }
    }
}
