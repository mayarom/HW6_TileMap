using UnityEngine;

public class InstructionsToggle : MonoBehaviour {
    public GameObject panel;

    void Update() {
        if (Input.GetKeyDown(KeyCode.H)) {
            panel.SetActive(!panel.activeSelf);
        }
    }
}
