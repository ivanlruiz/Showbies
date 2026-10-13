// Un color plano, sin luz, con la niebla: lo mismo que Unlit/Color, pero con instancing.
//
// Es el de las balas (13/10, la auditoria de rendimiento de la etapa 4): en Android no hay
// batching dinamico y Unlit/Color no tiene variantes de instancing, asi que cada bala viva
// era una llamada de dibujo aparte; con la cadencia alta y las cartas de mas balas eran cientos.
// Las balas de un mismo tramo comparten el material (BulletController), y con esto salen
// juntas. El color va por material, no por instancia. Lo usa BalaPlana.mat, que el prefab de
// la bala trae puesto (y con eso el shader entra en la build).
Shader "ShowBies/ColorPlano"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(0)
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.pos = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _Color;
                UNITY_APPLY_FOG(i.fogCoord, col);
                UNITY_OPAQUE_ALPHA(col.a);
                return col;
            }
            ENDCG
        }
    }
}
