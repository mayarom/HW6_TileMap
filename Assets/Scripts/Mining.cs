using UnityEngine;
using UnityEngine.Tilemaps;

public class Mining : MonoBehaviour {
    [Header("References")]
    [SerializeField] public PlayerController player;
    [SerializeField] public Tilemap tilemap;
    [SerializeField] public TileCatalog catalog;
    
    [Header("Mountain Tile Variants")]
    [Tooltip("Additional mountain tile variants (for RuleTiles or visual variations)")]
    [SerializeField] public TileBase[] mountainVariants;

    [Header("Mining Options")]
    [Tooltip("Allow mining diagonally adjacent tiles (8 directions instead of 4)")]
    [SerializeField] public bool allowDiagonalMining = false;
    
    [Tooltip("Allow mining the tile the player is currently standing on")]
    [SerializeField] public bool allowMiningCurrentTile = false;

    void Awake() {
        // Auto-wire missing references
        if (player == null) {
            player = FindObjectOfType<PlayerController>();
            if (player == null) {
                Debug.LogError("[Mining] PlayerController not found in scene!");
            }
        }
        
        if (tilemap == null) {
            tilemap = FindObjectOfType<Tilemap>();
            if (tilemap == null) {
                Debug.LogError("[Mining] Tilemap not found in scene!");
            }
        }
        
        if (catalog == null && player != null) {
            catalog = player.catalog;
            if (catalog == null) {
                Debug.LogError("[Mining] TileCatalog not found!");
            }
        }
    }

    void Update() {
        // Validate references before processing
        if (player == null || tilemap == null || catalog == null) return;
        
        // Only process mining if player has the ability
        if (!player.abilities.canMine) return;

        // Right click to mine
        if (Input.GetMouseButtonDown(1)) {
            Vector3? worldClick = GetMouseWorldOnTilemapZ();
            if (!worldClick.HasValue) {
                Debug.LogWarning("[Mining] Right-click did not hit tilemap plane. Ignoring.");
                return;
            }

            Vector3 world = worldClick.Value;
            Debug.Log($"[Mining] Right click screen {Input.mousePosition} -> world {world}");

            var cell = tilemap.WorldToCell(world);
            var myCell = tilemap.WorldToCell(player.transform.position);

            Debug.Log($"[Mining] Target cell: {cell}, Player cell: {myCell}");

            // Check if tile is mineable based on settings
            if (IsMineable(cell, myCell)) {
                var t = tilemap.GetTile(cell);
                if (IsMountainTile(t)) {
                    tilemap.SetTile(cell, catalog.grass);
                    Debug.Log($"[Mining] Successfully mined a mountain tile at {cell}!");
                } else {
                    Debug.Log($"[Mining] Tile at {cell} is not a mountain. Ignoring.");
                }
            } else {
                Debug.Log($"[Mining] Target tile at {cell} is not within mining range.");
            }
        }
    }

    // Robust conversion - ray from camera to tilemap's Z plane
    Vector3? GetMouseWorldOnTilemapZ() {
        if (Camera.main == null) {
            Debug.LogError("[Mining] No main camera found.");
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

    // Check if a cell is mineable based on distance from player and settings
    bool IsMineable(Vector3Int targetCell, Vector3Int playerCell) {
        int dx = Mathf.Abs(targetCell.x - playerCell.x);
        int dy = Mathf.Abs(targetCell.y - playerCell.y);

        // Check if mining current tile
        if (dx == 0 && dy == 0) {
            return allowMiningCurrentTile;
        }

        // Check adjacency based on settings
        if (allowDiagonalMining) {
            // 8-directional: adjacent includes diagonals
            return Mathf.Max(dx, dy) == 1;
        } else {
            // 4-directional: only cardinal directions (up, down, left, right)
            return dx + dy == 1;
        }
    }

    // Check if a tile is a mountain (includes variants)
    bool IsMountainTile(TileBase t) {
        if (t == null) return false;
        if (t == catalog.mountain) return true;
        
        // Check additional variants if provided
        if (mountainVariants != null) {
            foreach (var variant in mountainVariants) {
                if (t == variant) return true;
            }
        }
        
        return false;
    }
}