using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/TileCatalog")]
public class TileCatalog : ScriptableObject {
    public TileBase grass;
    public TileBase water;
    public TileBase mountain;
}
