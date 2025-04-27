using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class tutorialPlayer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public CapsuleCollider collider;


    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 camPos = Camera.main.transform.position;
        collider.center = new Vector3(camPos.x, camPos.y / 2, camPos.z);
        collider.height = camPos.y;
    }

}
