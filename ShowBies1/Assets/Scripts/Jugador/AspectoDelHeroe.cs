using UnityEngine;

// Lo que hace que el heroe se encuentre de un vistazo (10/10, Ivan lo eligio entre maquetas, de
// la revision de Gemini: con la remera negra y la gorra, de noche era un bulto oscuro):
//  - la remera blanca: el material del pack (TT_demo, que no se toca) cambia por una copia con
//    la remera pintada (Materiales/Resources/HeroeRemeraBlanca, que arma ConstructorHeroe);
//  - un aura bajo los pies, un charco de luz blanca que late apenas (AuraHeroe, con el shader de
//    los faroles: no es una luz de verdad);
//  - en el telefono, la sombra falsa (SombrasFalsas).
// Lo agrega ArmaEnLaMano al arrancar, que es quien encuentra el modelo (no esta en el prefab
// sino en cada escena). El aura y la sombra no son hijas del modelo: van en el piso, a la altura
// de los pies de siempre, aunque el modelo vuele por el golpe del jefe.
public class AspectoDelHeroe : MonoBehaviour
{
    public const float DiametroDelAura = 2.4f;
    public const float DiametroDeLaSombra = 1.5f;
    public const float AlturaDelAura = 0.04f;
    // El latido del aura: cuanto crece y a que ritmo.
    public const float Latido = 0.05f;
    public const float LatidosPorSegundo = 1f;

    private Transform aura;
    private Renderer renderDelAura;
    private Transform sombra;
    private float piesSobreLaRaiz;

    public void Vestir(Transform modelo)
    {
        if (modelo == null) return;
        // La altura de los pies respecto de la raiz, una vez: el modelo cuelga con los pies en
        // su pivote, y en el vuelo del golpe del jefe se levanta.
        piesSobreLaRaiz = modelo.position.y - transform.position.y;

        var remera = Resources.Load<Material>("HeroeRemeraBlanca");
        if (remera != null)
        {
            foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))
            {
                // Solo lo del pack: la pistola y el sombrero de calabaza tienen lo suyo.
                var mats = r.sharedMaterials;
                bool cambio = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || mats[i].name != "TT_demo") continue;
                    mats[i] = remera;
                    cambio = true;
                }
                if (cambio) r.sharedMaterials = mats;
            }
        }

        var materialAura = Resources.Load<Material>("AuraHeroe");
        if (materialAura != null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "AuraDelHeroe";
            Destroy(go.GetComponent<Collider>());
            aura = go.transform;
            aura.rotation = Quaternion.Euler(90f, 0f, 0f);
            aura.localScale = Vector3.one * DiametroDelAura;
            renderDelAura = go.GetComponent<Renderer>();
            renderDelAura.sharedMaterial = materialAura;
            renderDelAura.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderDelAura.receiveShadows = false;
        }

        if (SombrasFalsas.EnUso)
        {
            // Hija de la escena y no del jugador: el jugador esta a escala 0,5 y gira.
            var raiz = new GameObject("SombraDelHeroe").transform;
            sombra = SombrasFalsas.Poner(raiz, Vector3.zero, DiametroDeLaSombra);
            if (sombra == null) Destroy(raiz.gameObject);
            else sombra = raiz;
        }
        LateUpdate();
    }

    private void LateUpdate()
    {
        Vector3 pies = transform.position + Vector3.up * piesSobreLaRaiz;
        bool muerto = PlayerHealth.instance != null && PlayerHealth.instance.EstaMuerto;
        if (aura != null)
        {
            if (aura.gameObject.activeSelf == muerto) aura.gameObject.SetActive(!muerto);
            aura.position = pies + Vector3.up * AlturaDelAura;
            float latido = 1f + Latido * Mathf.Sin(Time.time * LatidosPorSegundo * 2f * Mathf.PI);
            aura.localScale = Vector3.one * (DiametroDelAura * latido);
        }
        if (sombra != null) sombra.position = pies;
    }

    private void OnDestroy()
    {
        if (aura != null) Destroy(aura.gameObject);
        if (sombra != null) Destroy(sombra.gameObject);
    }
}
