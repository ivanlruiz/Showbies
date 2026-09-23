// El fogonazo de la pistola (ArmaEnLaMano): una estrella aditiva, un nucleo casi blanco con
// puntas del color del fuego, calculada en el shader y sin textura, asi queda nitida a
// cualquier tamaño. El cuadrado se estira hacia donde apunta el cañon; _Giro cambia las
// puntas en cada tiro para que no se vea siempre la misma.
Shader "ShowBies/Fogonazo"
{
    Properties
    {
        _Color ("Color del fuego", Color) = (1, 0.72, 0.25, 1)
        _Intensidad ("Intensidad", Float) = 2
        _Puntas ("Puntas", Float) = 4
        _Giro ("Giro de las puntas", Float) = 0
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
            #include "UnityCG.cginc"

            fixed4 _Color;
            half _Intensidad;
            half _Puntas;
            half _Giro;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy * 2 - 1;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                half r = length(i.uv);
                half angulo = atan2(i.uv.y, i.uv.x) + _Giro;
                // El nucleo, blanco y chico; las puntas, afiladas, se apagan hacia el borde.
                half nucleo = saturate(1 - r * 2.4);
                nucleo *= nucleo;
                half puntas = pow(abs(cos(angulo * _Puntas * 0.5)), 10) * saturate(1 - r);
                half halo = saturate(1 - r) * 0.25;
                half3 fuego = _Color.rgb * (puntas + halo);
                half3 col = (fuego + nucleo * half3(1, 0.97, 0.85) * 1.6) * _Intensidad;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
