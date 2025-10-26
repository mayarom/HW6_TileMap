using UnityEngine;

public class WinUI : MonoBehaviour {
    [SerializeField] GameObject panel;   // Assign the WinPanel object here in the Inspector
    static GameObject sPanel;

    void Awake() {
        Debug.Log("[WinUI] Awake called.");

        if (panel == null) {
            Debug.LogWarning("[WinUI] No panel assigned in the Inspector!");
        } else {
            Debug.Log($"[WinUI] Panel '{panel.name}' assigned successfully.");
        }

        sPanel = panel;

        if (sPanel != null) {
            Debug.Log("[WinUI] Deactivating panel at start.");
            sPanel.SetActive(false);
        } else {
            Debug.LogWarning("[WinUI] Static panel reference is NULL after Awake.");
        }

        Debug.Log("[WinUI] Initialization complete.");
    }

    public static void Show() {
        Debug.Log("[WinUI] Show() called.");

        if (sPanel == null) {
            Debug.LogError("[WinUI] Cannot show panel: sPanel is NULL!");
            return;
        }

        if (sPanel.activeSelf) {
            Debug.Log("[WinUI] Panel is already active.");
        } else {
            Debug.Log("[WinUI] Activating WinPanel now!");
            sPanel.SetActive(true);
        }
    }
}
