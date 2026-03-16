using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("List of prefabs that can be spawned")]
    public List<GameObject> spawnablePrefabs;

    [Tooltip("Transform points where objects can spawn")]
    public List<Transform> spawnPoints;

    [Tooltip("Maximum number of spawned objects allowed at once")]
    public int maxSpawnedObjects = 10;
    public bool spawning = false;
    public bool isRunning = false;

    [Tooltip("Time between spawn attempts in seconds")]
    public float spawnRate = 2.0f;
    public float spawnCount = 0;

    [Tooltip("Whether prefabs should be selected randomly or sequentially")]
    public bool useRandomPrefabs = true;

    [Tooltip("Whether spawn points should be used randomly or sequentially when not using occupancy")]
    public bool useRandomSpawnPoints = true;

    [Tooltip("Whether spawn points can only be used once until their spawned object is destroyed")]
    public bool enforceSpawnPointOccupancy = true;

    [Tooltip("Link between spawn and score")]
    public bool linkSpawnToScore = false;


    [Header("Debug")]
    [SerializeField] private int currentSpawnedObjectsCount = 0;
    [SerializeField] private int nextPrefabIndex = 0;
    [SerializeField] private int availableSpawnPoints = 0;

    // Dictionary to track which spawn points are occupied and by which objects
    private Dictionary<Transform, GameObject> spawnPointOccupancy = new Dictionary<Transform, GameObject>();

    // List to keep track of all spawned objects
    private List<GameObject> spawnedObjects = new List<GameObject>();

    // Flag to control spawning
    private bool canSpawn = true;

    public Coroutine spawnCoroutine;

    void Start()
    {
        // Validate that we have prefabs and spawn points
        if (spawnablePrefabs.Count == 0)
        {
            Debug.LogError("No prefabs assigned to the spawner!");
            return;
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points assigned to the spawner!");
            return;
        }

        // Initialize all spawn points as available
        InitializeSpawnPoints();

        // Start the spawning coroutine
        //spawnCoroutine = StartCoroutine(SpawnRoutine());

    }

    void Update()
    {
        //Debug.Log("GAME STATE: " + GameManager.Instance.CurrentState);
        //Debug.Log("canSpawn: " + canSpawn);

        //if (isRunning == false)
        //{
            //isRunning = true;
            //spawnCoroutine = StartCoroutine(SpawnRoutine());
        //}

        if (GameManager.Instance.CurrentState == GameState.Playing)
        {   //only update if playing

            spawnSequence();

            // Clean up the list by removing null references (destroyed objects)
            //CleanupSpawnedObjectsList();

            // Update the counter for debug purposes
            currentSpawnedObjectsCount = spawnedObjects.Count;
            availableSpawnPoints = CountAvailableSpawnPoints();

            if(linkSpawnToScore) crowdOnScore();

        }
    }

    public void crowdOnScore()
    {
        int globalScore = ResourceManager.getGlobalCapital();
        maxSpawnedObjects = globalScore * 10;
    }

    private void InitializeSpawnPoints()
    {
        // Clear the dictionary
        spawnPointOccupancy.Clear();

        // Initialize all spawn points as unoccupied (null value)
        foreach (Transform spawnPoint in spawnPoints)
        {
            spawnPointOccupancy[spawnPoint] = null;
        }
    }

    private int CountAvailableSpawnPoints()
    {
        int count = 0;
        foreach (var kvp in spawnPointOccupancy)
        {
            if (kvp.Value == null)
            {
                count++;
            }
        }
        return count;
    }

    public void spawnSequence()
    {
        spawnCount += Time.deltaTime;
        //Debug.Log("RUNNING");

        if (spawnCount > spawnRate)
        {
            //Debug.Log("SPAWN TICK!");
            if (spawnedObjects.Count < maxSpawnedObjects && HasAvailableSpawnPoints())
            {
                //Debug.Log("GAME STATE: " + GameManager.Instance.CurrentState);
                if (GameManager.Instance.CurrentState == GameState.Playing)
                {
                    SpawnObject();
                }
            }
            spawnCount = 0;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        
        while (canSpawn)
        {
            Debug.Log("RUNNING");

            // Wait for the spawn rate duration
            yield return new WaitForSeconds(spawnRate);

            //SpawnObject();
            // Check if we can spawn more objects and have available spawn points
            if (spawnedObjects.Count < maxSpawnedObjects && HasAvailableSpawnPoints())
            {
                //Debug.Log("GAME STATE: " + GameManager.Instance.CurrentState);
                if (GameManager.Instance.CurrentState == GameState.Playing)
                {
                    SpawnObject();
                }  
            }
        }
    }

    private bool HasAvailableSpawnPoints()
    {
        // If not enforcing occupancy, we can always spawn if there are any spawn points
        if (!enforceSpawnPointOccupancy)
        {
            return spawnPoints.Count > 0;
        }

        // If enforcing occupancy, check for available (unoccupied) spawn points
        foreach (var kvp in spawnPointOccupancy)
        {
            if (kvp.Value == null)
            {
                return true;
            }
        }
        return false;
    }

    private void SpawnObject()
    {
        // Select a spawn point based on our settings
        Transform selectedSpawnPoint = SelectSpawnPoint();

        // Select prefab
        GameObject selectedPrefab = SelectPrefab();

        // Spawn the object at the chosen spawn point
        if (selectedSpawnPoint != null && selectedPrefab != null)
        {
            GameObject spawnedObject = Instantiate(
                selectedPrefab,
                selectedSpawnPoint.position,
                selectedSpawnPoint.rotation
            );

            // Add to our tracking list
            spawnedObjects.Add(spawnedObject);

            // Mark the spawn point as occupied if we're using that feature
            if (enforceSpawnPointOccupancy)
            {
                spawnPointOccupancy[selectedSpawnPoint] = spawnedObject;
            }

            // Optionally tag the object for easier reference
            spawnedObject.tag = "Spawned";

            // Add a component to the spawned object to track its spawn point
            SpawnedObject tracker = spawnedObject.AddComponent<SpawnedObject>();
            tracker.Initialize(this, selectedSpawnPoint);

            Debug.Log($"Spawned object at {selectedSpawnPoint.name}");
        } else
        {
            Debug.LogWarning("Failed to spawn object - no available spawn point or prefab.");
        }
    }

    private Transform SelectSpawnPoint()
    {
        // If enforcing occupancy, only select from available spawn points
        if (enforceSpawnPointOccupancy)
        {
            // Create a list of available spawn points
            List<Transform> availablePoints = new List<Transform>();

            foreach (var kvp in spawnPointOccupancy)
            {
                if (kvp.Value == null)
                {
                    availablePoints.Add(kvp.Key);
                }
            }

            if (availablePoints.Count == 0)
                return null;

            // Choose a random available spawn point
            int randomIndex = Random.Range(0, availablePoints.Count);
            return availablePoints[randomIndex];
        }
        // If not enforcing occupancy, use the original selection method
        else
        {
            if (spawnPoints.Count == 0)
                return null;

            Transform selectedPoint;

            if (useRandomSpawnPoints)
            {
                // Choose a random spawn point
                int randomIndex = Random.Range(0, spawnPoints.Count);
                selectedPoint = spawnPoints[randomIndex];
            } else
            {
                // Choose the next spawn point in sequence
                // We'll track this with a static variable
                int nextSpawnPointIndex = 0;

                selectedPoint = spawnPoints[nextSpawnPointIndex];

                // Update the index for next time
                nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Count;
            }

            return selectedPoint;
        }
    }

    private GameObject SelectPrefab()
    {
        if (spawnablePrefabs.Count == 0)
            return null;

        GameObject selectedPrefab;

        if (useRandomPrefabs)
        {
            // Choose a random prefab
            int randomIndex = Random.Range(0, spawnablePrefabs.Count);
            selectedPrefab = spawnablePrefabs[randomIndex];
        } else
        {
            // Choose the next prefab in sequence
            selectedPrefab = spawnablePrefabs[nextPrefabIndex];

            // Update the index for next time
            nextPrefabIndex = (nextPrefabIndex + 1) % spawnablePrefabs.Count;
        }

        return selectedPrefab;
    }

    private void CleanupSpawnedObjectsList()
    {
        // Remove any null entries (destroyed objects) from the list
        spawnedObjects.RemoveAll(item => item == null);

        // Check spawn point occupancy and clear any null references
        List<Transform> pointsToFree = new List<Transform>();

        foreach (var kvp in spawnPointOccupancy)
        {
            if (kvp.Value == null)
                continue;

            if (kvp.Value == null || !spawnedObjects.Contains(kvp.Value))
            {
                pointsToFree.Add(kvp.Key);
            }
        }

        // Free up the spawn points
        foreach (Transform point in pointsToFree)
        {
            FreeSpawnPoint(point);
        }
    }

    // Public method to free a spawn point
    public void FreeSpawnPoint(Transform spawnPoint)
    {
        if (spawnPointOccupancy.ContainsKey(spawnPoint))
        {
            Debug.Log($"Freed spawn point: {spawnPoint.name}");
            spawnPointOccupancy[spawnPoint] = null;
        }
    }

    // Public method to manually trigger a spawn
    public void ForceSpawn()
    {
        if (spawnedObjects.Count < maxSpawnedObjects && HasAvailableSpawnPoints())
        {
            SpawnObject();
        } else
        {
            if (spawnedObjects.Count >= maxSpawnedObjects)
            {
                Debug.Log("Cannot spawn more objects - maximum limit reached.");
            } else
            {
                Debug.Log("Cannot spawn more objects - no available spawn points.");
            }
        }
    }

    // Public method to stop the spawner
    public void StopSpawning()
    {
        canSpawn = false;
    }

    // Public method to resume spawning
    public void ResumeSpawning()
    {
        if (!canSpawn)
        {
            canSpawn = true;
            StartCoroutine(SpawnRoutine());
        }
    }

    // Public method to destroy all spawned objects
    public void DestroyAllSpawnedObjects()
    {
        foreach (GameObject obj in new List<GameObject>(spawnedObjects))
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        // Clear the list
        spawnedObjects.Clear();

        // Reset all spawn points
        InitializeSpawnPoints();
    }

    // Public method to get current count
    public int GetSpawnedObjectsCount()
    {
        return spawnedObjects.Count;
    }

    // Public method to report when an object is destroyed
    public void NotifyObjectDestroyed(GameObject obj, Transform spawnPoint)
    {
        if (spawnedObjects.Contains(obj))
        {
            spawnedObjects.Remove(obj);
        }

        // Free up the spawn point if we're using occupancy
        if (enforceSpawnPointOccupancy)
        {
            FreeSpawnPoint(spawnPoint);
        }
    }

    // Optional: Add a method to visualize spawn points in the editor
    private void OnDrawGizmos()
    {
        if (spawnPoints == null)
            return;

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            // If in play mode and we have the dictionary, check occupancy
            if (Application.isPlaying && spawnPointOccupancy.ContainsKey(point))
            {
                if (spawnPointOccupancy[point] == null)
                {
                    // Available spawn point (green)
                    Gizmos.color = Color.green;
                } else
                {
                    // Occupied spawn point (red)
                    Gizmos.color = Color.red;
                }
            } else
            {
                // Default color in edit mode (yellow)
                Gizmos.color = Color.yellow;
            }

            Gizmos.DrawWireSphere(point.position, 0.5f);
            Gizmos.DrawLine(point.position, point.position + point.forward);
        }
    }
}

// Helper component to add to spawned objects
public class SpawnedObject : MonoBehaviour
{
    private ObjectSpawner parentSpawner;
    private Transform spawnPoint;

    public void Initialize(ObjectSpawner spawner, Transform originSpawnPoint)
    {
        parentSpawner = spawner;
        spawnPoint = originSpawnPoint;
    }

    private void OnDestroy()
    {
        // Notify the spawner when this object is destroyed
        if (parentSpawner != null)
        {
            parentSpawner.NotifyObjectDestroyed(gameObject, spawnPoint);
        }
    }
}