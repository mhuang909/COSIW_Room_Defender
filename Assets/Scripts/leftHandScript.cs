using UnityEngine;

public class leftHandScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject [] items;

    public OVRInput.RawButton activeButton;

    public OVRInput.RawButton switchButton;

    public PlayerScript playerScript;

    public float healTimer = 1.0f;

    private float timer;

    private int currentItem;

    public bool enabled;
    void Start()
    {
        currentItem = 0;
        timer = 0.0f;
        enabled = true;
        items[1].SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.Get(activeButton)){
            activate(currentItem);
            enabled = false;
        }
        else
            enabled = true;

        if (OVRInput.GetDown(switchButton)){
            items[currentItem].SetActive(false);
            currentItem = (currentItem + 1) % 2;
            items[currentItem].SetActive(true);
        }
    }

    public void activate(int item){
        if (item == 1){ 
            timer += Time.deltaTime;
            if (timer >= healTimer){
                playerScript.ChangeHealth(PowerUpScript.healthAmount);
                timer -= healTimer;
            }
        }
    }


}
