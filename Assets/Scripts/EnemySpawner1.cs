using UnityEngine;
using Meta.XR.MRUtilityKit;

public class EnemySpawner1 : MonoBehaviour
{

    public float spawnTimer = 1;
    private float timer;
    public GameObject SpawnObject;

    public MRUKAnchor.SceneLabels spawnLabels;


    public int MaxIterations = 1000;

    public LayerMask LayerMask = -1;

    public float SurfaceClearanceDistance = 0.1f;

    public float distToPlayer = 0.5f;

    private float waveTimer;

    public float waveTime = 15.0f;

    public float waveIncrease = 0.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timer = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (!MRUK.Instance && !MRUK.Instance.IsInitialized)
            return;
        if(GameObject.FindGameObjectsWithTag("Enemy").Length > 70) 
            return;
        timer += Time.deltaTime;
        if (timer > spawnTimer){
            StartSpawn();
            timer -= spawnTimer;
        }
        waveTimer += Time.deltaTime;
        if (waveTimer >= waveTime && spawnTimer >= 0.15){
            spawnTimer *= waveIncrease;
            waveTimer -= waveTime;

        }
    }

    public void StartSpawn()
    {
        MRUKRoom room = MRUK.Instance.GetCurrentRoom();
        var prefabBounds = Utilities.GetPrefabBounds(SpawnObject);
        float minRadius = 0.0f;
        const float clearanceDistance = 0.01f;
        float baseOffset = -prefabBounds?.min.y ?? 0.0f;
        float centerOffset = prefabBounds?.center.y ?? 0.0f;
        Bounds adjustedBounds = new();

        if (prefabBounds.HasValue)
        {
            minRadius = Mathf.Min(-prefabBounds.Value.min.x, -prefabBounds.Value.min.z, prefabBounds.Value.max.x, prefabBounds.Value.max.z);
            if (minRadius < 0f)
            {
                minRadius = 0f;
            }

            var min = prefabBounds.Value.min;
            var max = prefabBounds.Value.max;
            min.y += clearanceDistance;
            if (max.y < min.y)
            {
                max.y = min.y;
            }

            adjustedBounds.SetMinMax(min, max);
        }

        bool foundValidSpawnPosition = false;
        for (int j = 0; j < MaxIterations; j++)
        {
            Vector3 spawnPosition = Vector3.zero;
            Vector3 spawnNormal = Vector3.zero;
            MRUK.SurfaceType surfaceType = 0;
            surfaceType |= MRUK.SurfaceType.FACING_UP;
            surfaceType |= MRUK.SurfaceType.VERTICAL;
            surfaceType |= MRUK.SurfaceType.FACING_DOWN;

            if (room.GenerateRandomPositionOnSurface(surfaceType, minRadius, new LabelFilter(spawnLabels), out var pos, out var normal))
            {
                spawnPosition = pos + normal * baseOffset;
                spawnNormal = normal;
                var center = spawnPosition + normal * centerOffset;
                Vector3 targetPos = Camera.main.transform.position;
                targetPos.y = 0;

                // In some cases, surfaces may protrude through walls and end up outside the room
                // check to make sure the center of the prefab will spawn inside the room
                if (!room.IsPositionInRoom(center))
                {
                    continue;
                }

                // Ensure the center of the prefab will not spawn inside a scene volume
                if (room.IsPositionInSceneVolume(center))
                {
                    continue;
                }

                // Also make sure there is nothing close to the surface that would obstruct it
                if (room.Raycast(new Ray(pos, normal), SurfaceClearanceDistance, out _))
                {
                    continue;
                }
                if (Vector3.Distance(targetPos, spawnPosition) < distToPlayer)
                {
                    continue;
                }
            }
                

            Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, spawnNormal);
            if (prefabBounds.HasValue)
            {
                if (Physics.CheckBox(spawnPosition + spawnRotation * adjustedBounds.center, adjustedBounds.extents, spawnRotation, LayerMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                } 
            }

            foundValidSpawnPosition = true;

            if (SpawnObject.gameObject.scene.path == null)
            {
                GameObject enemy = Instantiate(SpawnObject, spawnPosition, spawnRotation, transform);
 
                // UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                // if (agent != null)
                // {
                //     agent.enabled = false; // Disable initially
                // }

                // // Delay enabling NavMeshAgent
                // enemy.GetComponent<MonoBehaviour>().StartCoroutine(EnableNavMeshAfterFrame(agent));
            }
            else
            {
                SpawnObject.transform.position = spawnPosition;
                SpawnObject.transform.rotation = spawnRotation;
                return; // ignore SpawnAmount once we have a successful move of existing object in the scene
            }
            break;
        }

        if (!foundValidSpawnPosition)
        {
            Debug.LogWarning($"Failed to find valid spawn position after {MaxIterations} iterations.");
        }
    }

    // System.Collections.IEnumerator EnableNavMeshAfterFrame(UnityEngine.AI.NavMeshAgent agent)
    // {
    //     yield return new WaitForEndOfFrame(); // Wait a frame to allow positioning
    //     Debug.Log("reached inside iemuerator");
    //     if (agent != null)
    //     {
    //         Debug.Log("inside agent enabler");
    //         agent.enabled = true; // Enable after the enemy is correctly placed
    //     }
    //     else {
    //         Debug.LogWarning("Error");
    //     }
    // }
}
