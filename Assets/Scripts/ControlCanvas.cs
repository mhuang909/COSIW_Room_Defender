using UnityEngine;

public class ControlCanvas : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public Transform camera;
    public float distance = 2.0f;

    public GameObject image;

    private bool active;
    void Start()
    {
        active = false;
    }

    // Update is called once per frame
    void Update()
    {

        Vector3 targetPos = camera.position + camera.forward * distance;

        transform.position = targetPos;
        transform.LookAt(camera);
        transform.Rotate(0, 180, 0);
        if (OVRInput.GetDown(OVRInput.Button.One)){
            active = !active;
            image.SetActive(active);
        }
    }
}
