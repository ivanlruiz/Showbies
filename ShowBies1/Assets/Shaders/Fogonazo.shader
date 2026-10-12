// El fogonazo de la pistola (ArmaEnLaMano): una estrella aditiva, un nucleo casi blanco con
// puntas del color del fuego, calculada en el shader y sin textura, asi queda nitida a
// cualquier tamaño. El cuadrado se estira hacia donde apunta el cañon; _Giro cambia las
// puntas en cada tiro para que no se vea siempre la misma. Con _Puntas en 0 es un disco
// (la pistola y el revolver); con _Anillo en 1, un anillo de radio _RadioAnillo sin fuego,
// el del arma de energia, que ArmaEnLaMano agranda mientras dura.
Shader "ShowBies/Fogonazo"
{
    Properties
    {
        _Color ("Color del fuego", Color) = (1, 0.72, 0.25, 1)
        _Intensidad ("Intensidad", Float) = 2
        _Puntas ("Puntas", Float) = 4
        _Giro ("Giro de las puntas", Float) = 0
        _Anillo ("Anillo (arma de energia)", Float) = 0
        _RadioAnillo ("Radio del anillo", Float) = 0.5
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
            half _Anillo;
            half _RadioAnillo;

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
                // El anillo: una banda fina en _RadioAnillo, que se apaga al crecer.
                half d = (r - _RadioAnillo) * 9;
                half anillo = exp(-d * d) * saturate(1.15 - r);
                half3 colAnillo = (_Color.rgb + anillo * 0.35) * anillo * _Intensidad;
                col = lerp(col, colAnillo, _Anillo);
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
