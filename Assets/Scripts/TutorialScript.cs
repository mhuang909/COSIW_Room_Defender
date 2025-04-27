using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject staff;
    public GameObject leftHand;

    public GameObject[] slides;

    public GameObject leftControl;
    public GameObject rightControl;
    public Transform camera;
    public float distance = 2.0f;
    
    public GameObject floorEnemy;

    public GameObject laserEnemy;

    public GameObject staffPowerup;

    public GameObject HealthPowerup;
    private int slideIndex;

    public AudioSource slideSource;
    public AudioClip slideTransition;

    void Start()
    {
        slideIndex = 0;

    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Two)){
            SceneManager.LoadScene("GameScreen");
        }

        if (OVRInput.GetDown(OVRInput.Button.One)){
            loadScreen();
        }
    }

    void loadScreen(){
        slideSource.PlayOneShot(slideTransition);
        if (slideIndex == slides.Length - 1)
            SceneManager.LoadScene("GameScreen");
        slides[slideIndex].SetActive(false);
        slideIndex++;
        slides[slideIndex].SetActive(true);
        if (slideIndex == 2)
            staff.SetActive(true);
        if (slideIndex == 4)
            leftHand.SetActive(true);

        if (slideIndex == 7){
            floorEnemy.SetActive(true);
            laserEnemy.SetActive(true);
        }
        else {
            floorEnemy.SetActive(false);
            laserEnemy.SetActive(false);
        }

        if (slideIndex == 3)
            rightControl.SetActive(true);
        else
            rightControl.SetActive(false);
        if (slideIndex == 5)
            leftControl.SetActive(true);
        else
            leftControl.SetActive(false);
        
        if (slideIndex == 6){
            HealthPowerup.SetActive(true);
            staffPowerup.SetActive(true);
        }
        else {
            HealthPowerup.SetActive(false);
            staffPowerup.SetActive(false);
        }

    }
}
