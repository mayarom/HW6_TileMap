using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public struct PlayerAbilities {
    public bool canSail;   // Boat
    public bool canClimb;  // Goat
    public bool canMine;   // Pickaxe
}

public class PlayerController : MonoBehaviour {
    [Header("World")]
    public Tilemap tilemap;
    public TileCatalog catalog;

    [Header("Movement")]
    public float moveSpeed = 3f;

    public PlayerAbilities abilities;

    private Queue<Vector3> pathQ = new();
    private Vector3? currentTarget = null;

    void Awake() {
        // Validate references
        if (tilemap == null) {
            Debug.LogError("[PlayerController] Tilemap reference is missing!");
        }
        if (catalog == null) {
            Debug.LogError("[PlayerController] TileCatalog reference is missing!");
        }
    }

    void Update() {
        // Early exit if references are missing
        if (tilemap == null || catalog == null) return;

        // Left click - request path
        if (Input.GetMouseButtonDown(0)) {
            Vector3? worldClick = GetMouseWorldOnTilemapZ();
            if (!worldClick.HasValue) {
                Debug.LogWarning("[PlayerController] Click did not hit tilemap plane. Ignoring.");
                return;
            }

            Vector3 world = worldClick.Value;
            Debug.Log($"[PlayerController] Left click screen {Input.mousePosition} -> world {world}");

            var path = FindPathBFS(transform.position, world, abilities);
            pathQ = new Queue<Vector3>(path);
            currentTarget = pathQ.Count > 0 ? pathQ.Peek() : (Vector3?)null;

            Debug.Log($"[PlayerController] Path calculated with {pathQ.Count} nodes.");
        }

        // Movement along the path
        if (currentTarget.HasValue) {
            Vector3 target = currentTarget.Value;
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, target) < 0.01f) {
                pathQ.Dequeue();
                currentTarget = pathQ.Count > 0 ? pathQ.Peek() : (Vector3?)null;
                Debug.Log($"[PlayerController] Reached node. Remaining: {pathQ.Count}");
            }
        }
    }

    // Robust conversion - ray from camera to tilemap's Z plane
    Vector3? GetMouseWorldOnTilemapZ() {
        if (Camera.main == null) {
            Debug.LogError("[PlayerController] No main camera found.");
            return null;
        }
        
        float z = tilemap != null ? tilemap.transform.position.z : 0f;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, z));
        
        if (plane.Raycast(ray, out float enter)) {
            Vector3 hit = ray.GetPoint(enter);
            hit.z = z;
            return hit;
        }
        return null;
    }

    // Check if the player can enter a tile based on abilities
    bool CanEnter(TileBase t, PlayerAbilities abil) {
        if (t == null) return false;
        if (t == catalog.grass) return true;
        if (t == catalog.water) return abil.canSail;     // Boat enables sailing
        if (t == catalog.mountain) return abil.canClimb; // Goat enables climbing
        return false;
    }

    // BFS 4-directional pathfinding
    static readonly Vector3Int[] DIRS = {
        new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
    };

    List<Vector3> FindPathBFS(Vector3 worldStart, Vector3 worldGoal, PlayerAbilities abil) {
        var start = tilemap.WorldToCell(worldStart);
        var goal  = tilemap.WorldToCell(worldGoal);

        Debug.Log($"[PlayerController] Finding path from {start} to {goal}");

        var q = new Queue<Vector3Int>();
        var came = new Dictionary<Vector3Int, Vector3Int>();
        var seen = new HashSet<Vector3Int>();

        q.Enqueue(start);
        seen.Add(start);

        while (q.Count > 0) {
            var cur = q.Dequeue();
            if (cur == goal) break;

            foreach (var d in DIRS) {
                var nxt = cur + d;
                if (seen.Contains(nxt)) continue;
                var tile = tilemap.GetTile(nxt);
                if (!CanEnter(tile, abil)) continue;
                seen.Add(nxt);
                came[nxt] = cur;
                q.Enqueue(nxt);
            }
        }

        var path = new List<Vector3>();
        if (start == goal) {
            path.Add(CellCenter(start));
            Debug.Log("[PlayerController] Start equals goal - single tile path.");
            return path;
        }
        if (!came.ContainsKey(goal)) {
            Debug.LogWarning("[PlayerController] No path found!");
            return path;
        }

        var c = goal;
        path.Add(CellCenter(c));
        while (c != start) {
            c = came[c];
            path.Add(CellCenter(c));
        }
        path.Reverse();

        Debug.Log($"[PlayerController] Path found with {path.Count} tiles.");
        return path;
    }

    Vector3 CellCenter(Vector3Int c) => tilemap.GetCellCenterWorld(c);

    // Ability pickups
    public void GrantSail()  { abilities.canSail = true; Debug.Log("[PlayerController] Player gained sailing ability!"); }
    public void GrantClimb() { abilities.canClimb = true; Debug.Log("[PlayerController] Player gained climbing ability!"); }
    public void GrantMine()  { abilities.canMine = true; Debug.Log("[PlayerController] Player gained mining ability!"); }
}