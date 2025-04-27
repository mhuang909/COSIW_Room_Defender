using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CanvasScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform camera;
    public float distance = 2.0f;
    public float currentTime;

    public bool isLock;

    void Start()
    {
        isLock = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isLock){
            Vector3 targetPos = camera.position + camera.forward * distance;

            transform.position = targetPos;
            transform.LookAt(camera);
            transform.Rotate(0, 180, 0);
        }
        
        if(OVRInput.GetDown(OVRInput.Button.Three)){
            isLock = !isLock;
        }


    }
}
