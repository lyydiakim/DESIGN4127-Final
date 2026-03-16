using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class defaultSelectOnEnable : MonoBehaviour
{
    public Button primaryButton;
    public float selectDelay = 0.1f; // Default delay of 0.1 seconds
    public bool useDelayOnStart = false; // Option to use delay on Start as well

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (useDelayOnStart)
        {
            StartCoroutine(SelectButtonWithDelay());
        } else
        {
            primaryButton.Select();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnEnable()
    {
        StartCoroutine(SelectButtonWithDelay());
    }

    // Coroutine to handle delay before selecting the button
    private IEnumerator SelectButtonWithDelay()
    {
        yield return new WaitForSeconds(selectDelay);

        if (primaryButton != null)
        {
            primaryButton.Select();
        } else
        {
            Debug.LogWarning("Primary button reference is missing in " + gameObject.name);
        }
    }
}