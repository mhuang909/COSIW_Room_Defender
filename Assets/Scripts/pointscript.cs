using UnityEngine;

public class pointscript : MonoBehaviour
{   
    public AudioSource staffSource;
    public AudioClip staffHit;
    public AudioClip powerupHit;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnTriggerEnter(Collider other){
        if (other.gameObject.CompareTag("Enemy")){
            Enemy1 enemy = other.GetComponentInParent<Enemy1>();
            if (enemy){
                staffSource.PlayOneShot(staffHit);
                enemy.destroyCurrent();
                return;
            }

            LaserEnemy laserE = other.GetComponentInParent<LaserEnemy>();
            if (laserE){
                staffSource.PlayOneShot(staffHit);
                laserE.destroyCurrent();
                return;
            }
            
        }
        if (other.gameObject.CompareTag("healthPowerup")){
            staffSource.PlayOneShot(powerupHit);
            PowerUpScript.healthAmount += 1;
            Destroy(other.transform.parent.gameObject);
        }
        if (other.gameObject.CompareTag("staffPowerup")){
            staffSource.PlayOneShot(powerupHit);
            PowerUpScript.barrierTime += 3;
            Destroy(other.transform.parent.gameObject);
        }
    }
}
