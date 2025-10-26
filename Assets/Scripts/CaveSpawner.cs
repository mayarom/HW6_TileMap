using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CaveSpawner : MonoBehaviour
{
    [Header("References")]
    public Tilemap tilemap;
    public TileCatalog catalog;
    public Transform player;

    [Header("Item Prefabs")]
    public GameObject pickaxePrefab;
    public GameObject boatPrefab;
    public GameObject goatPrefab;
    public GameObject goalPrefab;

    [Header("Map Settings")]
    public int width = 60;
    public int height = 40;
    public float noiseScale = 0.12f;
    public float waterThreshold = 0.35f;
    public float mountainThreshold = 0.62f;
    public bool borderMountains = true;

    [Header("Spawn Rules")]
    public int minReachable = 100;
    public int maxTries = 200;
    public float minItemDistance = 5f;
    public float minGoalDistance = 15f;

    [Header("Camera")]
    public bool snapCameraToPlayer = true;

    private int seed;
    private System.Random rng;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private Dictionary<string, string> placedItems = new Dictionary<string, string>();
    private HashSet<Vector3Int> usedCells = new HashSet<Vector3Int>();

    // Helpers to classify tiles
    bool IsWater(Vector3Int c) => tilemap.GetTile(c) == catalog.water;
    bool IsGrass(Vector3Int c) => tilemap.GetTile(c) == catalog.grass;
    bool IsMountain(Vector3Int c) => tilemap.GetTile(c) == catalog.mountain;

    string TileKind(Vector3Int c)
    {
        var t = tilemap.GetTile(c);
        if (t == null) return "None";
        if (t == catalog.grass) return "Grass";
        if (t == catalog.water) return "Water";
        if (t == catalog.mountain) return "Mountain";
        return "Other";
    }

    void SpawnObjectSafe(GameObject prefab, Vector3Int cell, System.Func<Vector3Int, bool> ok, string label)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[CaveSpawner] ⚠ Prefab is NULL for {label}");
            return;
        }
        if (!ok(cell))
        {
            Debug.LogWarning($"[CaveSpawner] ✗ Blocked {label} at {cell} because tile={TileKind(cell)}");
            return;
        }
        
        // Safety: Mark cell as used (prevents overlaps even if called without TryPickCell)
        usedCells.Add(cell);
        
        var pos = tilemap.GetCellCenterWorld(cell);
        pos.z = 0f;
        var obj = Instantiate(prefab, pos, Quaternion.identity);
        spawnedObjects.Add(obj);
        
        // Track for summary
        string itemName = label.Split(' ')[0];
        placedItems[itemName] = TileKind(cell);
        
        Debug.Log($"[CaveSpawner] ✓ {label} at {cell} (tile={TileKind(cell)}) | prefab={prefab.name}");
    }

    bool TryPickCell(List<Vector3Int> candidates, System.Predicate<Vector3Int> ok, out Vector3Int cell, int maxAttempts = 80)
    {
        for (int i = 0; i < maxAttempts && candidates.Count > 0; i++)
        {
            int idx = rng.Next(candidates.Count);
            var c = candidates[idx];
            if (ok(c) && !usedCells.Contains(c))
            {
                cell = c;
                usedCells.Add(c); // Mark cell as used
                return true;
            }
            candidates.RemoveAt(idx);
        }
        cell = default;
        return false;
    }

    bool TryPickNotUsed(List<Vector3Int> list, out Vector3Int cell)
    {
        // Find a candidate that is not in usedCells
        foreach (var c in list)
        {
            if (!usedCells.Contains(c))
            {
                cell = c;
                return true;
            }
        }
        cell = default;
        return false;
    }

    void Start()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("[CaveSpawner] Missing required references!");
            return;
        }

        seed = UnityEngine.Random.Range(0, 1000000);
        rng = new System.Random(seed);
        GenerateAndSetup();
    }

    bool ValidateReferences()
    {
        if (tilemap == null)
        {
            Debug.LogError("[CaveSpawner] Tilemap reference is missing!");
            return false;
        }
        if (catalog == null || catalog.grass == null || 
            catalog.mountain == null || catalog.water == null)
        {
            Debug.LogError("[CaveSpawner] TileCatalog is missing tiles.");
            return false;
        }
        if (player == null)
        {
            Debug.LogError("[CaveSpawner] Player reference is missing!");
            return false;
        }
        return true;
    }

    void GenerateAndSetup()
    {
        ClearSpawnedObjects();
        placedItems.Clear();
        usedCells.Clear();
        GenerateMap();
        PlacePlayerSmart();
        PlaceItems();
        PrintItemSummary();
    }

    void ClearSpawnedObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    void GenerateMap()
    {
        tilemap.ClearAllTiles();
        Vector3Int origin = new Vector3Int(-width / 2, -height / 2, 0);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = origin + new Vector3Int(x, y, 0);

                if (borderMountains && (x == 0 || y == 0 || x == width - 1 || y == height - 1))
                {
                    tilemap.SetTile(cell, catalog.mountain);
                    continue;
                }

                float nx = (x + seed) * noiseScale;
                float ny = (y + seed) * noiseScale;
                float n = Mathf.PerlinNoise(nx, ny);

                if (n < waterThreshold)
                {
                    tilemap.SetTile(cell, catalog.water);
                }
                else if (n > mountainThreshold)
                {
                    tilemap.SetTile(cell, catalog.mountain);
                }
                else
                {
                    tilemap.SetTile(cell, catalog.grass);
                }
            }
        }
        Debug.Log("[CaveSpawner] Map generated.");
    }

    void PlacePlayerSmart()
    {
        List<Vector3Int> grassCells = new List<Vector3Int>();
        BoundsInt bounds = tilemap.cellBounds;
        
        foreach (var c in bounds.allPositionsWithin)
        {
            var t = tilemap.GetTile(c);
            if (t == catalog.grass) grassCells.Add(c);
        }

        if (grassCells.Count == 0)
        {
            Debug.LogWarning("[CaveSpawner] No grass - regenerating...");
            seed = rng.Next(0, 1000000);
            GenerateAndSetup();
            return;
        }

        for (int i = 0; i < maxTries; i++)
        {
            var cell = grassCells[rng.Next(grassCells.Count)];
            HashSet<Vector3Int> reachable = BFSReachableMulti(cell, new TileBase[] { catalog.grass });
            
            if (reachable.Count >= minReachable)
            {
                Vector3 p = tilemap.GetCellCenterWorld(cell);
                p.z = 0f;
                player.position = p;
                
                // Mark player cell as used
                var playerCell = tilemap.WorldToCell(player.position);
                usedCells.Add(playerCell);
                
                Debug.Log($"[CaveSpawner] Player spawned at {cell}");
                AdjustCamera();
                return;
            }
        }

        Debug.LogWarning("[CaveSpawner] No valid spawn - regenerating...");
        seed = rng.Next(0, 1000000);
        GenerateAndSetup();
    }

    void AdjustCamera()
    {
        if (!snapCameraToPlayer || Camera.main == null) return;

        var bounds = tilemap.localBounds;
        Vector3 centerWorld = tilemap.transform.TransformPoint(bounds.center);
        Camera.main.transform.position = new Vector3(centerWorld.x, centerWorld.y, Camera.main.transform.position.z);

        float mapWidth = bounds.size.x;
        float mapHeight = bounds.size.y;
        float screenAspect = (float)Screen.width / (float)Screen.height;
        float mapAspect = mapWidth / mapHeight;
        
        if (mapAspect > screenAspect)
        {
            Camera.main.orthographicSize = mapWidth / (2f * screenAspect);
        }
        else
        {
            Camera.main.orthographicSize = mapHeight / 2f;
        }
    }

    void PlaceItems()
    {
        Vector3Int playerCell = tilemap.WorldToCell(player.position);
        
        HashSet<Vector3Int> reachableGrassOnly = BFSReachableMulti(playerCell, new TileBase[] { catalog.grass });
        
        List<Vector3Int> reachableGrassList = new List<Vector3Int>();
        
        BoundsInt bounds = tilemap.cellBounds;
        foreach (var c in bounds.allPositionsWithin)
        {
            if (!IsGrass(c)) continue;
            if (!reachableGrassOnly.Contains(c)) continue;
            
            int sqrDist = (c - playerCell).sqrMagnitude;
            float minSqrDist = minItemDistance * minItemDistance;
            
            if (sqrDist >= minSqrDist)
            {
                reachableGrassList.Add(c);
            }
        }

        if (reachableGrassList.Count == 0)
        {
            Debug.LogWarning("[CaveSpawner] No reachable grass - regenerating...");
            seed = rng.Next(0, 1000000);
            GenerateAndSetup();
            return;
        }

        bool goatFirst = rng.Next(2) == 0;

        Debug.Log($"[CaveSpawner] === ITEM PLACEMENT - Scenario: {(goatFirst ? "Goat First" : "Boat First")} ===");

        // --- SCENARIO A: Goat reachable on GRASS, Boat on MOUNTAIN ---
        if (goatFirst)
        {
            // Place Goat on reachable grass
            if (goatPrefab != null && TryPickCell(reachableGrassList, c => IsGrass(c), out var goatCell))
            {
                SpawnObjectSafe(goatPrefab, goatCell, IsGrass, "Goat");
            }

            // After getting Goat, player can walk on grass + mountains
            if (boatPrefab != null)
            {
                HashSet<Vector3Int> reachableWithGoat = BFSReachableMulti(playerCell, new TileBase[] { catalog.grass, catalog.mountain });
                
                List<Vector3Int> reachableMountains = new List<Vector3Int>();
                foreach (var c in bounds.allPositionsWithin)
                {
                    if (IsMountain(c) && reachableWithGoat.Contains(c))
                    {
                        reachableMountains.Add(c);
                    }
                }

                if (reachableMountains.Count > 0 && TryPickCell(reachableMountains, c => IsMountain(c), out var boatCell))
                {
                    SpawnObjectSafe(boatPrefab, boatCell, IsMountain, "Boat");
                }
                else if (TryPickCell(reachableGrassList, c => IsGrass(c), out var boatCellFallback))
                {
                    SpawnObjectSafe(boatPrefab, boatCellFallback, IsGrass, "Boat (fallback)");
                }
            }
        }
        // --- SCENARIO B: Boat reachable on GRASS, Goat also on GRASS (never water) ---
        else
        {
            // Place Boat on reachable grass
            if (boatPrefab != null && TryPickCell(reachableGrassList, c => IsGrass(c), out var boatCell))
            {
                SpawnObjectSafe(boatPrefab, boatCell, IsGrass, "Boat");
            }

            // Place Goat on reachable grass (never on water!)
            if (goatPrefab != null && TryPickCell(reachableGrassList, c => IsGrass(c), out var goatCell))
            {
                SpawnObjectSafe(goatPrefab, goatCell, IsGrass, "Goat");
            }
        }

        // Pickaxe - always on reachable grass with verification
        if (pickaxePrefab != null)
        {
            bool pickaxePlaced = false;
            for (int attempt = 0; attempt < 100; attempt++)
            {
                if (TryPickCell(reachableGrassList, c => IsGrass(c), out var pickCell))
                {
                    var center = tilemap.GetCellCenterWorld(pickCell);
                    center.z = 0f;
                    var obj = Instantiate(pickaxePrefab, center, Quaternion.identity);
                    
                    // Verify tile is still grass after spawning
                    if (IsGrass(pickCell))
                    {
                        spawnedObjects.Add(obj);
                        placedItems["Pickaxe"] = TileKind(pickCell);
                        Debug.Log($"[CaveSpawner] ✓ Pickaxe at {pickCell} (tile={TileKind(pickCell)}) | prefab={pickaxePrefab.name}");
                        pickaxePlaced = true;
                        break;
                    }
                    else
                    {
                        // Tile changed unexpectedly, destroy and retry
                        Debug.LogWarning($"[CaveSpawner] Pickaxe tile verification failed at {pickCell}. Retrying...");
                        Destroy(obj);
                        usedCells.Remove(pickCell); // Release the cell for retry
                    }
                }
            }
            
            if (!pickaxePlaced)
            {
                Debug.LogWarning("[CaveSpawner] Failed to place Pickaxe after 100 attempts!");
            }
        }

        PlaceGoal(playerCell);
    }

    void PlaceGoal(Vector3Int playerCell)
    {
        if (goalPrefab == null) return;

        HashSet<Vector3Int> reachableWithBothItems = BFSReachableMulti(playerCell, 
            new TileBase[] { catalog.grass, catalog.water, catalog.mountain });

        List<Vector3Int> farCells = new List<Vector3Int>();
        List<Vector3Int> challengingCells = new List<Vector3Int>();
        BoundsInt bounds = tilemap.cellBounds;

        foreach (var c in bounds.allPositionsWithin)
        {
            if (!reachableWithBothItems.Contains(c)) continue;
            
            int sqrDist = (c - playerCell).sqrMagnitude;
            float minSqrDist = minGoalDistance * minGoalDistance;
            
            if (sqrDist >= minSqrDist)
            {
                farCells.Add(c);
                if (IsWater(c) || IsMountain(c))
                {
                    challengingCells.Add(c);
                }
            }
        }

        Vector3Int goalCell = default; // Initialize to avoid CS0165 error
        bool found =
            (challengingCells.Count > 0 && TryPickNotUsed(challengingCells, out goalCell)) ||
            (challengingCells.Count == 0 && farCells.Count > 0 && TryPickNotUsed(farCells, out goalCell));

        if (found)
        {
            usedCells.Add(goalCell);
            SpawnObjectSafe(goalPrefab, goalCell, c => IsWater(c) || IsMountain(c) || IsGrass(c), "Goal");
        }
        else
        {
            Debug.LogWarning("[CaveSpawner] Could not place Goal without overlap.");
        }
    }

    void PrintItemSummary()
    {
        Debug.Log("========================================");
        Debug.Log("[CaveSpawner] === ITEM PLACEMENT SUMMARY ===");
        
        if (placedItems.ContainsKey("Goat"))
            Debug.Log($"[CaveSpawner] 🐐 GOAT (canClimb) → {placedItems["Goat"]}");
        else
            Debug.Log("[CaveSpawner] 🐐 GOAT → NOT PLACED");
            
        if (placedItems.ContainsKey("Boat"))
            Debug.Log($"[CaveSpawner] 🚤 BOAT (canSail) → {placedItems["Boat"]}");
        else
            Debug.Log("[CaveSpawner] 🚤 BOAT → NOT PLACED");
            
        if (placedItems.ContainsKey("Pickaxe"))
            Debug.Log($"[CaveSpawner] 🔨 PICKAXE (canMine) → {placedItems["Pickaxe"]}");
        else
            Debug.Log("[CaveSpawner] 🔨 PICKAXE → NOT PLACED");
            
        if (placedItems.ContainsKey("Goal"))
            Debug.Log($"[CaveSpawner] 🏆 GOAL → {placedItems["Goal"]}");
        else
            Debug.Log("[CaveSpawner] 🏆 GOAL → NOT PLACED");
            
        Debug.Log("========================================");
    }

    HashSet<Vector3Int> BFSReachableMulti(Vector3Int start, TileBase[] walkableTiles)
    {
        Queue<Vector3Int> q = new Queue<Vector3Int>();
        HashSet<Vector3Int> seen = new HashSet<Vector3Int>();
        q.Enqueue(start);
        seen.Add(start);

        Vector3Int[] dirs = {
            new Vector3Int(1, 0, 0), 
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), 
            new Vector3Int(0, -1, 0)
        };

        while (q.Count > 0 && seen.Count < 10000)
        {
            var cur = q.Dequeue();
            foreach (var d in dirs)
            {
                var nxt = cur + d;
                if (seen.Contains(nxt)) continue;
                
                var t = tilemap.GetTile(nxt);
                
                bool canWalk = false;
                foreach (var walkable in walkableTiles)
                {
                    if (t == walkable)
                    {
                        canWalk = true;
                        break;
                    }
                }
                
                if (canWalk)
                {
                    seen.Add(nxt);
                    q.Enqueue(nxt);
                }
            }
        }
        return seen;
    }
}