using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    [Tooltip("Time in seconds before the object self-destructs")]
    public float lifetime = 3.0f;

    [Tooltip("Optional particle effect to play when destroyed")]
    public GameObject destroyEffect;

    [Tooltip("Whether to play destroy effect when self-destructing")]
    public bool playEffectOnDestroy = false;

    // Called when the script instance is being loaded
    void Start()
    {
        // Schedule the object to be destroyed after lifetime seconds
        Destroy(gameObject, lifetime);
    }

    // This method will be called just before the object is destroyed
    void OnDestroy()
    {
        // If a destroy effect is specified and we want to play it
        if (destroyEffect != null && playEffectOnDestroy)
        {
            // Instantiate the effect at our position and rotation
            Instantiate(destroyEffect, transform.position, transform.rotation);
        }
    }
}