using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GameCanvasScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform camera;
    public float distance = 2.0f;

    public float currentTime;

    public static float finalTime;

    public static int score;

    private bool activeTimer;

    public TMP_Text timerText;
    void Start()
    {
        currentTime = 0.0f;
        activeTimer = true;
        score = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetPos = camera.position + camera.forward * distance;

        transform.position = targetPos;
        transform.LookAt(camera);
        transform.Rotate(0, 180, 0);
        if (activeTimer){
            currentTime += Time.deltaTime;
            TimeSpan time = TimeSpan.FromSeconds(currentTime);
            timerText.text = score.ToString();
        }


    }

    public void stopTimer(){
        activeTimer = false;
        finalTime = currentTime;
    }
}
