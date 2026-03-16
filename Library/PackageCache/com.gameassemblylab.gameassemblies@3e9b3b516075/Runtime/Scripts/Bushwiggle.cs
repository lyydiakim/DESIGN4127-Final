using System.Collections;
using UnityEngine;

public class Bushwiggle : MonoBehaviour
{
    [Header("Wiggle Settings")]
    // Total duration of the wiggle effect.
    public float wiggleDuration = 0.5f;
    // Maximum angle (in degrees) for the wiggle rotation.
    public float wiggleAngle = 15f;
    // Number of full wiggle cycles (back and forth).
    public int wiggleCycles = 3;

    private bool isWiggling = false;
    private Quaternion originalRotation;

    private void Awake()
    {
        originalRotation = transform.rotation;
    }

    // Detect when the player enters the trigger.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isWiggling && collision.CompareTag("Player"))
        {
            StartCoroutine(Wiggle());
        }
    }

    private IEnumerator Wiggle()
    {
        isWiggling = true;
        float halfCycleDuration = wiggleDuration / (wiggleCycles * 2);

        for (int i = 0; i < wiggleCycles; i++)
        {
            // Rotate to the positive angle.
            yield return RotateToAngle(wiggleAngle, halfCycleDuration);
            // Rotate to the negative angle.
            yield return RotateToAngle(-wiggleAngle, halfCycleDuration);
        }

        // Return to original rotation.
        transform.rotation = originalRotation;
        isWiggling = false;
    }

    private IEnumerator RotateToAngle(float targetAngle, float duration)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsed / duration);
            yield return null;
        }
        transform.rotation = targetRotation;
    }
}
