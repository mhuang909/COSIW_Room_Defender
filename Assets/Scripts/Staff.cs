using UnityEngine;
using UnityEngine.InputSystem;

public class Staff : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public OVRInput.RawButton shootingButton;

    public OVRInput.RawButton switchButton;
    public LayerMask layerMaskDestroy;
    public LayerMask layerMaskBarrier;
    public LineRenderer linePrefab;
    public Transform shootingPoint;

    public Transform RayPoint;
    public float maxLineDistance = 7;
    public float lineShowTimer = 0.3f;
    public GameObject ImpactPrefab;

    public Animator staffAnimator;

    private bool isDestroy;

    private bool animatingDestroy = false;

    private bool animatingBarrier = false;

    private Vector3 original = new Vector3(1.0f, 1.0f, 1.0f);

    public leftHandScript lScript;

    public AudioSource powerupSource;
    public AudioClip powerupHit;

    public AudioSource staffSource;
    public AudioClip staffHit;
    public AudioClip staffSpin;

    void Start()
    {
        isDestroy = false;
        print(PowerUpScript.barrierTime);
    }

    // Update is called once per frame
    void Update()
    {
        if (!lScript.enabled)
            return;
        if (OVRInput.Get(shootingButton)){
            shoot();
        }
        else if (OVRInput.GetDown(switchButton)){
            staffSource.PlayOneShot(staffSpin);
            if (!animatingBarrier && !animatingDestroy){
                if (isDestroy){
                    animatingBarrier = true;
                    staffAnimator.SetTrigger("SpinBarrier");
                }
                else {
                    animatingDestroy = true;
                    staffAnimator.SetTrigger("SpinDestroy");
                }
                isDestroy = !isDestroy;
            }
        }
        else {
            this.transform.localScale = original;
        }
    }
    public void shoot(){
        Ray ray = new Ray(RayPoint.position, RayPoint.forward);
        if (isDestroy){
            bool hasHit = Physics.Raycast(ray, out RaycastHit hit, maxLineDistance, layerMaskDestroy);
            if (hasHit && hit.distance < 5){
                this.transform.localScale = new Vector3 (1.0f, hit.distance + 0.5f, 1.0f);
                Enemy1 enemy = hit.transform.GetComponentInParent<Enemy1>();
                if (enemy){
                    staffSource.PlayOneShot(staffHit);
                    enemy.destroyCurrent();
                }
                LaserEnemy enemyL = hit.transform.GetComponentInParent<LaserEnemy>();
                if (enemyL){
                    staffSource.PlayOneShot(staffHit);
                    enemyL.destroyCurrent();
                }
                if (hit.transform.CompareTag("healthPowerup")){
                    powerupSource.PlayOneShot(powerupHit);
                    PowerUpScript.healthAmount += 1;
                    Destroy(hit.transform.parent.gameObject);
                }
                if (hit.transform.CompareTag("staffPowerup")){
                    powerupSource.PlayOneShot(powerupHit);
                    PowerUpScript.barrierTime += 3.0f;
                    Destroy(hit.transform.parent.gameObject);
                }
            }
        }
        else {
            bool hasHit = Physics.Raycast(ray, out RaycastHit hit, maxLineDistance, layerMaskBarrier);
            if (hasHit && !hit.transform.GetComponentInParent<obstacle>() && !hit.transform.GetComponentInParent<Enemy1>()&& !hit.transform.GetComponentInParent<LaserEnemy>()){
                    Vector3 endPoint = hit.point;
                    Quaternion ImpactRotation = Quaternion.LookRotation(-hit.normal);
                    GameObject Impact = Instantiate (ImpactPrefab, endPoint, ImpactRotation);
                    Destroy(Impact, PowerUpScript.barrierTime);
                    LineRenderer line = Instantiate(linePrefab);
                    line.positionCount = 2;
                    line.SetPosition(0, shootingPoint.position);
                    line.SetPosition(1, hit.point);
                    Destroy(line.gameObject, lineShowTimer);
            }
        }
    }

    public void setDestroyAnimation(){
        animatingDestroy = false;
    }
    public void setBarrierAnimation(){
        animatingBarrier = false;
    }
}
