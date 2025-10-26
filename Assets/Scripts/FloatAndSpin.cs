using UnityEngine;

public class FloatAndSpin : MonoBehaviour {
    public float floatAmplitude = 0.1f;   
    public float floatFrequency = 2f;     
    public float rotationSpeed = 50f;     

    private Vector3 startPos;

    void Start() {
        startPos = transform.position;
    }

    void Update() {
       
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

       
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
    }
}
