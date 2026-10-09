// La piel de los zombis: lo mismo que Legacy Shaders/Diffuse (la textura por _Color, con la
// luz de la luna, el relleno y la niebla) mas un brillo propio de su mismo color (_Brillo),
// que no depende de la luz. De noche la textura, que es oscura, casi no recibia luz y los
// zombis eran siluetas negras: lo dijeron en Discord (8/10), y mas luz de relleno no los
// despegaba del piso. Con el brillo, cada tipo se ve de su color en cualquier capitulo.
Shader "ShowBies/PielDeZombi"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Brillo ("Brillo propio", Range(0, 2)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _MainTex;
        fixed4 _Color;
        half _Brillo;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Emission = c.rgb * _Brillo;
        }
        ENDCG
    }
    Fallback "Legacy Shaders/VertexLit"
}
