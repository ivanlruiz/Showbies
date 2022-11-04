using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{

    public static PlayerHealth instance;

    public AudioSource AudioSource;

    public int health;
    public int maxHealth = 10;
    // Start is called before the first frame update
    void Start()
    {
        AudioSource = GetComponent<AudioSource>();
        health = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void Awake()
    {
        instance = this;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if(health <= 0)
        {
        AudioSource.Play();

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            
            Destroy(gameObject);
        }
    }
}
