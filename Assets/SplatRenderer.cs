using UnityEngine;
using Unity.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using TMPro;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SplatRenderer : MonoBehaviour
{
    public Material material;
    private Mesh mesh;

    private Vector3[] positions;
    private Vector3[] colors;
    private float[] opacities;
    private Vector3[] covariances0_3;  
    private Vector3[] covariances3_6;  

    private float lastSortTime = 0f;
    private const float SORT_INTERVAL = 1; // s

    [SerializeField]
    public string resourcePath = "23.12.2024-splatplyedited_processed_subset.csv";

    [SerializeField] private TMPro.TextMeshPro debugText;  // Changed from TextMeshProUGUI to TextMeshPro
    private float sortingTime, meshCreateTime, meshSetTime;

    private System.Threading.Tasks.Task<(int index, float dist)[]> sortingTask;
    private System.Threading.Tasks.Task<(Vector3[] vertices, Vector3[] centerPositions, Vector3[] vertexColors, 
        Vector4[] vertexOpacities, Vector3[] vertexCov0_3, Vector3[] vertexCov3_6, int[] indices)> meshArraysTask;
    private bool isSorting = false;
    private bool isCreatingMeshArrays = false;

    private System.Threading.Tasks.Task meshSetTask;
    private bool isSettingMesh = false;
    private Mesh tempMesh;

    public Transform flashlightTransform;
    public float flashlightAngle;
    public float scaleFactor = 1.0f;

    private bool scannerActive = false;
    private float scannerPos = -1000.0f;
    private float scannerWidth = 0.5f;
    private float scannerStart = -5.0f;
    private float scannerEnd = 5.0f;
    private float scanTime = 0f;
    private int scanPhase = 0; // 0: fast forward, 1: faster backward, 2: slow forward

    public Texture2D flashlightFalloffTexture;

    public void Start()
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        mesh.MarkDynamic(); 
        GetComponent<MeshFilter>().mesh = mesh;


        var meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = material;
        
        // Disable frustum culling on the renderer
        meshRenderer.forceRenderingOff = false;
        meshRenderer.allowOcclusionWhenDynamic = false;
        meshRenderer.bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
        

        tempMesh = new Mesh(); // Create a second mesh for double buffering
        tempMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        tempMesh.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        tempMesh.MarkDynamic();
        
        LoadFromCSV();
        
        // Set the initial flashlight properties
        material.SetFloat("_FlashlightAngle", flashlightAngle);
        material.SetTexture("_FlashlightFalloffTex", flashlightFalloffTexture);
        material.SetFloat("_ScaleFactor", scaleFactor);
    }
    public void LoadFromCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>(resourcePath);
        if (csvFile == null)
        {
            Debug.LogError($"Failed to load CSV file from Resources/{resourcePath}");
        }

        string[] lines = csvFile.text.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        // Only select point at index 30242
        int targetIndex = -1; // 30242;
        if (targetIndex != -1)
        {
            if (targetIndex >= lines.Length)
            {
                Debug.LogError($"Target index {targetIndex} is out of range. File only contains {lines.Length} points.");
                return;
            }
            lines = new string[] { lines[targetIndex] };
        }
        
        
        int pointCount = lines.Length;

        // Initialize arrays
        Vector3[] positions = new Vector3[pointCount];
        Vector3[] colors = new Vector3[pointCount];
        float[] opacities = new float[pointCount];
        Vector3[] covariances0_3 = new Vector3[pointCount];
        Vector3[] covariances3_6 = new Vector3[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            string[] values = lines[i].Split(',');

            // Parse position (x,y,z)
            positions[i] = new Vector3(
                float.Parse(values[0], CultureInfo.InvariantCulture),
                float.Parse(values[1], CultureInfo.InvariantCulture),
                float.Parse(values[2], CultureInfo.InvariantCulture)
            );
            // Debug.Log(positions[i]);            

            // Parse opacity
            opacities[i] = float.Parse(values[3], CultureInfo.InvariantCulture);

            // Parse colors (f_dc_0,f_dc_1,f_dc_2)
            colors[i] = new Vector3(
                float.Parse(values[4], CultureInfo.InvariantCulture),
                float.Parse(values[5], CultureInfo.InvariantCulture),
                float.Parse(values[6], CultureInfo.InvariantCulture)
            );

            // Parse covariance matrix (first 3 elements)
            covariances0_3[i] = new Vector3(
                float.Parse(values[7], CultureInfo.InvariantCulture),
                float.Parse(values[8], CultureInfo.InvariantCulture),
                float.Parse(values[9], CultureInfo.InvariantCulture)
            );

            // Parse covariance matrix (last 3 elements)
            covariances3_6[i] = new Vector3(
                float.Parse(values[10], CultureInfo.InvariantCulture),
                float.Parse(values[11], CultureInfo.InvariantCulture),
                float.Parse(values[12], CultureInfo.InvariantCulture)
            );
        }

        this.positions = positions;
        this.colors = colors;
        this.opacities = opacities;
        this.covariances0_3 = covariances0_3;
        this.covariances3_6 = covariances3_6;

        this.lastSortTime = -SORT_INTERVAL; // Force first sort
    }

    private bool flashlightOn = false;
    private void Update()
    {
        /*
        if (flashlightTransform != null)
        {
           //if (OVRInput.GetDown(OVRInput.RawButton.A))
           // {
           //     flashlightOn = !flashlightOn;
           // }   
        }

        if (flashlightOn)
        {
            Matrix4x4 flashlightWorldToLocal = flashlightTransform.worldToLocalMatrix;
            material.SetMatrix("_FlashlightWorldToLocal", flashlightWorldToLocal);
            material.SetFloat("_FlashlightAngle", flashlightAngle);
        } else
        {
            material.SetFloat("_FlashlightAngle", 0.0f);
        }

        // Scanner effect control
        if (OVRInput.GetDown(OVRInput.RawButton.Y))
        {
            scannerActive = true;
            scanPhase = 0;
            scanTime = 0f;
            scannerPos = scannerStart;
        }
        */

        if (scannerActive)
        {
            scanTime += Time.deltaTime;
            
            switch (scanPhase)
            {
                case 0: // Fast forward (3 m/s)
                    scannerPos += 3f * Time.deltaTime;
                    if (scannerPos >= scannerEnd)
                    {
                        scanPhase = 1;
                        scannerPos = scannerEnd;
                    }
                    break;
                    
                case 1: // Faster backward (9 m/s)
                    scannerPos -= 9f * Time.deltaTime;
                    if (scannerPos <= scannerStart)
                    {
                        scanPhase = 2;
                        scannerPos = scannerStart;
                    }
                    break;
                    
                case 2: // Slow forward (1 m/s)
                    scannerPos += 2f * Time.deltaTime;
                    if (scannerPos >= scannerEnd)
                    {
                        scannerActive = false;
                        scanPhase = 0;
                    }
                    break;
            }

            // Update shader properties
            material.SetFloat("_ScannerActive", scannerActive ? 1f : 0f);
            material.SetFloat("_ScannerPos", scannerPos);
            material.SetFloat("_ScannerWidth", scannerWidth);
        }
        else
        {
            material.SetFloat("_ScannerActive", 0f);
        }

        // Check if we need to start a new update cycle based on time
        if (Time.time - lastSortTime >= SORT_INTERVAL && !isSorting && !isCreatingMeshArrays && !isSettingMesh)
        {
            UpdateMesh();
            lastSortTime = Time.time;
        }
        // Check if any ongoing tasks have completed
        else if ((isSorting && sortingTask?.IsCompleted == true) || 
                 (isCreatingMeshArrays && meshArraysTask?.IsCompleted == true) ||
                 (isSettingMesh && meshSetTask?.IsCompleted == true))
        {
            UpdateMesh();
        }
    }

    private void UpdateMesh()
    {
        if (positions == null) return;

        var startTime = Time.realtimeSinceStartup;
        var cameraPos = Camera.main.transform.position;
        var worldMatrix = transform.localToWorldMatrix; // Store the transform matrix

        // Debug.Log(Camera.main.projectionMatrix);
        // verified standard matrix notation
        // 1.35800	0.00000	 0.00000  0.00000
        // 0.00000  2.41421  0.00000  0.00000
        // 0.00000  0.00000 -1.00200 -0.20020
        // 0.00000  0.00000 -1.00000  0.00000

        //Debug.Log(GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false));
        // verified standard matrix notation
        // 1.35800	0.00000	 0.00000  0.00000
        // 0.00000  2.41421  0.00000  0.00000
        // 0.00000  0.00000  0.00100  0.10010
        // 0.00000  0.00000 -1.00000  0.00000

        if (!isSorting && !isCreatingMeshArrays && !isSettingMesh)
        {
            isSorting = true;
            sortingTask = System.Threading.Tasks.Task.Run(() =>
            {
                var sortingArray = new (int index, float dist)[positions.Length];
                
                for (int i = 0; i < positions.Length; i++)
                {
                    Vector3 worldPos = worldMatrix.MultiplyPoint3x4(positions[i]);
                    float distSqr = (worldPos - cameraPos).sqrMagnitude;
                    sortingArray[i] = (i, distSqr);
                }
                
                System.Array.Sort(sortingArray, (a, b) => b.dist.CompareTo(a.dist));
                return sortingArray;
            });
            return;
        }

        // If sorting is complete, start mesh array creation
        if (isSorting && sortingTask.IsCompleted && !isCreatingMeshArrays)
        {
            var sortingArray = sortingTask.Result;
            isSorting = false;
            isCreatingMeshArrays = true;
            sortingTime = (Time.realtimeSinceStartup - startTime) * 1000f;

            startTime = Time.realtimeSinceStartup;
            meshArraysTask = System.Threading.Tasks.Task.Run(() => CreateMeshArrays(sortingArray));
            return;
        }

        // If mesh arrays creation is complete, handle platform-specific mesh setting
        if (isCreatingMeshArrays && meshArraysTask.IsCompleted && !isSettingMesh)
        {
            var meshArrays = meshArraysTask.Result;
            isCreatingMeshArrays = false;
            meshCreateTime = (Time.realtimeSinceStartup - startTime) * 1000f;

            startTime = Time.realtimeSinceStartup;
            
            bool isQuestPlatform = Application.platform == RuntimePlatform.Android && !Application.isEditor;
            
            if (isQuestPlatform)
            {
                // Quest device: Use async mesh setting with double buffering
                isSettingMesh = true;
                meshSetTask = System.Threading.Tasks.Task.Run(() => 
                {
                    tempMesh.vertices = meshArrays.vertices;
                    tempMesh.SetUVs(1, meshArrays.centerPositions);
                    tempMesh.SetUVs(2, meshArrays.vertexColors);
                    tempMesh.SetUVs(3, meshArrays.vertexOpacities);
                    tempMesh.SetUVs(5, meshArrays.vertexCov0_3);
                    tempMesh.SetUVs(6, meshArrays.vertexCov3_6);
                    tempMesh.SetIndices(meshArrays.indices, MeshTopology.Triangles, 0);
                    tempMesh.RecalculateBounds();
                });
            }
            else
            {
                /*Debug.Log("Center Positions: " + string.Join(", ", meshArrays.centerPositions.Select(pos => pos.ToString("F8")).ToArray()));
                Debug.Log("Colors: " + string.Join(", ", meshArrays.vertexColors.Select(color => color.ToString("F8")).ToArray()));
                Debug.Log("Opacities: " + string.Join(", ", meshArrays.vertexOpacities.Select(opacity => opacity.ToString("F8")).ToArray()));
                Debug.Log("Covariance 0-3: " + string.Join(", ", meshArrays.vertexCov0_3.Select(cov => cov.ToString("F8")).ToArray()));
                Debug.Log("Covariance 3-6: " + string.Join(", ", meshArrays.vertexCov3_6.Select(cov => cov.ToString("F8")).ToArray()));
                */
                // Desktop: Set mesh data immediately
                mesh.vertices = meshArrays.vertices;
                mesh.SetUVs(1, meshArrays.centerPositions);
                mesh.SetUVs(2, meshArrays.vertexColors);
                mesh.SetUVs(3, meshArrays.vertexOpacities);
                mesh.SetUVs(5, meshArrays.vertexCov0_3);
                mesh.SetUVs(6, meshArrays.vertexCov3_6);
                mesh.SetIndices(meshArrays.indices, MeshTopology.Triangles, 0);
                mesh.RecalculateBounds();

                meshSetTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                if (debugText != null)
                {
                    debugText.text = $"Sort: {sortingTime:F1}ms\nCreate: {meshCreateTime:F1}ms\nSet: {meshSetTime:F1}ms";
                }
            }
            return;
        }

        // Handle mesh swap for Quest platform
        bool isQuestDevice = Application.platform == RuntimePlatform.Android && !Application.isEditor;
        if (isQuestDevice && isSettingMesh && meshSetTask.IsCompleted)
        {
            isSettingMesh = false;
            meshSetTime = (Time.realtimeSinceStartup - startTime) * 1000f;

            // Swap meshes on main thread
            var temp = mesh;
            mesh = tempMesh;
            tempMesh = temp;
            GetComponent<MeshFilter>().mesh = mesh;

            if (debugText != null)
            {
                debugText.text = $"Sort: {sortingTime:F1}ms\nCreate: {meshCreateTime:F1}ms\nSet: {meshSetTime:F1}ms";
            }
        }
    }

    private (Vector3[] vertices, Vector3[] centerPositions, Vector3[] vertexColors, 
             Vector4[] vertexOpacities, Vector3[] vertexCov0_3, Vector3[] vertexCov3_6, 
             int[] indices) 
        CreateMeshArrays((int index, float dist)[] sortingArray)
    {
        int pointCount = sortingArray.Length;
        int vertexCount = pointCount * 4;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] centerPositions = new Vector3[vertexCount];
        Vector3[] vertexColors = new Vector3[vertexCount];
        Vector4[] vertexOpacities = new Vector4[vertexCount];
        Vector3[] vertexCov0_3 = new Vector3[vertexCount];
        Vector3[] vertexCov3_6 = new Vector3[vertexCount];

        // Create vertices (4 per point)
        for (int i = 0; i < pointCount; i++)
        {
            int baseIndex = i * 4;
            
            // Create vertices for a quad
            vertices[baseIndex + 0] = new Vector3(-1, -1, 0);
            vertices[baseIndex + 1] = new Vector3( 1, -1, 0);
            vertices[baseIndex + 2] = new Vector3(-1,  1, 0);
            vertices[baseIndex + 3] = new Vector3( 1,  1, 0);

            // Store data for each vertex
            for (int j = 0; j < 4; j++)
            {
                centerPositions[baseIndex + j] = positions[sortingArray[i].index];
                vertexColors[baseIndex + j] = colors[sortingArray[i].index];
                vertexOpacities[baseIndex + j] = new Vector4(opacities[sortingArray[i].index], 0, 0, 0);
                vertexCov0_3[baseIndex + j] = covariances0_3[sortingArray[i].index];
                vertexCov3_6[baseIndex + j] = covariances3_6[sortingArray[i].index];
            }
        }

        // Create indices for triangles
        int[] indices = new int[pointCount * 6];  // 2 triangles = 6 indices
        for (int i = 0; i < pointCount; i++)
        {
            int baseIndex = i * 6;
            int baseVertex = i * 4;
            
            // First triangle
            indices[baseIndex + 0] = baseVertex + 0; // Bottom left
            indices[baseIndex + 1] = baseVertex + 2; // Top left
            indices[baseIndex + 2] = baseVertex + 1; // Bottom right
            
            // Second triangle
            indices[baseIndex + 3] = baseVertex + 2; // Top left
            indices[baseIndex + 4] = baseVertex + 3; // Top right
            indices[baseIndex + 5] = baseVertex + 1; // Bottom right
        }

        return (vertices, centerPositions, vertexColors, vertexOpacities, vertexCov0_3, vertexCov3_6, indices);
    }

    private void OnDestroy()
    {
        if ((isSorting && sortingTask != null) || 
            (isCreatingMeshArrays && meshArraysTask != null) || 
            (isSettingMesh && meshSetTask != null))
        {
            try
            {
                sortingTask?.Wait();
                meshArraysTask?.Wait();
                meshSetTask?.Wait();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error waiting for background tasks to complete: {e}");
            }
        }
        
        if (mesh != null) Destroy(mesh);
        if (tempMesh != null) Destroy(tempMesh);
    }
}
