using UnityEngine;

public class WinUITestHotkey : MonoBehaviour {
    public GameObject panel;
    void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (panel == null) { Debug.LogError("[WinUITestHotkey] Panel is null"); return; }
            panel.SetActive(!panel.activeSelf);
            Debug.Log("[WinUITestHotkey] Toggled panel: " + panel.activeSelf);
        }
    }
}
