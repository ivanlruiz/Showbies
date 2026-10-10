// La sombra falsa de un personaje (10/10, de la revision de Gemini): un disco oscuro en el piso,
// debajo de los pies, que se apaga hacia el borde, calculado en el shader y sin textura. Va en un
// cuadrado acostado. En el telefono reemplaza a las sombras de verdad, que son una pasada mas
// de toda la escena desde la luna: en el Redmi Note 8 eso pesaba, y desde la camara de arriba lo
// que se lee es la mancha bajo los pies, no la forma de la sombra.
Shader "ShowBies/SombraFalsa"
{
    Properties
    {
        _Opacidad ("Opacidad en el centro", Range(0, 1)) = 0.55
        _Duro ("Donde empieza a apagarse, de 0 a 1", Range(0, 0.95)) = 0.35
    }

    SubShader
    {
        // Antes que lo transparente (los charcos de luz, que suman encima) y despues de lo opaco:
        // los pies tapan el disco por la profundidad, asi no oscurece al personaje.
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            half _Opacidad;
            half _Duro;

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
                half r = sqrt(dot(i.uv, i.uv));
                half a = _Opacidad * (1 - smoothstep(_Duro, 1, r));
                fixed4 col = fixed4(0, 0, 0, a);
                // Con la niebla, la sombra se pierde como el piso: hacia el cielo.
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
