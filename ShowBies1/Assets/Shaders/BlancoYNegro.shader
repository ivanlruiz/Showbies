// Filtro de pantalla que le saca el color a lo que ve la camara. Lo usa la oferta
// de revivir: al morir, el mundo se va quedando en blanco y negro mientras la
// ventanita, que es UI en overlay, sigue a todo color.
//
// Es un image effect de los de siempre (Graphics.Blit con OnRenderImage): el
// proyecto es built-in y no tiene post-proceso, asi que no hay volumen ni stack
// donde meterlo.
Shader "ShowBies/BlancoYNegro"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        // 0 = como estaba, 1 = gris del todo.
        _Cantidad ("Cantidad", Range(0, 1)) = 0
    }

    SubShader
    {
        // Un quad de pantalla completa: sin cull, sin z, sin escribir profundidad.
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Cantidad;

            fixed4 frag (v2f_img entrada) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, entrada.uv);
                // Luminancia percibida (la misma que usa Unity en Luminance()): el
                // verde pesa mas que el rojo y mucho mas que el azul, asi que el
                // pasto y la sangre no terminan en el mismo gris.
                fixed gris = dot(color.rgb, fixed3(0.299, 0.587, 0.114));
                color.rgb = lerp(color.rgb, gris.xxx, saturate(_Cantidad));
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
