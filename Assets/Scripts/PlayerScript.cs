using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-9999)]
public class PlayerScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public CapsuleCollider collider;
    public Action OnHealthChange;
    public int CurrentHealth => Mathf.CeilToInt(m_CurrentHealth);
    private float m_CurrentHealth;
    public int maxHealth;

    public float laserTime = 0.5f;

    private float laserTimer;

    public Image healthBar;

    public TMP_Text healthText;

    public GameCanvasScript canvas;


    void Start()
    {
        m_CurrentHealth = maxHealth;
        laserTimer = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 camPos = Camera.main.transform.position;
        collider.center = new Vector3(camPos.x, camPos.y / 2, camPos.z);
        collider.height = camPos.y;
    }
    void OnTriggerEnter(Collider other){
        if (other.gameObject.CompareTag("Enemy")){
            Destroy(other.transform.gameObject);
            ChangeHealth(-2);
        }
    }

    public void ChangeHealth(float healthChange)
    {
        m_CurrentHealth += healthChange;
        if (m_CurrentHealth > maxHealth)
            m_CurrentHealth = maxHealth;
        if (m_CurrentHealth <= 0){
            canvas.stopTimer();
            SceneManager.LoadScene("EndScreen");
        }
        healthText.SetText($"{CurrentHealth}/{maxHealth}");
        float healthRatio = (float)CurrentHealth / maxHealth;
        healthBar.fillAmount = healthRatio;
    }

    public void laserHit(){
        laserTimer += Time.deltaTime;
        if (laserTimer >= laserTime){
            ChangeHealth(-1);
            laserTimer -= laserTime;
        }
    }
}
