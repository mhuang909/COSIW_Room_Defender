using UnityEngine;
using System.Collections;


public class LaserEnemy : MonoBehaviour
{
    public LineRenderer linePrefab;
    private LineRenderer laser;

    [SerializeField]
    private Transform startPoint;

    private Vector3 direction;

    private float delayTime = 2.0f;

    private bool startShooting;

    public LayerMask layerMask;

    public float maxLineDistance = 7;

    public AudioSource source;
    public AudioClip laserSound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        startShooting = false;
        Vector3 targetPos = Camera.main.transform.position;
        targetPos.y -= 0.5f;
        direction = targetPos - this.transform.position;
        this.transform.rotation = Quaternion.LookRotation(direction);
        Ray ray = new Ray(this.transform.position, direction);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, maxLineDistance, layerMask);
        PlayerScript player = hit.transform.GetComponentInParent<PlayerScript> ();
        tutorialPlayer t_player = hit.transform.GetComponentInParent<tutorialPlayer> ();
        if (!player && !t_player){
            Destroy(laser);
            Destroy(gameObject);
        }
        StartCoroutine(LaserDelayCoroutine());
    }

    // Update is called once per frame
    void Update()
    {
        if (startShooting){
            CastLaser(this.transform.position, direction);
        }
    }

    void CastLaser(Vector3 position, Vector3 direction){

        for (int i = 0; i < 2; i++){
            laser.SetPosition(0, startPoint.position);
            Ray ray = new Ray(position, direction);
            bool hasHit = Physics.Raycast(ray, out RaycastHit hit, maxLineDistance, layerMask);
            check(hit);
            position = hit.point;
            direction = Vector3.Reflect(direction, hit.normal);
            laser.SetPosition(i + 1, hit.point);
        }
    }

    IEnumerator LaserDelayCoroutine()
    {
        yield return new WaitForSeconds(delayTime);
        startShooting = true;
        source.PlayOneShot(laserSound);
        laser = Instantiate(linePrefab);
        laser.positionCount = 3;
    }

    void check(RaycastHit hit){
        Enemy1 enemy = hit.transform.GetComponentInParent<Enemy1>();
        if (enemy){
            enemy.destroyCurrent();
            return;
        }

        LaserEnemy laserE = hit.transform.GetComponentInParent<LaserEnemy>();
        {
            if (laserE){
                laserE.destroyCurrent();
                return;
            }
        }

        PlayerScript player = hit.transform.GetComponentInParent<PlayerScript> ();
        if (player){
            player.laserHit();
        }
    }

    public void destroyCurrent(){
        GameCanvasScript.score += 2;
        Destroy(laser);
        Destroy(gameObject);
    }
}
