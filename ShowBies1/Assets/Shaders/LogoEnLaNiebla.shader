// El logo del juego como parte del fondo del menu (Materiales/LogoMenu.mat).
//
// Un quad sin luz con la imagen del logo, que se esconde a medias en la niebla y vuelve
// a salir: lo maneja TituloEnLaNiebla. La niebla es a mano y no la de Unity, igual que
// cuando el titulo era un TextMeshPro, porque tiene que ir al color del cielo de
// FondoMenu y no al de la niebla de la escena, que ademas aca esta lejisimos.
//
// `_Niebla` va de 0 (el logo como es) a 1 (del color del cielo entero). El alfa no se
// toca: lo que se esconde es el color, no la silueta, asi el logo nunca desaparece del
// todo y se sigue leyendo en las capturas de la ficha.
Shader "ShowBies/LogoEnLaNiebla"
{
    Properties
    {
        _MainTex ("Logo", 2D) = "white" {}
        _Color ("Tinte", Color) = (1, 1, 1, 1)
        _ColorNiebla ("Color de la niebla", Color) = (0.66, 0.86, 0.96, 1)
        _Niebla ("Niebla", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _ColorNiebla;
            float _Niebla;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                c.rgb = lerp(c.rgb, _ColorNiebla.rgb, _Niebla);
                return c;
            }
            ENDCG
        }
    }

    Fallback "Unlit/Transparent"
}
