using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created    void Start()
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
            SceneManager.LoadScene("TutorialScene");

    }
}
