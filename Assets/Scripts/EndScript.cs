using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using TMPro;

public class EndScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform camera;
    public float distance = 2.0f;


    public TMP_Text timerText;
    
    public TMP_Text scoreText;
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
        float finalTime = GameCanvasScript.finalTime;
        TimeSpan time = TimeSpan.FromSeconds(finalTime);
        string timeText = time.Minutes.ToString() + " Minutes " + time.Seconds.ToString() + " Seconds";;
        timerText.text = "You survived for " + timeText;
        scoreText.text = "Your score was: " + GameCanvasScript.score.ToString();

        if (OVRInput.GetDown(OVRInput.Button.One))
            SceneManager.LoadScene("TitleScreen");


    }
}
