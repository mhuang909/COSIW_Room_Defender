using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class obstacle : MonoBehaviour
{
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
           Enemy1 enemy = other.transform.gameObject.GetComponent<Enemy1>();
           if (enemy)
            enemy.disableEnemy();
        }
    }
}
