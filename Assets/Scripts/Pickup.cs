using UnityEngine;

public enum PickupType { Boat, Goat, Pickaxe }

public class Pickup : MonoBehaviour {
    public PickupType type;

    void OnTriggerEnter2D(Collider2D other) {
        var pc = other.GetComponent<PlayerController>();
        if (pc == null) return;
        switch (type) {
            case PickupType.Boat:    pc.GrantSail();  break;
            case PickupType.Goat:    pc.GrantClimb(); break;
            case PickupType.Pickaxe: pc.GrantMine();  break;
        }
        Destroy(gameObject);
    }
}
