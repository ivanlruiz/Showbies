using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    

    public bool isFiring;
    public PlayerController player;
    public BulletController bala;
    public int velocidadBala;

    public float tiempoDisparo;
    private float contadorDisp;

    [Header("Mejora de cadencia")]
    public float duracionMejora = 10f;   // segundos que dura la cadencia de un pickup

    private float tiempoDisparoBase;
    private float mejoraVenceEn;
    private bool mejoraActiva;

    public AudioSource AudioSource;

    [Header("Sonido")]
    public float intervaloMinimoSonido = 0.04f;   // techo de sonidos de disparo por segundo
    private float proximoSonido;


    public Transform firePoint;
    // Start is called before the first frame update
    void Start()
    {

        AudioSource = GetComponent<AudioSource>();

        // La cadencia con la que arranca la escena es la que se recupera cuando
        // vence una mejora.
        tiempoDisparoBase = tiempoDisparo;
    }

    // Los pickups pasan por aca en vez de pisar tiempoDisparo directo. Antes la
    // mejora era permanente: agarrabas un PUArma y quedabas con cadencia x4 para
    // siempre. No acumula: el ultimo pickup pisa al anterior y reinicia el reloj.
    public void MejorarCadencia(float nuevoTiempoDisparo)
    {
        tiempoDisparo = nuevoTiempoDisparo;
        mejoraActiva = true;
        mejoraVenceEn = Time.time + duracionMejora;
    }

    // Para el indicador del HUD.
    public bool MejoraActiva { get { return mejoraActiva; } }
    public float MejoraRestante { get { return mejoraActiva ? Mathf.Max(0f, mejoraVenceEn - Time.time) : 0f; } }

    // Update is called once per frame
    void Update()
    {
        if (mejoraActiva && Time.time >= mejoraVenceEn)
        {
            mejoraActiva = false;
            tiempoDisparo = tiempoDisparoBase;
        }

        if(isFiring && player.cantBalas>0)
        {
            
            contadorDisp -= Time.deltaTime;
            if(contadorDisp <= 0)
            {
                player.cantBalas--;
                // PlayOneShot y no Play: Play reinicia el mismo sonido, y a esta
                // cadencia lo cortaba en cada tiro antes de que llegara a oirse. El
                // techo evita apilar decenas de sonidos con la cadencia mejorada.
                if (Time.time >= proximoSonido && AudioSource.clip != null)
                {
                    AudioSource.PlayOneShot(AudioSource.clip);
                    proximoSonido = Time.time + intervaloMinimoSonido;
                }
                contadorDisp = tiempoDisparo;
                // Antes era un Instantiate por disparo. Ahora las balas se reusan.
                BulletController newBullet = BulletController.Obtener(bala, firePoint.position, firePoint.rotation);
                newBullet.velocidad = velocidadBala;
            }
        } 
        else
        {
            contadorDisp = 0;
        }

        
    }

    


}