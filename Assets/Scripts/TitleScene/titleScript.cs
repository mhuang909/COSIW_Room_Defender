using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class titleScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform camera;
    public float distance = 2.0f;
    

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetPos = camera.position + camera.forward * distance;

        transform.position = targetPos;
        transform.LookAt(camera);
        transform.Rotate(0, 180, 0);
        if (OVRInput.GetDown(OVRInput.Button.One))
            SceneManager.LoadScene("TutorialScreen");


    }
}
