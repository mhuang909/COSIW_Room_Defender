using UnityEngine;
using Meta.XR.MRUtilityKit;

public class GhostSpawner : MonoBehaviour
{

    public float spawnTimer = 1;
    private float timer;
    public GameObject prefabToSpawn;

    public MRUKAnchor.SceneLabels spawnLabels;

    public float minEdgeDistance = 0.3f;

    public int spawnTry = 1000;

    public float normalOffset;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!MRUK.Instance && !MRUK.Instance.IsInitialized)
            return;
        timer += Time.deltaTime;
        if (timer > spawnTimer){
            SpawnGhost();
            timer -= spawnTimer;
        }
    }

    public void SpawnGhost(){
        MRUKRoom room = MRUK.Instance.GetCurrentRoom();

        int currentTry = 0;
        while (currentTry < spawnTry){
            bool hasFoundPosition = room.GenerateRandomPositionOnSurface(MRUK.SurfaceType.VERTICAL, minEdgeDistance,LabelFilter.Included(spawnLabels), out Vector3 pos, out Vector3 norm);
            
            if (hasFoundPosition){
                Vector3 randomPositionNormalOffset = pos + norm * normalOffset;
                randomPositionNormalOffset.y = 0;
                Instantiate(prefabToSpawn, randomPositionNormalOffset, Quaternion.identity);
                return;
            }
            else
                currentTry++;
        }
    }
}
