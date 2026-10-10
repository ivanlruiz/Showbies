// La piel de los zombis: lo mismo que Legacy Shaders/Diffuse (la textura por _Color, con la
// luz de la luna, el relleno y la niebla) mas un brillo propio de su mismo color (_Brillo),
// que no depende de la luz. De noche la textura, que es oscura, casi no recibia luz y los
// zombis eran siluetas negras: lo dijeron en Discord (8/10), y mas luz de relleno no los
// despegaba del piso. Con el brillo, cada tipo se ve de su color en cualquier capitulo.
//
// El borde de luz (rim light, 10/10, de la revision de Gemini): lo que mira de costado a la
// camara se prende con _ColorBorde, para que la silueta se despegue del piso. Es una cuenta por
// vertice en la misma pasada (un contorno de dos pasadas duplicaria el skinning de cada zombi;
// por vertice alcanza a 40 px y es mas barato que por pixel). En 0, que es como viene, la piel
// queda como antes.
Shader "ShowBies/PielDeZombi"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Brillo ("Brillo propio", Range(0, 2)) = 0.5
        _Borde ("Borde de luz", Range(0, 3)) = 0
        _ColorBorde ("Color del borde", Color) = (1, 1, 1, 1)
        _FinuraBorde ("Finura del borde", Range(0.5, 8)) = 2.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert

        sampler2D _MainTex;
        fixed4 _Color;
        half _Brillo;
        half _Borde;
        fixed4 _ColorBorde;
        half _FinuraBorde;

        struct Input
        {
            float2 uv_MainTex;
            half borde;
        };

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            half deFrente = saturate(dot(normalize(ObjSpaceViewDir(v.vertex)), normalize(v.normal)));
            o.borde = pow(1 - deFrente, _FinuraBorde) * _Borde;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Emission = c.rgb * _Brillo + _ColorBorde.rgb * IN.borde;
        }
        ENDCG
    }
    Fallback "Legacy Shaders/VertexLit"
}
