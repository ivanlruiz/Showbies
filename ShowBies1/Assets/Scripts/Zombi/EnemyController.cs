using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public TransitionsZM transZM;

    private int vidaActual;
    private Rigidbody rb;
    [Header("Unity Setup")]
    public ParticleSystem deathParticles;
    public Enemy enemyType;
    public PlayerController thePlayer;
    public GameObject manchaDeSangrePrefab;
    public Texture2D[] sangreSprites;
   
    void Start()
    {
        GameObject manchaDeSangre = Instantiate(manchaDeSangrePrefab, new Vector3(1000f, 1000f, 1000f), Quaternion.identity);
        manchaDeSangre.SetActive(false);

        vidaActual = enemyType.hp;
        rb = GetComponent<Rigidbody>();
        thePlayer = FindObjectOfType<PlayerController>();
    }
    private void FixedUpdate()
    {
       if (thePlayer == null) return;
       transform.LookAt(thePlayer.transform.position);
       rb.velocity = (transform.forward * enemyType.velocidad);
}

    void Update()
    { }
    public void DanoZombi(int daño)
    {
        vidaActual -= daño;

        if (vidaActual <= 0)        
        {
            /*GameObject manchaDeSangre = Instantiate(bloodstainPrefab, transform.position, Quaternion.identity);
            manchaDeSangre.SetActive(true);
            manchaDeSangre.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            manchaDeSangre.transform.position = new Vector3(transform.position.x, 0f, transform.position.z);*/
            
            
            GameObject manchaDeSangre = Instantiate(manchaDeSangrePrefab, transform.position, Quaternion.identity);
            manchaDeSangre.SetActive(true);
            manchaDeSangre.transform.position = transform.position;
            Texture2D spriteSangreAleatorio = sangreSprites[Random.Range(0, sangreSprites.Length)];
            manchaDeSangre.GetComponent<SpriteRenderer>().sprite = Sprite.Create(spriteSangreAleatorio, new Rect(0f, 0f, spriteSangreAleatorio.width, spriteSangreAleatorio.height), new Vector2(0.5f, 0.5f), 100f);
            manchaDeSangre.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            manchaDeSangre.transform.position = new Vector3(transform.position.x, .1f, transform.position.z);

            StartCoroutine(DestruirManchaDeSangre(manchaDeSangre));

            IEnumerator DestruirManchaDeSangre(GameObject manchaDeSangre)
            {
                yield return new WaitForSeconds(2f);
                manchaDeSangre.SetActive(false);
                Destroy(manchaDeSangre);
            }

            Instantiate(deathParticles, transform.position, Quaternion.identity);
          Destroy(gameObject);
          Puntaje.instance.contadorKill++;
          Puntaje.instance.UpdateKillCounterUI();
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            PlayerHealth.instance.TakeDamage(enemyType.daño);
        }
    }


}
