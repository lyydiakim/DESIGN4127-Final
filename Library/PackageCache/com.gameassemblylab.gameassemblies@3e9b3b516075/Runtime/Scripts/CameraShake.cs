using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    // Singleton instance
    private static CameraShake instance;

    // Original camera position
    private Vector3 originalPosition;

    // Shake coroutine reference
    private Coroutine shakeCoroutine;

    // Shake parameters
    [SerializeField] private float defaultDuration = 0.5f;
    [SerializeField] private float defaultMagnitude = 0.1f;
    [SerializeField] private float defaultFrequency = 10f;

    private void Awake()
    {
        // Set up singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(this);
        } else
        {
            instance = this;
        }

        // Store original position
        originalPosition = transform.localPosition;
    }

    /// <summary>
    /// Shakes the camera with default parameters
    /// </summary>
    public static void Shake()
    {
        if (instance != null)
        {
            instance.DoShake(instance.defaultDuration, instance.defaultMagnitude, instance.defaultFrequency);
        }
    }

    /// <summary>
    /// Shakes the camera with custom parameters
    /// </summary>
    /// <param name="duration">Duration of the shake in seconds</param>
    /// <param name="magnitude">Strength of the shake</param>
    /// <param name="frequency">Speed of the shake</param>
    public static void Shake(float duration, float magnitude, float frequency)
    {
        if (instance != null)
        {
            instance.DoShake(duration, magnitude, frequency);
        }
    }

    /// <summary>
    /// Performs the actual camera shake
    /// </summary>
    private void DoShake(float duration, float magnitude, float frequency)
    {
        // Stop any existing shake
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        // Start a new shake
        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude, frequency));
    }

    /// <summary>
    /// Coroutine that handles the shake effect
    /// </summary>
    private IEnumerator ShakeCoroutine(float duration, float magnitude, float frequency)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float percentComplete = elapsed / duration;

            // Dampen the shake effect over time
            float damper = 1f - Mathf.Clamp01(percentComplete);

            // Generate a random shake offset
            float x = Random.Range(-1f, 1f) * magnitude * damper;
            float y = Random.Range(-1f, 1f) * magnitude * damper;

            // Apply the shake offset
            transform.localPosition = originalPosition + new Vector3(x, y, 0);

            // Update elapsed time
            elapsed += Time.deltaTime;

            // Wait for next frame
            yield return null;
        }

        // Reset to original position
        transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }
}