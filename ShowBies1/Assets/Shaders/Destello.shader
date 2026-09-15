// El destello blanco de los zombis cuando reciben un golpe (Prefabs/Materials/Destello.mat).
//
// Un color plano y sin luz, igual que Unlit/Color, pero con pasada de sombra:
// Unlit/Color no la tiene, y mientras el zombi estaba blanco dejaba de proyectar
// su sombra en el piso. La sombra la saca del VertexLit de Unity, que tambien
// sirve para las mallas con esqueleto.
Shader "ShowBies/Destello"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(0)
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = _Color;
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
