using UnityEngine;

public class move : MonoBehaviour
{
    public float speedZ = 0.1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       transform.Translate(0, 0, speedZ);
       transform.Rotate(0, 0.1f, 0);
    }
}
