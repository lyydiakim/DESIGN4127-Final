using UnityEngine;

public class forceResolution : MonoBehaviour
{
    public int width = 2560;
    public int height = 1440;
    public bool fullscreen = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Screen.SetResolution(width, height, fullscreen);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
