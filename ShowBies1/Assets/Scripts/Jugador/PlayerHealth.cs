using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerHealth : MonoBehaviour
{

    public static PlayerHealth instance;

    public AudioSource AudioSource;

    public int health;
    public int maxHealth = 10;
    public TMP_Text healthTMP;
    // Start is called before the first frame update
    void Start()
    {
        AudioSource = GetComponent<AudioSource>();
        health = maxHealth;
        
    }

    // Update is called once per frame
    void Update()
    {
        healthTMP.text = health.ToString();
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
            SceneManager.LoadScene(2);
            Destroy(gameObject);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Vida"))
        {
            Destroy(other.gameObject);
            health= 200;    
            if (health > 200)
            {
                health = 200;
            }
        }
    }
}
