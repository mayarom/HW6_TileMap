using UnityEngine;
using UnityEngine.UI;

public class Goal : MonoBehaviour {
    [Header("Optional direct reference")]
    [SerializeField] GameObject winPanel;   // Drag your WinPanel here if you have it

    [Header("Optional lookup hints")]
    [SerializeField] string panelName = "WinPanel"; // exact name to find if reference is missing
    [SerializeField] string panelTag  = "";         // or set a tag on the panel and put it here

    void OnTriggerEnter2D(Collider2D other) {
        Debug.Log($"[Goal] Trigger with '{other.name}'");
        var player = other.GetComponent<PlayerController>();
        if (player == null) {
            Debug.Log("[Goal] Not a Player. Ignoring.");
            return;
        }

        Debug.Log("[Goal] Player detected. Trying to show WinPanel...");

        // 1) Resolve the panel reference if missing
        if (winPanel == null) {
            if (!string.IsNullOrEmpty(panelTag)) {
                var byTag = GameObject.FindGameObjectWithTag(panelTag);
                if (byTag != null) {
                    winPanel = byTag;
                    Debug.Log($"[Goal] Found panel by tag '{panelTag}': {winPanel.name}");
                }
            }
        }
        if (winPanel == null && !string.IsNullOrEmpty(panelName)) {
            var byName = GameObject.Find(panelName);
            if (byName != null) {
                winPanel = byName;
                Debug.Log($"[Goal] Found panel by name '{panelName}': {winPanel.name}");
            }
        }

        if (winPanel == null) {
            Debug.LogError("[Goal] WinPanel reference is NULL and lookup failed. Assign it in the Inspector or set 'panelName'/'panelTag'.");
            return;
        }

        // 2) Log canvas & layout details to diagnose visibility issues
        var canvas = winPanel.GetComponentInParent<Canvas>();
        if (canvas != null) {
            Debug.Log($"[Goal] Canvas found: mode={canvas.renderMode}, sortingLayer={canvas.sortingLayerName}, order={canvas.sortingOrder}");
        } else {
            Debug.LogWarning("[Goal] No Canvas found in parents of WinPanel. UI may be invisible.");
        }

        var rt = winPanel.GetComponent<RectTransform>();
        if (rt != null) {
            Debug.Log($"[Goal] Panel RectTransform - anchorMin={rt.anchorMin}, anchorMax={rt.anchorMax}, sizeDelta={rt.sizeDelta}, anchoredPos={rt.anchoredPosition}, scale={rt.localScale}");
        }

        var cg = winPanel.GetComponent<CanvasGroup>();
        if (cg != null) {
            Debug.Log($"[Goal] CanvasGroup detected: alpha={cg.alpha}, interactable={cg.interactable}, blocksRaycasts={cg.blocksRaycasts}");
            if (cg.alpha <= 0f) {
                Debug.Log("[Goal] Forcing CanvasGroup alpha to 1.");
                cg.alpha = 1f;
            }
        }

        // 3) Show the panel
        bool wasActive = winPanel.activeInHierarchy;
        winPanel.SetActive(true);
        Debug.Log($"[Goal] WinPanel SetActive(true). WasActive={wasActive}, NowActive={winPanel.activeInHierarchy}");

        // 4) Stop player & time
        if (player.enabled) {
            player.enabled = false;
            Debug.Log("[Goal] Player controls disabled.");
        }
        Time.timeScale = 0f;
        Debug.Log("[Goal] Time.timeScale set to 0. Goal sequence complete.");
    }
}
